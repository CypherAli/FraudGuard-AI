# ⚠️ IMPORTANT: App.xaml.cs Update Required

## You need to update your `App.xaml.cs` file to use AppShell

### Find your App.xaml.cs file and update it:

**Old Code** (if using MainPage directly):
```csharp
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new MainPage();  // ❌ OLD
    }
}
```

**New Code** (use AppShell):
```csharp
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();  // ✅ NEW - Enables tab navigation
    }
}
```

### Why This Change?

- `AppShell` provides the bottom tab bar navigation
- Allows users to switch between Protection, History, and Settings
- Without this, the app will only show MainPage (no tabs)

### After Making This Change:

1. Rebuild the app
2. You should see 3 tabs at the bottom:
   - 🛡️ Bảo vệ
   - 📋 Lịch sử
   - ⚙️ Cài đặt

---

## Alternative: If App.xaml.cs doesn't exist

Create it in the root of your project:

```csharp
namespace FraudGuardAI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }
    }
}
```

And create `App.xaml`:

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="FraudGuardAI.App">
    <Application.Resources>
        <ResourceDictionary>
            <!-- Add global resources here -->
        </ResourceDictionary>
    </Application.Resources>
</Application>
```
