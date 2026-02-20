filepath = '/e/FraudGuard-AI/services/api-gateway/internal/services/fraud_detector.go'
with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

errors = []

# FIX 1a: Add LastAlertTime to SessionState
tag = 'LastAlertTime     time.Time // For alert cooldown enforcement'
if tag not in content:
    old = '	AlertsSent        int\n}'
    new = '	AlertsSent        int\n	LastAlertTime     time.Time // For alert cooldown enforcement\n}'
    if old in content:
        content = content.replace(old, new, 1)
    else:
        errors.append("FIX1a: SessionState end not found")

# FIX 1b: Add spam guards before switch
tag = 'alertsExhausted'
if tag not in content:
    old = '\t// Determine alert level based on accumulated score\n\tswitch {'
    new = '''\t// Determine alert level based on accumulated score
\t// Spam guards: respect MaxAlertsPerSession and AlertCooldown
\talertsExhausted := fd.session.AlertsSent >= fd.config.MaxAlertsPerSession
\tcooldownActive := fd.config.AlertCooldownMs > 0 &&
\t\t!fd.session.LastAlertTime.IsZero() &&
\t\ttime.Since(fd.session.LastAlertTime) < time.Duration(fd.config.AlertCooldownMs)*time.Millisecond

\tswitch {'''
    if old in content:
        content = content.replace(old, new, 1)
    else:
        errors.append("FIX1b: switch prefix not found")

# FIX 1c: CRITICAL case - add guard
if 'alertsExhausted' in content:
    old = '''\tcase currentScore >= fd.config.CriticalThreshold:
\t\tresult.IsAlert = true
\t\tresult.Action = "CRITICAL"
\t\tresult.Message = fmt.Sprintf("C\u1ea2NH B\u00c1O NGHI\u00caM TR\u1eccNG: Ph\u00e1t hi\u1ec7n d\u1ea5u hi\u1ec7u l\u1eeba \u0111\u1ea3o r\u1ea5t cao! (\u0110i\u1ec3m r\u1ee7i ro: %d/100)", currentScore)
\t\tfd.session.AlertsSent++
\t\tfd.alertCount++
\t\tlog.Printf("[%s] CRITICAL ALERT: Score=%d, Patterns=%v", fd.deviceID, currentScore, patterns)'''
    new = '''\tcase currentScore >= fd.config.CriticalThreshold:
\t\tresult.Action = "CRITICAL"
\t\tresult.Message = fmt.Sprintf("C\u1ea2NH B\u00c1O NGHI\u00caM TR\u1eccNG: Ph\u00e1t hi\u1ec7n d\u1ea5u hi\u1ec7u l\u1eeba \u0111\u1ea3o r\u1ea5t cao! (\u0110i\u1ec3m r\u1ee7i ro: %d/100)", currentScore)
\t\tif !alertsExhausted && !cooldownActive {
\t\t\tresult.IsAlert = true
\t\t\tfd.session.AlertsSent++
\t\t\tfd.session.LastAlertTime = time.Now()
\t\t\tfd.alertCount++
\t\t\tlog.Printf("[%s] CRITICAL ALERT: Score=%d, Patterns=%v", fd.deviceID, currentScore, patterns)
\t\t} else {
\t\t\tlog.Printf("[%s] CRITICAL suppressed (exhausted=%v cooldown=%v)", fd.deviceID, alertsExhausted, cooldownActive)
\t\t}'''
    if old in content:
        content = content.replace(old, new, 1)
    else:
        errors.append("FIX1c CRITICAL: case not found")

# HIGH case
    old = '''\tcase currentScore >= fd.config.HighThreshold:
\t\tresult.IsAlert = true
\t\tresult.Action = "HIGH"
\t\tresult.Message = fmt.Sprintf("C\u1ea2NH B\u00c1O CAO: Cu\u1ed9c g\u1ecdi c\u00f3 d\u1ea5u hi\u1ec7u \u0111\u00e1ng ng\u1edd! (\u0110i\u1ec3m r\u1ee7i ro: %d/100)", currentScore)
\t\tfd.session.AlertsSent++
\t\tfd.alertCount++
\t\tlog.Printf("[%s] HIGH ALERT: Score=%d", fd.deviceID, currentScore)'''
    new = '''\tcase currentScore >= fd.config.HighThreshold:
\t\tresult.Action = "HIGH"
\t\tresult.Message = fmt.Sprintf("C\u1ea2NH B\u00c1O CAO: Cu\u1ed9c g\u1ecdi c\u00f3 d\u1ea5u hi\u1ec7u \u0111\u00e1ng ng\u1edd! (\u0110i\u1ec3m r\u1ee7i ro: %d/100)", currentScore)
\t\tif !alertsExhausted && !cooldownActive {
\t\t\tresult.IsAlert = true
\t\t\tfd.session.AlertsSent++
\t\t\tfd.session.LastAlertTime = time.Now()
\t\t\tfd.alertCount++
\t\t\tlog.Printf("[%s] HIGH ALERT: Score=%d", fd.deviceID, currentScore)
\t\t} else {
\t\t\tlog.Printf("[%s] HIGH suppressed (exhausted=%v cooldown=%v)", fd.deviceID, alertsExhausted, cooldownActive)
\t\t}'''
    if old in content:
        content = content.replace(old, new, 1)
    else:
        errors.append("FIX1c HIGH: case not found")

