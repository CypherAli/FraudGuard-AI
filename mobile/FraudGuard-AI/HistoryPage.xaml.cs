using FraudGuardAI.Models;
using FraudGuardAI.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

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
            
            // Initialize service
            _historyService = new HistoryService();
            
            BindingContext = this;
        }

        #endregion

        #region Lifecycle

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Load history when page appears
            await LoadHistoryAsync();
        }

        #endregion

        #region Data Loading

        private async Task LoadHistoryAsync()
        {
            try
            {
                // Show loading indicator
                ShowLoading(true);
                HideError();
                HideEmptyState();

                System.Diagnostics.Debug.WriteLine("[HistoryPage] Loading call history...");

                // Get device ID from Settings
                string deviceId = SettingsPage.GetDeviceID();

                // Fetch data from API
                var history = await _historyService.GetHistoryAsync(deviceId, limit: 50);

                // Update UI on main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _callLogs.Clear();
                    
                    if (history != null && history.Count > 0)
                    {
                        foreach (var log in history)
                        {
                            _callLogs.Add(log);
                        }
                        
                        SubtitleLabel.Text = $"Tìm thấy {history.Count} cuộc gọi";
                        System.Diagnostics.Debug.WriteLine($"[HistoryPage] Loaded {history.Count} call logs");
                    }
                    else
                    {
                        ShowEmptyState();
                        System.Diagnostics.Debug.WriteLine("[HistoryPage] No call logs found");
                    }
                    
                    ShowLoading(false);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HistoryPage] Error loading history: {ex.Message}");
                
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
            ErrorMessageLabel.Text = message;
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

        // Pull-to-refresh handler
        public async Task RefreshCommand()
        {
            IsRefreshing = true;
            await LoadHistoryAsync();
        }

        #endregion

        #region Public Methods



        /// <summary>
        /// Reload history data
        /// </summary>
        public async Task ReloadAsync()
        {
            await LoadHistoryAsync();
        }

        #endregion
    }
}
