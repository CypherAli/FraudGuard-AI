package services

import (
	"os"
	"regexp"
	"strings"
)

var dataMaskingEnabled = os.Getenv("FEATURE_DATA_MASK") == "true"

// Compiled regex patterns for sensitive data
var (
	// Credit card numbers: 13-19 digits with optional spaces/dashes
	creditCardPattern = regexp.MustCompile(`\b(?:\d[ -]*?){13,19}\b`)

	// OTP codes: standalone 4-6 digit codes (with context like "mã", "otp", "code")
	otpPattern = regexp.MustCompile(`(?i)(mã|otp|code|xác nhận|xác thực)[\s:]*(\d{4,6})\b`)

	// Vietnamese bank account numbers: 8-19 digits (common format)
	bankAccountPattern = regexp.MustCompile(`(?i)(tài khoản|tk|stk|số tài khoản)[\s:]*(\d{8,19})\b`)

	// Vietnamese ID numbers: CCCD (12 digits) / CMND (9-12 digits)
	idNumberPattern = regexp.MustCompile(`(?i)(cccd|cmnd|căn cước|chứng minh)[\s:]*(\d{9,12})\b`)

	// Phone numbers: Vietnamese format (10-11 digits, starting with 0 or +84)
	phonePattern = regexp.MustCompile(`(?:\+84|0)\d{9,10}\b`)
)

// MaskSensitiveData replaces sensitive information with masked placeholders
// Only applied at save time, not during real-time fraud analysis
func MaskSensitiveData(text string) string {
	if !dataMaskingEnabled {
		return text
	}

	masked := text

	// Mask credit card numbers
	masked = creditCardPattern.ReplaceAllString(masked, "***CARD***")

	// Mask OTP codes (preserve the label, mask the number)
	masked = otpPattern.ReplaceAllString(masked, "${1} ***OTP***")

	// Mask bank account numbers
	masked = bankAccountPattern.ReplaceAllString(masked, "${1} ***ACCOUNT***")

	// Mask ID numbers (CCCD/CMND)
	masked = idNumberPattern.ReplaceAllString(masked, "${1} ***ID***")

	// Mask phone numbers (keep first 3 chars for context)
	masked = phonePattern.ReplaceAllStringFunc(masked, func(match string) string {
		if len(match) > 3 {
			return match[:3] + strings.Repeat("*", len(match)-3)
		}
		return "***PHONE***"
	})

	return masked
}