# MEDIUM case
    old = '''\tcase currentScore >= fd.config.MediumThreshold:
\t\tresult.IsAlert = true
\t\tresult.Action = "MEDIUM"
\t\tresult.Message = fmt.Sprintf("C\u1ea2NH B\u00c1O: Ph\u00e1t hi\u1ec7n m\u1ed9t s\u1ed1 d\u1ea5u hi\u1ec7u b\u1ea5t th\u01b0\u1eddng (\u0110i\u1ec3m r\u1ee7i ro: %d/100)", currentScore)
\t\tfd.session.AlertsSent++
\t\tfd.alertCount++'''
    new = '''\tcase currentScore >= fd.config.MediumThreshold:
\t\tresult.Action = "MEDIUM"
\t\tresult.Message = fmt.Sprintf("C\u1ea2NH B\u00c1O: Ph\u00e1t hi\u1ec7n m\u1ed9t s\u1ed1 d\u1ea5u hi\u1ec7u b\u1ea5t th\u01b0\u1eddng (\u0110i\u1ec3m r\u1ee7i ro: %d/100)", currentScore)
\t\tif !alertsExhausted && !cooldownActive {
\t\t\tresult.IsAlert = true
\t\t\tfd.session.AlertsSent++
\t\t\tfd.session.LastAlertTime = time.Now()
\t\t\tfd.alertCount++
\t\t} else {
\t\t\tlog.Printf("[%s] MEDIUM suppressed (exhausted=%v cooldown=%v)", fd.deviceID, alertsExhausted, cooldownActive)
\t\t}'''
    if old in content:
        content = content.replace(old, new, 1)
    else:
        errors.append("FIX1c MEDIUM: case not found")

# FIX 2: Clamp AccumulatedScore after Gemini boost
tag = 'Clamp so AccumulatedScore never exceeds 100'
if tag not in content:
    old = '''\t\t\tfd.mu.Lock()
\t\t\tif fd.session.SessionID == sessionID {
\t\t\t\tfd.session.AccumulatedScore += geminiBoost
\t\t\t\tfd.session.DetectedPatterns = append(fd.session.DetectedPatterns,'''
    new = '''\t\t\tfd.mu.Lock()
\t\t\tif fd.session.SessionID == sessionID {
\t\t\t\tfd.session.AccumulatedScore += geminiBoost
\t\t\t\t// Clamp so AccumulatedScore never exceeds 100
\t\t\t\tif fd.session.AccumulatedScore > 100 {
\t\t\t\t\tfd.session.AccumulatedScore = 100
\t\t\t\t}
\t\t\t\tfd.session.DetectedPatterns = append(fd.session.DetectedPatterns,'''
    if old in content:
        content = content.replace(old, new, 1)
    else:
        errors.append("FIX2: Gemini boost clamp not found")

# FIX 5: Gemini alert uses current AccumulatedScore not stale capturedScore
tag = 'Use the updated AccumulatedScore'
if tag not in content:
    old = '''\t\t\tif alertCallback != nil {
\t\t\t\tcombinedScore := capturedScore + geminiBoost
\t\t\t\talertType := "MEDIUM"
\t\t\t\tif combinedScore >= 80 || aiResult.RiskScore >= 80 {
\t\t\t\t\talertType = "CRITICAL"
\t\t\t\t} else if combinedScore >= 60 || aiResult.RiskScore >= 60 {
\t\t\t\t\talertType = "HIGH"
\t\t\t\t}'''
    new = '''\t\t\tif alertCallback != nil {
\t\t\t\t// Use current AccumulatedScore (already includes geminiBoost) for accurate alertType
\t\t\t\tfd.mu.RLock()
\t\t\t\tcurrentAccumulated := fd.session.AccumulatedScore
\t\t\t\tfd.mu.RUnlock()

\t\t\t\talertType := "MEDIUM"
\t\t\t\tif currentAccumulated >= 80 || aiResult.RiskScore >= 80 {
\t\t\t\t\talertType = "CRITICAL"
\t\t\t\t} else if currentAccumulated >= 60 || aiResult.RiskScore >= 60 {
\t\t\t\t\talertType = "HIGH"
\t\t\t\t}'''
    if old in content:
        content = content.replace(old, new, 1)
    else:
        errors.append("FIX5: Gemini stale score not found")

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)

if errors:
    print("ERRORS:", errors)
else:
    print("ALL FIXES APPLIED OK")
