namespace FraudGuardAI.Services
{
    /// <summary>
    /// Abstraction over app configuration (server URL, device ID, feature flags).
    /// Breaks the tight coupling where services called SettingsPage static methods directly.
    /// </summary>
    public interface IAppSettings
    {
        /// <summary>Base HTTP URL, e.g. https://myserver.onrender.com</summary>
        string GetApiBaseUrl();

        /// <summary>WebSocket URL, e.g. wss://myserver.onrender.com/ws</summary>
        string GetWebSocketUrl();

        /// <summary>Unique device identifier saved in Preferences</summary>
        string GetDeviceId();

        /// <summary>Whether auto-protection should start on app launch</summary>
        bool IsAutoProtectionEnabled();
    }
}
