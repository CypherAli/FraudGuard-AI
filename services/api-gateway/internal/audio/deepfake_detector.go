package audio

import (
	"encoding/binary"
	"log"
	"math"
	"os"
	"sync"
)

var deepfakeEnabled = os.Getenv("FEATURE_DEEPFAKE") == "true"

// dftSize is the fixed DFT window size used by calculateSpectralFlatness.
const dftSize = 512

// Precomputed DFT trig tables: dftCos[k][n] = cos(2π·k·n/N), dftSin[k][n] = sin(2π·k·n/N).
// Only the first N/2 frequency bins are needed (positive spectrum).
// Computed once at package init — eliminates math.Cos/Sin from the hot path.
var (
	dftCos [dftSize / 2][dftSize]float32
	dftSin [dftSize / 2][dftSize]float32
)

func init() {
	twoPiOverN := 2.0 * math.Pi / float64(dftSize)
	for k := 0; k < dftSize/2; k++ {
		for n := 0; n < dftSize; n++ {
			angle := twoPiOverN * float64(k) * float64(n)
			dftCos[k][n] = float32(math.Cos(angle))
			dftSin[k][n] = float32(math.Sin(angle))
		}
	}
}

// DeepfakeAnalysis holds the result of deepfake voice analysis
type DeepfakeAnalysis struct {
	Score           int     `json:"score"`            // 0-100, higher = more likely deepfake
	PitchVariance   float64 `json:"pitch_variance"`   // Natural speech has moderate variance
	BreathingScore  float64 `json:"breathing_score"`  // 0-1, lower = less breathing detected
	SpectralFlat    float64 `json:"spectral_flat"`    // Spectral flatness (AI voices tend to be flatter)
	EnergyConsist   float64 `json:"energy_consist"`   // Energy consistency (AI = too consistent)
	IsLikelyFake    bool    `json:"is_likely_fake"`
}

// DeepfakeDetector performs heuristic spectral analysis on audio chunks
type DeepfakeDetector struct {
	mu             sync.Mutex
	chunkScores    []int
	rollingAverage float64
	chunksAnalyzed int
}

// NewDeepfakeDetector creates a new deepfake detector for a session
func NewDeepfakeDetector() *DeepfakeDetector {
	return &DeepfakeDetector{
		chunkScores: make([]int, 0, 50),
	}
}

// IsEnabled returns whether deepfake detection is active
func IsEnabled() bool {
	return deepfakeEnabled
}

// AnalyzeChunk performs heuristic audio analysis on a PCM16 audio chunk
// Audio format: 16-bit signed PCM, 16kHz mono (matching Deepgram input)
func (dd *DeepfakeDetector) AnalyzeChunk(audioData []byte) DeepfakeAnalysis {
	if !deepfakeEnabled || len(audioData) < 640 { // Need at least 20ms of audio
		return DeepfakeAnalysis{}
	}

	// Convert PCM16 bytes to float64 samples
	samples := pcm16ToFloat64(audioData)
	if len(samples) < 320 { // Minimum 320 samples (20ms at 16kHz)
		return DeepfakeAnalysis{}
	}

	// Calculate 4 heuristic metrics
	pitchVar := calculatePitchVariance(samples)
	breathScore := detectBreathingGaps(samples)
	spectralFlat := calculateSpectralFlatness(samples)
	energyConsist := calculateEnergyConsistency(samples)

	// Combine metrics into a deepfake score (0-100)
	score := computeDeepfakeScore(pitchVar, breathScore, spectralFlat, energyConsist)

	analysis := DeepfakeAnalysis{
		Score:          score,
		PitchVariance:  pitchVar,
		BreathingScore: breathScore,
		SpectralFlat:   spectralFlat,
		EnergyConsist:  energyConsist,
		IsLikelyFake:   score > 70,
	}

	// Update rolling average
	dd.mu.Lock()
	dd.chunkScores = append(dd.chunkScores, score)
	dd.chunksAnalyzed++

	// Keep last 20 chunks for rolling average
	if len(dd.chunkScores) > 20 {
		dd.chunkScores = dd.chunkScores[len(dd.chunkScores)-20:]
	}

	total := 0
	for _, s := range dd.chunkScores {
		total += s
	}
	dd.rollingAverage = float64(total) / float64(len(dd.chunkScores))
	dd.mu.Unlock()

	return analysis
}

