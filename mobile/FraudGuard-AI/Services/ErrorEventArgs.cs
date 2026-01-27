using System;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Event arguments for error events
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception? Exception { get; }

        public ErrorEventArgs(string message, Exception? exception = null)
        {
            Message = message;
            Exception = exception;
        }
    }
}
