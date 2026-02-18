using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace FraudGuardAI.Localization
{
    public sealed class LocalizationResourceManager : INotifyPropertyChanged
    {
        public static LocalizationResourceManager Instance { get; } = new LocalizationResourceManager();

        private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string this[string key]
            => ResourceManager.GetString(key, _currentCulture) ?? key;

        private static ResourceManager ResourceManager
            => new ResourceManager("FraudGuardAI.Resources.Strings", typeof(App).Assembly);

        public void SetCulture(CultureInfo culture)
        {
            if (_currentCulture.Equals(culture))
            {
                return;
            }

            _currentCulture = culture;

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }
}