// GetRollingScore returns the cumulative rolling average deepfake score
func (dd *DeepfakeDetector) GetRollingScore() int {
	dd.mu.Lock()
	defer dd.mu.Unlock()
	return int(dd.rollingAverage)
}

// GetChunksAnalyzed returns the number of chunks analyzed
func (dd *DeepfakeDetector) GetChunksAnalyzed() int {
	dd.mu.Lock()
	defer dd.mu.Unlock()
	return dd.chunksAnalyzed
}

// pcm16ToFloat64 converts PCM16 little-endian bytes to normalized float64 samples
func pcm16ToFloat64(data []byte) []float64 {
	numSamples := len(data) / 2
	samples := make([]float64, numSamples)
	for i := 0; i < numSamples; i++ {
		sample := int16(binary.LittleEndian.Uint16(data[i*2 : i*2+2]))
		samples[i] = float64(sample) / 32768.0
	}
	return samples
}

// calculatePitchVariance estimates pitch variance using zero-crossing rate
// Natural speech: moderate variance. AI-generated: often too uniform or too erratic
func calculatePitchVariance(samples []float64) float64 {
	const frameSize = 320 // 20ms at 16kHz
	if len(samples) < frameSize*2 {
		return 0.5 // Insufficient data
	}

	numFrames := len(samples) / frameSize
	zeroCrossings := make([]float64, numFrames)

	for f := 0; f < numFrames; f++ {
		start := f * frameSize
		end := start + frameSize
		if end > len(samples) {
			break
		}

		crossings := 0
		for i := start + 1; i < end; i++ {
			if (samples[i-1] >= 0 && samples[i] < 0) || (samples[i-1] < 0 && samples[i] >= 0) {
				crossings++
			}
		}
		zeroCrossings[f] = float64(crossings)
	}

	// Calculate variance of zero-crossing rates
	if len(zeroCrossings) < 2 {
		return 0.5
	}

	mean := 0.0
	for _, zc := range zeroCrossings {
		mean += zc
	}
	mean /= float64(len(zeroCrossings))

	variance := 0.0
	for _, zc := range zeroCrossings {
		diff := zc - mean
		variance += diff * diff
	}
	variance /= float64(len(zeroCrossings) - 1)

	// Normalize: natural speech variance is typically 50-200
	// Very low (<30) or very high (>300) suggests synthetic
	normalizedVar := variance / 200.0
	if normalizedVar > 1.0 {
		normalizedVar = 1.0
	}

	return normalizedVar
}

// detectBreathingGaps checks for natural breathing pauses in audio
// Real humans breathe; AI-generated speech often lacks natural pauses
func detectBreathingGaps(samples []float64) float64 {
	const frameSize = 800 // 50ms at 16kHz
	silenceThreshold := 0.02

	numFrames := len(samples) / frameSize
	if numFrames < 4 {
		return 0.5
	}

	silentFrames := 0
	for f := 0; f < numFrames; f++ {
		start := f * frameSize
		end := start + frameSize
		if end > len(samples) {
			break
		}

		rms := 0.0
		for i := start; i < end; i++ {
			rms += samples[i] * samples[i]
		}
		rms = math.Sqrt(rms / float64(frameSize))

		if rms < silenceThreshold {
			silentFrames++
		}
	}

	// Natural speech typically has 10-30% silence (breathing gaps)
	// AI voice tends to have very few pauses or too many
	silenceRatio := float64(silentFrames) / float64(numFrames)
	return silenceRatio
}

