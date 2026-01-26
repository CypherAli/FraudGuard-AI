namespace FraudGuardAI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // ✅ USE APPSHELL FOR TAB NAVIGATION
            // This enables the bottom tab bar with Protection, History, and Settings tabs
            MainPage = new AppShell();
            
            // ❌ OLD WAY (no tabs):
            // MainPage = new MainPage();
        }
    }
}
