using FraudGuardAI.Pages;

namespace FraudGuardAI
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register push-navigation routes
            Routing.RegisterRoute("HistoryDetailPage", typeof(HistoryDetailPage));
            Routing.RegisterRoute("WhitelistPage", typeof(WhitelistPage));
        }
    }
}
