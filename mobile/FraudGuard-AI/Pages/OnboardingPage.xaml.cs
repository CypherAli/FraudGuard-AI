using System;
using Microsoft.Maui.Controls;
using FraudGuardAI.Helpers;
using FraudGuardAI.Localization;
using FraudGuardAI.Pages.Auth;

namespace FraudGuardAI.Pages
{
    public partial class OnboardingPage : ContentPage
    {
        private readonly (string Icon, string TitleKey, string DescKey)[] _slides = new[]
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
            // We use the CurrentItemChanged event to update slide content
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateSlideContent(0);
        }

        private void OnCurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            if (OnboardingCarousel.Position >= 0 && OnboardingCarousel.Position < _slides.Length)
            {
                _currentIndex = OnboardingCarousel.Position;
                UpdateSlideContent(_currentIndex);
            }
        }

        private void UpdateSlideContent(int index)
        {
            bool isLastSlide = index == _slides.Length - 1;

            // Update button text
            GetStartedButton.Text = isLastSlide ? T("Onboarding_GetStarted") : T("Onboarding_Next");
            SkipLabel.IsVisible = !isLastSlide;

            // Update the visible carousel item content
            // Since DataTemplate doesn't support direct binding to slide data easily,
            // we update the carousel's children directly using the VisualTreeHelper approach
            UpdateCarouselItemContent(index);
        }

        private void UpdateCarouselItemContent(int index)
        {
            // Carousel items are generated from DataTemplate, so we need to manually update
            // the content of the current visible item. This approach works with MAUI CarouselView.
            try
            {
                var slide = _slides[index];
                // Find the current visible item's VisualStack
                // The CarouselView renders DataTemplates, but we handle content via events
                // For simplicity, we rely on the visual tree if available
#pragma warning disable CS0618
                foreach (var child in OnboardingCarousel.LogicalChildren)
                {
                    if (child is VisualElement ve)
                    {
                        UpdateVisualElement(ve, slide);
                    }
                }
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Onboarding] Update error: {ex.Message}");
            }
        }

        private void UpdateVisualElement(Element element, (string Icon, string TitleKey, string DescKey) slide)
        {
            if (element is VerticalStackLayout stack && stack.Children.Count >= 3)
            {
                // Icon container
                if (stack.Children[0] is Border border && border.Content is Label iconLabel)
                    iconLabel.Text = slide.Icon;
                // Title
                if (stack.Children[1] is Label titleLabel)
                    titleLabel.Text = T(slide.TitleKey);
                // Description
                if (stack.Children[2] is Label descLabel)
                    descLabel.Text = T(slide.DescKey);
            }

#pragma warning disable CS0618
            foreach (var child in element.LogicalChildren)
#pragma warning restore CS0618
            {
                if (child is Element childElement)
                    UpdateVisualElement(childElement, slide);
            }
        }

        private void OnSkipClicked(object sender, EventArgs e)
        {
            // Jump to last slide
            OnboardingCarousel.ScrollTo(_slides.Length - 1);
        }

        private async void OnActionClicked(object sender, EventArgs e)
        {
            if (_currentIndex < _slides.Length - 1)
            {
                // Next slide
                OnboardingCarousel.ScrollTo(_currentIndex + 1);
            }
            else
            {
                // Get Started — complete onboarding
                Preferences.Set("OnboardingCompleted", true);

                // Request permissions
                await PermissionManager.RequestAllPermissionsAsync();

                // Navigate to login
                Application.Current!.MainPage = new NavigationPage(new LoginPage())
                {
                    BarBackgroundColor = Color.FromArgb("#0D1B2A"),
                    BarTextColor = Color.FromArgb("#E0E6ED")
                };
            }
        }

        private static string T(string key) => LocalizationResourceManager.Instance[key];
    }
}
