using FraudGuardAI.Models;

namespace FraudGuardAI.Services
{
    /// <summary>
    /// Contract for fetching call-analysis history from the backend.
    /// </summary>
    public interface IHistoryService
    {
        Task<List<CallLog>> GetHistoryAsync(string? deviceId = null, int limit = 20, bool fraudOnly = false);
        Task<CallLog?> GetCallDetailAsync(int callId);
        Task<bool> TestConnectionAsync();
    }
}
