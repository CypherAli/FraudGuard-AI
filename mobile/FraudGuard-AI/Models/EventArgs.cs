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
