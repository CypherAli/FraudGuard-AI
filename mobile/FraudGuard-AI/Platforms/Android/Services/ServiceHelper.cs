using Android.Content;
using Microsoft.Maui.Controls;

namespace FraudGuardAI.Platforms.Android.Services
{
    /// <summary>
    /// Helper class để quản lý foreground service từ cross-platform code
    /// </summary>
    public static class ServiceHelper
    {
        /// <summary>
        /// Start foreground service khi bắt đầu bảo vệ
        /// </summary>
        public static void StartProtectionService()
        {
            try
            {
                var context = global::Android.App.Application.Context;
                FraudGuardForegroundService.Start(context);
                System.Diagnostics.Debug.WriteLine("[ServiceHelper] Protection service started");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ServiceHelper] Error starting service: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop foreground service khi dừng bảo vệ
        /// </summary>
        public static void StopProtectionService()
        {
            try
            {
                var context = global::Android.App.Application.Context;
                FraudGuardForegroundService.Stop(context);
                System.Diagnostics.Debug.WriteLine("[ServiceHelper] Protection service stopped");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ServiceHelper] Error stopping service: {ex.Message}");
            }
        }
    }
}
