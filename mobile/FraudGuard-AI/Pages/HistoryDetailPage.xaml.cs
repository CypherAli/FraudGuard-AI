using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using FraudGuardAI.Models;
using FraudGuardAI.Services;
using FraudGuardAI.Localization;

namespace FraudGuardAI.Pages
{
    [QueryProperty(nameof(CallLogId), "id")]
    public partial class HistoryDetailPage : ContentPage
    {
        private int _callLogId;
        private CallLog? _callLog;
        private readonly IHistoryService _historyService;

        public int CallLogId
        {
            get => _callLogId;
            set
            {
                _callLogId = value;
                _ = LoadDetailAsync();
            }
        }

        public HistoryDetailPage(IHistoryService historyService)
        {
            InitializeComponent();
            _historyService = historyService;
        }

        private async Task LoadDetailAsync()
        {
            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;

                _callLog = await _historyService.GetCallDetailAsync(_callLogId);

                if (_callLog == null)
                {
                    await DisplayAlert(T("Common_Error"), T("Detail_NotFound"), T("Common_OK"));
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                MainThread.BeginInvokeOnMainThread(() => PopulateUI());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Detail] Error: {ex.Message}");
                await DisplayAlert(T("Common_Error"), ex.Message, T("Common_OK"));
                await Shell.Current.GoToAsync("..");
            }
            finally
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LoadingIndicator.IsRunning = false;
                    LoadingIndicator.IsVisible = false;
                });
            }
        }

        private void PopulateUI()
        {
            var log = _callLog;
            if (log == null) return;

            // Risk Score
            RiskScoreLabel.Text = $"{log.RiskScore}%";
            RiskCircle.BackgroundColor = log.RiskScoreColor;
            RiskCircle.Stroke = new SolidColorBrush(log.RiskScoreColor);

            // Alert Type Badge
            AlertTypeLabel.Text = log.AlertTypeDisplay;
            AlertBadge.BackgroundColor = log.RiskScoreColor;

            // Info row
            DurationLabel.Text = log.DurationDisplay;
            TimestampLabel.Text = log.StartTime.ToString("HH:mm");
            DeepfakeLabel.Text = log.DeepfakeScore > 0 ? $"{log.DeepfakeScore}%" : "N/A";

            RiskCard.IsVisible = true;

            // Transcript
            if (!string.IsNullOrEmpty(log.Transcript))
            {
                TranscriptLabel.Text = log.Transcript;
                TranscriptCard.IsVisible = true;
            }

            // Keywords
            var keywords = log.DetectedKeywords;
            if (keywords.Count > 0)
            {
                KeywordsContainer.Children.Clear();
                foreach (var kw in keywords)
                {
                    var badgeColor = kw.StartsWith("CRITICAL")  ? Color.FromArgb("#D32F2F")
                        : kw.StartsWith("WARNING")              ? Color.FromArgb("#FF9800")
                        : kw.StartsWith("SUSPICIOUS")           ? Color.FromArgb("#FBBF24")
                        : Color.FromArgb("#4CAF50");

                    var badge = new Border
                    {
                        BackgroundColor = badgeColor,
                        Padding = new Thickness(10, 5),
                        Margin = new Thickness(0, 0, 6, 6),
                        StrokeThickness = 0,
                        StrokeShape = new RoundRectangle { CornerRadius = 8 },
                        Content = new Label
                        {
                            Text = kw,
                            FontSize = 11,
                            TextColor = Colors.White
                        }
                    };
                    KeywordsContainer.Children.Add(badge);
                }
                KeywordsCard.IsVisible = true;
            }

            // Evidence
            if (!string.IsNullOrEmpty(log.Evidence))
            {
                EvidenceLabel.Text = log.Evidence;
                EvidenceCard.IsVisible = true;
            }

            ShareButton.IsVisible = true;
        }

        private async void OnBackClicked(object sender, EventArgs e)
            => await Shell.Current.GoToAsync("..");

        private async void OnShareClicked(object sender, EventArgs e)
        {
            if (_callLog == null) return;

            try
            {
                string shareText = $"FraudGuard AI Report\n" +
                    $"Risk Score: {_callLog.RiskScore}/100 ({_callLog.AlertTypeDisplay})\n" +
                    $"Time: {_callLog.TimeDisplay}\n" +
                    $"Duration: {_callLog.DurationDisplay}\n" +
                    $"Fraud: {(_callLog.IsFraud ? "Yes" : "No")}\n";

                if (!string.IsNullOrEmpty(_callLog.Transcript))
                    shareText += $"\nTranscript:\n{_callLog.Transcript}\n";

                if (_callLog.DetectedKeywords.Count > 0)
                    shareText += $"\nPatterns: {string.Join(", ", _callLog.DetectedKeywords)}\n";

                shareText += "\nGenerated by FraudGuard AI";

                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Text = shareText,
                    Title = "Fraud Report"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Detail] Share error: {ex.Message}");
            }
        }

        private static string T(string key) => LocalizationResourceManager.Instance[key];
    }
}
