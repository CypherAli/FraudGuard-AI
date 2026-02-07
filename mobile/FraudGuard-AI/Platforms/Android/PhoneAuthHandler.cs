using Firebase.Auth;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Java.Util.Concurrent;
using AndroidApp = Android.App;
using Firebase;

namespace FraudGuardAI.Platforms.Android
{
    /// <summary>Android Phone Authentication Handler</summary>
    public class PhoneAuthHandler
    {
        private static PhoneAuthProvider.OnVerificationStateChangedCallbacks? _callbacks;
        private static TaskCompletionSource<string>? _verificationTcs;
        private static string? _verificationId;

        /// <summary>Send OTP with Android callbacks</summary>
        public static Task<string> SendOtpAsync(string phoneNumber, AndroidApp.Activity activity)
        {
            _verificationTcs = new TaskCompletionSource<string>();
            _verificationId = null;

            _callbacks = new PhoneAuthCallbacks(
                onCodeSent: (verificationId, token) =>
                {
                    Debug.WriteLine($"[PhoneAuth] onCodeSent: {verificationId}");
                    _verificationId = verificationId;
                    _verificationTcs?.TrySetResult(verificationId);
                },
                onVerificationCompleted: (credential) =>
                {
                    Debug.WriteLine("[PhoneAuth] onVerificationCompleted - Auto-verification");
                    // Auto-verification happened (SMS auto-retrieval)
                    var code = credential.SmsCode;
                    if (!string.IsNullOrEmpty(code))
                    {
                        Debug.WriteLine($"[PhoneAuth] Auto-retrieved code: {code}");
                    }
                },
                onVerificationFailed: (exception) =>
                {
                    Debug.WriteLine($"[PhoneAuth] onVerificationFailed: {exception.Message}");
                    _verificationTcs?.TrySetException(exception);
                }
            );

            var options = PhoneAuthOptions.NewBuilder()
                .SetPhoneNumber(phoneNumber)
                .SetTimeout(new Java.Lang.Long(60), TimeUnit.Seconds)
                .SetActivity(activity)
                .SetCallbacks(_callbacks)
                .Build();

            PhoneAuthProvider.VerifyPhoneNumber(options);

            return _verificationTcs.Task;
        }

        /// <summary>Get stored verification ID</summary>
        public static string? GetVerificationId() => _verificationId;

        /// <summary>Create credential from verification ID and code</summary>
        public static PhoneAuthCredential GetCredential(string verificationId, string code)
            => PhoneAuthProvider.GetCredential(verificationId, code);

        private class PhoneAuthCallbacks : PhoneAuthProvider.OnVerificationStateChangedCallbacks
        {
            private readonly Action<string, PhoneAuthProvider.ForceResendingToken> _onCodeSent;
            private readonly Action<PhoneAuthCredential> _onVerificationCompleted;
            private readonly Action<FirebaseException> _onVerificationFailed;

            public PhoneAuthCallbacks(
                Action<string, PhoneAuthProvider.ForceResendingToken> onCodeSent,
                Action<PhoneAuthCredential> onVerificationCompleted,
                Action<FirebaseException> onVerificationFailed)
            {
                _onCodeSent = onCodeSent;
                _onVerificationCompleted = onVerificationCompleted;
                _onVerificationFailed = onVerificationFailed;
            }

            public override void OnCodeSent(string verificationId, PhoneAuthProvider.ForceResendingToken token)
            {
                base.OnCodeSent(verificationId, token);
                _onCodeSent?.Invoke(verificationId, token);
            }

            public override void OnVerificationCompleted(PhoneAuthCredential credential)
            {
                _onVerificationCompleted?.Invoke(credential);
            }

            public override void OnVerificationFailed(FirebaseException exception)
            {
                _onVerificationFailed?.Invoke(exception);
            }
        }
    }
}
