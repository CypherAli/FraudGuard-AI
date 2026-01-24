package services

// FraudDetectionConfig holds configurable parameters for fraud detection
type FraudDetectionConfig struct {
	// Alert thresholds (can be tuned based on real-world testing)
	CriticalThreshold int // Default: 90
	HighThreshold     int // Default: 70
	MediumThreshold   int // Default: 50
	LowThreshold      int // Default: 30

	// Keyword score multipliers (for fine-tuning sensitivity)
	CriticalMultiplier   float64 // Default: 1.0
	WarningMultiplier    float64 // Default: 1.0
	SuspiciousMultiplier float64 // Default: 1.0

	// Session settings
	MaxSessionDuration int  // Minutes, default: 60
	AutoResetScore     bool // Auto reset after threshold time, default: false
}

// DefaultFraudDetectionConfig returns the default configuration
// These values are optimized for Vietnamese fraud patterns
func DefaultFraudDetectionConfig() *FraudDetectionConfig {
	return &FraudDetectionConfig{
		// Thresholds - Can be adjusted based on testing
		CriticalThreshold: 90, // Very high confidence fraud
		HighThreshold:     70, // High confidence fraud
		MediumThreshold:   50, // Moderate suspicion
		LowThreshold:      30, // Low suspicion

		// Multipliers - Keep at 1.0 for now
		CriticalMultiplier:   1.0,
		WarningMultiplier:    1.0,
		SuspiciousMultiplier: 1.0,

		// Session settings
		MaxSessionDuration: 60,
		AutoResetScore:     false,
	}
}

// ConservativeConfig returns a more conservative configuration
// Use this if you're getting too many false positives
func ConservativeConfig() *FraudDetectionConfig {
	return &FraudDetectionConfig{
		CriticalThreshold:    100, // Harder to trigger
		HighThreshold:        85,
		MediumThreshold:      65,
		LowThreshold:         40,
		CriticalMultiplier:   0.8, // Reduce keyword scores
		WarningMultiplier:    0.8,
		SuspiciousMultiplier: 0.8,
		MaxSessionDuration:   60,
		AutoResetScore:       false,
	}
}

// AggressiveConfig returns a more aggressive configuration
// Use this if you're missing fraud cases
func AggressiveConfig() *FraudDetectionConfig {
	return &FraudDetectionConfig{
		CriticalThreshold:    80, // Easier to trigger
		HighThreshold:        60,
		MediumThreshold:      40,
		LowThreshold:         20,
		CriticalMultiplier:   1.2, // Increase keyword scores
		WarningMultiplier:    1.2,
		SuspiciousMultiplier: 1.2,
		MaxSessionDuration:   60,
		AutoResetScore:       false,
	}
}

// TuningGuide provides guidance for threshold tuning
const TuningGuide = `
FRAUD DETECTION THRESHOLD TUNING GUIDE
======================================

## Current Default Thresholds

- CRITICAL (≥90): Immediate danger, very high confidence
- HIGH (≥70): High risk, strong indicators
- MEDIUM (≥50): Moderate risk, some indicators
- LOW (≥30): Watch carefully, weak indicators

## How to Tune

### If Too Many False Positives (Crying Wolf):
1. Use ConservativeConfig()
2. Or increase thresholds:
   - CriticalThreshold: 90 → 100
   - HighThreshold: 70 → 85
   - MediumThreshold: 50 → 65

### If Missing Real Fraud Cases:
1. Use AggressiveConfig()
2. Or decrease thresholds:
   - CriticalThreshold: 90 → 80
   - HighThreshold: 70 → 60
   - MediumThreshold: 50 → 40

### Fine-Tuning Keyword Scores:
- Adjust multipliers (0.5 - 2.0 range)
- Or modify keyword scores in initializeKeywordMatcher()

## Testing Methodology

1. Collect real call data (100+ samples)
2. Label as fraud/not-fraud manually
3. Run through detector
4. Calculate metrics:
   - Precision = True Positives / (True Positives + False Positives)
   - Recall = True Positives / (True Positives + False Negatives)
   - F1 Score = 2 * (Precision * Recall) / (Precision + Recall)
5. Adjust thresholds to optimize F1 score

## Recommended Approach for Hackathon

Start with DEFAULT config, then:
- Day 1-2: Collect feedback from demos
- Day 3: Adjust if needed based on feedback
- Keep it simple - don't over-tune!

## Production Recommendations

- A/B test different configs
- Monitor alert rates
- Collect user feedback on accuracy
- Retrain thresholds monthly
`