// calculateSpectralFlatness measures how "flat" the frequency spectrum is
// AI-generated audio tends to have a flatter spectrum than natural speech
func calculateSpectralFlatness(samples []float64) float64 {
	if len(samples) < dftSize {
		return 0.5
	}

	// Apply Hamming window and cast to float32 for table-lookup DFT
	windowed := make([]float32, dftSize)
	for i := 0; i < dftSize; i++ {
		hamming := 0.54 - 0.46*math.Cos(2*math.Pi*float64(i)/float64(dftSize-1))
		windowed[i] = float32(samples[i] * hamming)
	}

	// Compute power spectrum using precomputed trig tables — O(N²/2) multiply-adds,
	// but no transcendental function calls in the inner loop (all lookups).
	halfN := dftSize / 2
	powerSpectrum := make([]float64, halfN)
	for k := 0; k < halfN; k++ {
		var re, im float32
		cosRow := &dftCos[k]
		sinRow := &dftSin[k]
		for n := 0; n < dftSize; n++ {
			w := windowed[n]
			re += w * cosRow[n]
			im -= w * sinRow[n]
		}
		powerSpectrum[k] = float64(re)*float64(re) + float64(im)*float64(im)
	}

	// Spectral flatness = geometric mean / arithmetic mean of power spectrum
	logSum := 0.0
	arithmeticSum := 0.0
	validBins := 0
	for _, p := range powerSpectrum {
		if p > 1e-10 { // Avoid log(0)
			logSum += math.Log(p)
			arithmeticSum += p
			validBins++
		}
	}

	if validBins == 0 || arithmeticSum == 0 {
		return 0.5
	}

	geometricMean := math.Exp(logSum / float64(validBins))
	arithmeticMean := arithmeticSum / float64(validBins)

	flatness := geometricMean / arithmeticMean
	if flatness > 1.0 {
		flatness = 1.0
	}

	return flatness
}

// calculateEnergyConsistency measures how consistent the audio energy is across frames
// AI-generated speech is often too consistent; natural speech varies more
func calculateEnergyConsistency(samples []float64) float64 {
	const frameSize = 480 // 30ms at 16kHz
	numFrames := len(samples) / frameSize
	if numFrames < 3 {
		return 0.5
	}

	energies := make([]float64, numFrames)
	for f := 0; f < numFrames; f++ {
		start := f * frameSize
		end := start + frameSize
		if end > len(samples) {
			break
		}

		energy := 0.0
		for i := start; i < end; i++ {
			energy += samples[i] * samples[i]
		}
		energies[f] = energy / float64(frameSize)
	}

	// Calculate coefficient of variation (CV)
	mean := 0.0
	for _, e := range energies {
		mean += e
	}
	mean /= float64(len(energies))

	if mean < 1e-10 {
		return 0.5 // Silence
	}

	variance := 0.0
	for _, e := range energies {
		diff := e - mean
		variance += diff * diff
	}
	variance /= float64(len(energies) - 1)

	cv := math.Sqrt(variance) / mean

	// Natural speech CV is typically 1.0-3.0
	// AI-generated tends to be <0.5 (too consistent)
	// Normalize to 0-1 range
	normalized := cv / 3.0
	if normalized > 1.0 {
		normalized = 1.0
	}

	return normalized
}

// computeDeepfakeScore combines all metrics into a single 0-100 score
func computeDeepfakeScore(pitchVar, breathScore, spectralFlat, energyConsist float64) int {
	score := 0.0

	// 1. Pitch variance analysis (25 points)
	// Too low (<0.1) or too high (>0.8) variance is suspicious
	if pitchVar < 0.1 {
		score += 25 * (1.0 - pitchVar/0.1) // Very low variance = suspicious
	} else if pitchVar > 0.8 {
		score += 25 * ((pitchVar - 0.8) / 0.2) // Very high variance = suspicious
	}

	// 2. Breathing gaps analysis (25 points)
	// Natural speech: 10-30% silence. No breathing = suspicious
	if breathScore < 0.05 {
		score += 25 // Almost no breathing pauses
	} else if breathScore < 0.10 {
		score += 15 // Very few pauses
	} else if breathScore > 0.50 {
		score += 10 // Too much silence (could be AI with pauses)
	}

	// 3. Spectral flatness (25 points)
	// Higher flatness = more likely synthetic
	if spectralFlat > 0.3 {
		score += 25 * math.Min((spectralFlat-0.3)/0.4, 1.0)
	}

	// 4. Energy consistency (25 points)
	// Very consistent energy (<0.15 normalized) = likely synthetic
	if energyConsist < 0.15 {
		score += 25 * (1.0 - energyConsist/0.15)
	}

	result := int(math.Round(score))
	if result > 100 {
		result = 100
	}
	if result < 0 {
		result = 0
	}

	if result > 50 {
		log.Printf("🎭 Deepfake analysis: score=%d, pitch=%.3f, breath=%.3f, spectral=%.3f, energy=%.3f",
			result, pitchVar, breathScore, spectralFlat, energyConsist)
	}

	return result
}
