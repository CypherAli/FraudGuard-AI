namespace FraudGuardAI.Models;

/// <summary>
/// Dashboard statistics for the main protection page
/// </summary>
public class DashboardStats
{
    public int BlockedTotal { get; set; }
    public int BlockedToday { get; set; }
    public int SeriousThreats { get; set; }
    public double ProtectionEfficiency { get; set; } = 98.5;
    public int WeeklyChange { get; set; } = 12;
    public double EfficiencyChange { get; set; } = 2.3;
    public bool IsProtectionActive { get; set; } = true;
    
    public string BlockedTotalDisplay => BlockedTotal.ToString();
    public string BlockedTodayDisplay => BlockedToday.ToString();
    public string ThreatsDisplay => SeriousThreats.ToString();
    public string EfficiencyDisplay => $"{ProtectionEfficiency:F1}%";
    public string WeeklyChangeDisplay => $"+{WeeklyChange} tuần này";
    public string EfficiencyChangeDisplay => $"+{EfficiencyChange:F1}%";
}
