using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using FraudGuardAI.Models;
using FraudGuardAI.Services;

namespace FraudGuardAI
{
    public partial class HistoryPage : ContentPage
    {
        #region Fields

        private HistoryService _historyService;
        private ObservableCollection<CallLog> _callLogs;
        private bool _isRefreshing;

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

        #endregion

        #region Constructor

        public HistoryPage()
        {
            InitializeComponent();
            
            _callLogs = new ObservableCollection<CallLog>();
            HistoryCollectionView.ItemsSource = _callLogs;
            _historyService = new HistoryService();
            
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

                string deviceId = SettingsPage.GetDeviceID();
                
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
                    throw new TimeoutException("Server không phản hồi sau 8 giây");
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _callLogs.Clear();
                    
                    if (history != null && history.Count > 0)
                    {
                        foreach (var log in history)
                        {
                            _callLogs.Add(log);
                        }
                        SubtitleLabel.Text = $"{history.Count} analyzed calls";
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

        private void HideEmptyState()
        {
            EmptyStateView.IsVisible = false;
        }

        private void ShowError(string message)
        {
            ErrorView.IsVisible = true;
            ErrorMessageLabel.Text = message.Length > 50 ? "Connection error" : message;
            HistoryCollectionView.IsVisible = false;
            EmptyStateView.IsVisible = false;
        }

        private void HideError()
        {
            ErrorView.IsVisible = false;
        }

        #endregion

        #region Event Handlers

        private async void OnRetryClicked(object sender, EventArgs e)
        {
            await LoadHistoryAsync();
        }

        public async Task RefreshCommand()
        {
            IsRefreshing = true;
            await LoadHistoryAsync();
        }

        #endregion

        #region Public Methods

        public async Task ReloadAsync()
        {
            await LoadHistoryAsync();
        }

        #endregion
    }
}
