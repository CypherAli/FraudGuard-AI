using System;

namespace FraudGuardAI.Models
{
    /// <summary>
    /// Event arguments for alert received events
    /// </summary>
    public class AlertEventArgs : EventArgs
    {
        public AlertData Alert { get; }

        public AlertEventArgs(AlertData alert)
        {
            Alert = alert;
        }
    }

    /// <summary>
    /// Event arguments for pending report — AI muốn report nhưng cần user xác nhận
    /// </summary>
    public class PendingReportEventArgs : EventArgs
    {
        public string PhoneNumber { get; }
        public string Reason      { get; }
        public double Confidence  { get; }

        public PendingReportEventArgs(string phoneNumber, string reason, double confidence)
        {
            PhoneNumber = phoneNumber;
            Reason      = reason;
            Confidence  = confidence;
        }
    }

    /// <summary>
    /// Event arguments for connection status changes
    /// </summary>
    public class ConnectionStatusEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string Message { get; }

        public ConnectionStatusEventArgs(bool isConnected, string message)
        {
            IsConnected = isConnected;
            Message = message;
        }
    }
}
