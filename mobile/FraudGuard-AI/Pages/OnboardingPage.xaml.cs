using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using FraudGuardAI.Helpers;
using FraudGuardAI.Localization;
using FraudGuardAI.Pages.Auth;

namespace FraudGuardAI.Pages
{
    // ── Data model for a single onboarding slide ─────────────────────────────
    public sealed class SlideData
    {
        public string Icon        { get; init; } = "";
        public string Title       { get; init; } = "";
        public string Description { get; init; } = "";
    }

    public partial class OnboardingPage : ContentPage
    {
        // Slide metadata: (emoji, localization title key, localization description key)
        private static readonly (string Icon, string TitleKey, string DescKey)[] SlideMeta =
        {
            ("🛡️", "Onboarding_Slide1_Title", "Onboarding_Slide1_Desc"),
            ("📡", "Onboarding_Slide2_Title", "Onboarding_Slide2_Desc"),
            ("🤖", "Onboarding_Slide3_Title", "Onboarding_Slide3_Desc"),
            ("🚀", "Onboarding_Slide4_Title", "Onboarding_Slide4_Desc"),
        };

        private int _currentIndex = 0;

        public OnboardingPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Build slide list with the current locale.
            // Setting ItemsSource here (not in XAML) ensures that localized strings
            // are resolved after LocalizationResourceManager is fully initialized.
            OnboardingCarousel.ItemsSource = SlideMeta
                .Select(s => new SlideData
                {
                    Icon        = s.Icon,
                    Title       = T(s.TitleKey),
                    Description = T(s.DescKey),
                })
                .ToList();

            _currentIndex = 0;
            UpdateButtonState(0);
        }

        // ── CarouselView position change ──────────────────────────────────────

        private void OnCurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            var pos = OnboardingCarousel.Position;
            if (pos >= 0 && pos < SlideMeta.Length)
            {
                _currentIndex = pos;
                UpdateButtonState(_currentIndex);
            }
        }

        // ── Update Next/GetStarted button and Skip visibility ─────────────────

        private void UpdateButtonState(int index)
        {
            bool isLastSlide = index == SlideMeta.Length - 1;
            GetStartedButton.Text = isLastSlide ? T("Onboarding_GetStarted") : T("Onboarding_Next");
            SkipLabel.IsVisible   = !isLastSlide;
        }

        // ── Button handlers ───────────────────────────────────────────────────

        private void OnSkipClicked(object sender, EventArgs e)
        {
            // Jump directly to the last slide
            OnboardingCarousel.ScrollTo(SlideMeta.Length - 1);
        }

        private async void OnActionClicked(object sender, EventArgs e)
        {
            if (_currentIndex < SlideMeta.Length - 1)
            {
                // Advance to next slide
                OnboardingCarousel.ScrollTo(_currentIndex + 1);
            }
            else
            {
                // Final slide — complete onboarding
                Preferences.Set("OnboardingCompleted", true);

                // Request all required permissions before landing on the main flow
                await PermissionManager.RequestAllPermissionsAsync();

                // Navigate to login
                Application.Current!.MainPage = new NavigationPage(new LoginPage())
                {
                    BarBackgroundColor = Color.FromArgb("#0D1B2A"),
                    BarTextColor       = Color.FromArgb("#E0E6ED"),
                };
            }
        }

        // ── Localization helper ───────────────────────────────────────────────

        private static string T(string key) => LocalizationResourceManager.Instance[key];
    }
}
