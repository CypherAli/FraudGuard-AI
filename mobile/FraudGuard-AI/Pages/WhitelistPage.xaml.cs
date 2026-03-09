using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using FraudGuardAI.Services;
using FraudGuardAI.Localization;

namespace FraudGuardAI.Pages
{
    public partial class WhitelistPage : ContentPage
    {
        private readonly ContactWhitelistService _whitelistService;
        private ObservableCollection<string> _allNumbers;
        private ObservableCollection<string> _filteredNumbers;

        public WhitelistPage()
        {
            InitializeComponent();
            _whitelistService = ContactWhitelistService.Instance;
            _allNumbers = new ObservableCollection<string>();
            _filteredNumbers = new ObservableCollection<string>();
            WhitelistCollectionView.ItemsSource = _filteredNumbers;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadWhitelist();
        }

        private void LoadWhitelist()
        {
            var numbers = _whitelistService.GetAll().OrderBy(n => n).ToList();
            _allNumbers.Clear();
            _filteredNumbers.Clear();
            foreach (var n in numbers)
            {
                _allNumbers.Add(n);
                _filteredNumbers.Add(n);
            }
            UpdateUI();
        }

        private void UpdateUI()
        {
            CountLabel.Text = _allNumbers.Count.ToString();
            EmptyStateView.IsVisible = _filteredNumbers.Count == 0;
            WhitelistCollectionView.IsVisible = _filteredNumbers.Count > 0;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var query = e.NewTextValue?.Trim().ToLower() ?? "";
            _filteredNumbers.Clear();
            var filtered = string.IsNullOrEmpty(query)
                ? _allNumbers
                : new ObservableCollection<string>(_allNumbers.Where(n => n.Contains(query)));
            foreach (var n in filtered)
                _filteredNumbers.Add(n);
            UpdateUI();
        }

        private async void OnAddNumberClicked(object sender, EventArgs e)
        {
            var number = await DisplayPromptAsync(
                T("Whitelist_AddNumber"),
                T("Whitelist_AddPromptMessage"),
                T("Common_OK"),
                T("Common_Cancel"),
                placeholder: "0912345678",
                keyboard: Keyboard.Telephone);

            if (string.IsNullOrWhiteSpace(number)) return;

            var trimmed = number.Trim();

            // Validate: only digits, optional leading +
            if (!IsValidPhoneNumber(trimmed))
            {
                await DisplayAlert(T("Common_Error"), T("Whitelist_InvalidNumber"), T("Common_OK"));
                return;
            }

            // Check for duplicates
            if (_allNumbers.Contains(trimmed))
            {
                await DisplayAlert(T("Common_Error"), T("Whitelist_DuplicateNumber"), T("Common_OK"));
                return;
            }

            await _whitelistService.AddNumberAsync(trimmed);
            LoadWhitelist();
        }

        private static bool IsValidPhoneNumber(string number)
        {
            if (string.IsNullOrWhiteSpace(number)) return false;
            // Allow optional leading + for international format, rest must be digits
            var digits = number.StartsWith("+") ? number.Substring(1) : number;
            return digits.Length >= 7 && digits.Length <= 15 && digits.All(char.IsDigit);
        }

        private async void OnImportContactsClicked(object sender, EventArgs e)
        {
            try
            {
                int count = await _whitelistService.ImportFromContactsAsync();
                LoadWhitelist();
                await DisplayAlert(
                    T("Whitelist_ImportContacts"),
                    string.Format(T("Whitelist_ImportedFormat"), count),
                    T("Common_OK"));
            }
            catch (Exception ex)
            {
                // Reload UI even on partial failure — import may have added some
                // contacts before throwing (e.g. permission revoked mid-import).
                // Without this call the list shows stale data even though the DB
                // already contains the successfully-imported entries.
                LoadWhitelist();
                await DisplayAlert(T("Common_Error"), ex.Message, T("Common_OK"));
            }
        }

        private async void OnClearAllClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                T("Whitelist_ClearAll"),
                T("Whitelist_ClearConfirmMessage"),
                T("Common_OK"),
                T("Common_Cancel"));

            if (!confirm) return;

            await _whitelistService.ClearAllAsync();
            LoadWhitelist();
        }

        private async void OnDeleteSwiped(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipeItem && swipeItem.BindingContext is string number)
            {
                await _whitelistService.RemoveNumberAsync(number);
                LoadWhitelist();
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private static string T(string key) => LocalizationResourceManager.Instance[key];
    }
}
