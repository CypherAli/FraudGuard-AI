namespace FraudGuardAI.Models;

/// <summary>
/// Dashboard statistics for the main protection page
/// Real data loaded from API, no test/dummy values
/// </summary>
public class DashboardStats
{
    public int BlockedTotal { get; set; } = 0;
    public int BlockedToday { get; set; } = 0;
    public int SeriousThreats { get; set; } = 0;
    public double ProtectionEfficiency { get; set; } = 0;
    public int WeeklyChange { get; set; } = 0;
    public double EfficiencyChange { get; set; } = 0;
    public bool IsProtectionActive { get; set; } = false;
    
    public string BlockedTotalDisplay => BlockedTotal.ToString();
    public string BlockedTodayDisplay => BlockedToday.ToString();
    public string ThreatsDisplay => SeriousThreats.ToString();
    public string EfficiencyDisplay => $"{ProtectionEfficiency:F1}%";
    public string WeeklyChangeDisplay => $"+{WeeklyChange} tuần này";
    public string EfficiencyChangeDisplay => $"+{EfficiencyChange:F1}%";
}
