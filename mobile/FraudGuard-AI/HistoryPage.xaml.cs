using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using FraudGuardAI.Models;
using FraudGuardAI.Services;
using FraudGuardAI.Localization;

namespace FraudGuardAI.Pages
{
    public partial class HistoryPage : ContentPage
    {
        #region Fields

        private readonly IHistoryService _historyService;
        private readonly IAppSettings _settings;
        private ObservableCollection<CallLog> _callLogs;
        private bool _isRefreshing;
        private Command? _refreshCommand;

        #endregion

        #region Properties

        public ObservableCollection<CallLog> CallLogs
        {
            get => _callLogs;
            set
            {
                _callLogs = value;
                OnPropertyChanged();
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                _isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public Command RefreshCommand
        {
            get
            {
                _refreshCommand ??= new Command(async () => await RefreshAsync());
                return _refreshCommand;
            }
        }

        #endregion

        #region Constructor

        public HistoryPage(IHistoryService historyService, IAppSettings settings)
        {
            InitializeComponent();

            _historyService = historyService;
            _settings = settings;
            _callLogs = new ObservableCollection<CallLog>();
            HistoryCollectionView.ItemsSource = _callLogs;

            BindingContext = this;
        }

        #endregion

        #region Lifecycle

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadHistoryAsync();
        }

        #endregion

        #region Data Loading

        private async Task LoadHistoryAsync()
        {
            try
            {
                ShowLoading(true);
                HideError();
                HideEmptyState();

                string deviceId = _settings.GetDeviceId();

                // Add timeout for history request
                var historyTask = _historyService.GetHistoryAsync(deviceId, limit: 50);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(8));
                var completedTask = await Task.WhenAny(historyTask, timeoutTask);

                List<CallLog> history;
                if (completedTask == historyTask)
                {
                    history = await historyTask;
                }
                else
                {
                    throw new TimeoutException(T("History_TimeoutError"));
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _callLogs.Clear();

                    if (history != null && history.Count > 0)
                    {
                        foreach (var log in history)
                            _callLogs.Add(log);

                        if (SubtitleLabel != null)
                            SubtitleLabel.Text = string.Format(T("History_AnalyzedCallsFormat"), history.Count);
                    }
                    else
                    {
                        ShowEmptyState();
                    }

                    ShowLoading(false);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[History] Error: {ex.Message}");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ShowLoading(false);
                    ShowError(ex.Message);
                });
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        #endregion

        #region UI State Management

        private void ShowLoading(bool show)
        {
            LoadingIndicator.IsRunning = show;
            LoadingIndicator.IsVisible = show;
            HistoryCollectionView.IsVisible = !show;
        }

        private void ShowEmptyState()
        {
            EmptyStateView.IsVisible = true;
            HistoryCollectionView.IsVisible = false;
            ErrorView.IsVisible = false;
        }

        private void HideEmptyState() => EmptyStateView.IsVisible = false;

        private void ShowError(string message)
        {
            ErrorView.IsVisible = true;
            ErrorMessageLabel.Text = message.Length > 50 ? "Connection error" : message;
            HistoryCollectionView.IsVisible = false;
            EmptyStateView.IsVisible = false;
        }

        private void HideError() => ErrorView.IsVisible = false;

        #endregion

        #region Event Handlers

        private async void OnRetryClicked(object sender, EventArgs e)
            => await LoadHistoryAsync();

        private async void OnCallLogTapped(object sender, EventArgs e)
        {
            if (sender is BindableObject bindable && bindable.BindingContext is CallLog log)
                await Shell.Current.GoToAsync($"HistoryDetailPage?id={log.Id}");
        }

        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadHistoryAsync();
        }

        #endregion

        #region Public Methods

        public async Task ReloadAsync() => await LoadHistoryAsync();

        #endregion

        #region Helpers

        private static string T(string key) => LocalizationResourceManager.Instance[key];

        #endregion
    }
}
