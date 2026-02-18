using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Provider;
using Color = Android.Graphics.Color;

namespace FraudGuardAI.Platforms.Android.Services
{
    /// <summary>
    /// Android overlay service that shows a floating bubble during calls
    /// Displays real-time fraud risk level and deepfake score
    /// Requires SYSTEM_ALERT_WINDOW permission
    /// </summary>
    [Service(Name = "com.fraudguard.ai.OverlayService", Exported = false)]
    public class OverlayService : Service
    {
        private IWindowManager _windowManager;
        private global::Android.Views.View _overlayView;
        private TextView _riskText;
        private TextView _deepfakeText;
        private TextView _statusText;
        private ImageView _closeButton;
        private bool _isShowing = false;

        public override IBinder OnBind(Intent intent) => null;

        public override void OnCreate()
        {
            base.OnCreate();
            _windowManager = GetSystemService(WindowService).JavaCast<IWindowManager>();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            string action = intent?.Action;

            switch (action)
            {
                case "SHOW":
                    ShowOverlay();
                    break;
                case "UPDATE":
                    int riskScore = intent.GetIntExtra("risk_score", 0);
                    int deepfakeScore = intent.GetIntExtra("deepfake_score", 0);
                    string status = intent.GetStringExtra("status") ?? "Đang bảo vệ";
                    UpdateOverlay(riskScore, deepfakeScore, status);
                    break;
                case "HIDE":
                    HideOverlay();
                    break;
            }

            return StartCommandResult.Sticky;
        }

        private void ShowOverlay()
        {
            if (_isShowing) return;
            if (!Settings.CanDrawOverlays(this))
            {
                System.Diagnostics.Debug.WriteLine("[Overlay] No SYSTEM_ALERT_WINDOW permission");
                return;
            }

            try
            {
                // Inflate overlay layout programmatically
                _overlayView = CreateOverlayView();

                var layoutParams = new WindowManagerLayoutParams(
                    WindowManagerLayoutParams.WrapContent,
                    WindowManagerLayoutParams.WrapContent,
                    WindowManagerTypes.ApplicationOverlay,
                    WindowManagerFlags.NotFocusable | WindowManagerFlags.LayoutInScreen,
                    Format.Translucent
                );

                layoutParams.Gravity = GravityFlags.Top | GravityFlags.Right;
                layoutParams.X = 20;
                layoutParams.Y = 200;

                _windowManager.AddView(_overlayView, layoutParams);
                _isShowing = true;

                // Enable drag
                SetupDrag(layoutParams);

                System.Diagnostics.Debug.WriteLine("[Overlay] Bubble shown");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Overlay] Show error: {ex.Message}");
            }
        }

        private global::Android.Views.View CreateOverlayView()
        {
            // Main container
            var container = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical,
            };
            container.SetPadding(24, 16, 24, 16);
            container.SetBackgroundColor(Color.ParseColor("#1A1F2E"));
            container.Background.Alpha = 230;

            // Apply rounded corners via GradientDrawable
            var bg = new global::Android.Graphics.Drawables.GradientDrawable();
            bg.SetColor(Color.ParseColor("#1A1F2E"));
            bg.SetCornerRadius(24f);
            bg.SetStroke(2, Color.ParseColor("#14B8A6"));
            container.Background = bg;

            // Title row
            var titleRow = new LinearLayout(this)
            {
                Orientation = Orientation.Horizontal,
            };
            titleRow.SetGravity(GravityFlags.CenterVertical);

            var titleText = new TextView(this)
            {
                Text = "🛡️ FraudGuard",
            };
            titleText.SetTextColor(Color.ParseColor("#14B8A6"));
            titleText.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 12);
            titleText.SetTypeface(null, TypefaceStyle.Bold);

            _closeButton = new ImageView(this);
            _closeButton.SetImageResource(global::Android.Resource.Drawable.IcMenuCloseClearCancel);
            _closeButton.SetColorFilter(Color.ParseColor("#9CA3AF"));
            var closeParams = new LinearLayout.LayoutParams(40, 40);
            closeParams.LeftMargin = 16;
            _closeButton.LayoutParameters = closeParams;
            _closeButton.Click += (s, e) => HideOverlay();

            titleRow.AddView(titleText, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1));
            titleRow.AddView(_closeButton);

            // Status text
            _statusText = new TextView(this)
            {
                Text = "🔵 Đang bảo vệ"
            };
            _statusText.SetTextColor(Color.ParseColor("#D1D5DB"));
            _statusText.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 10);

            // Risk score
            _riskText = new TextView(this)
            {
                Text = "Rủi ro: 0%"
            };
            _riskText.SetTextColor(Color.ParseColor("#10B981"));
            _riskText.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 11);
            _riskText.SetTypeface(null, TypefaceStyle.Bold);

            // Deepfake score
            _deepfakeText = new TextView(this)
            {
                Text = "Deepfake: 0%"
            };
            _deepfakeText.SetTextColor(Color.ParseColor("#9CA3AF"));
            _deepfakeText.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 10);

            container.AddView(titleRow);
            container.AddView(_statusText);
            container.AddView(_riskText);
            container.AddView(_deepfakeText);

            return container;
        }

        private void UpdateOverlay(int riskScore, int deepfakeScore, string status)
        {
            if (!_isShowing || _riskText == null) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    _statusText.Text = status;

                    // Update risk score with color coding
                    _riskText.Text = $"Rủi ro: {riskScore}%";
                    if (riskScore >= 80)
                        _riskText.SetTextColor(Color.ParseColor("#EF4444"));
                    else if (riskScore >= 60)
                        _riskText.SetTextColor(Color.ParseColor("#F59E0B"));
                    else if (riskScore >= 40)
                        _riskText.SetTextColor(Color.ParseColor("#FBBF24"));
                    else
                        _riskText.SetTextColor(Color.ParseColor("#10B981"));

                    // Update deepfake score
                    _deepfakeText.Text = $"Deepfake: {deepfakeScore}%";
                    if (deepfakeScore > 70)
                        _deepfakeText.SetTextColor(Color.ParseColor("#EF4444"));
                    else if (deepfakeScore > 40)
                        _deepfakeText.SetTextColor(Color.ParseColor("#F59E0B"));
                    else
                        _deepfakeText.SetTextColor(Color.ParseColor("#9CA3AF"));

                    // Update border color based on risk
                    var bg = new global::Android.Graphics.Drawables.GradientDrawable();
                    bg.SetColor(Color.ParseColor("#1A1F2E"));
                    bg.SetCornerRadius(24f);
                    if (riskScore >= 60)
                        bg.SetStroke(3, Color.ParseColor("#EF4444"));
                    else if (riskScore >= 40)
                        bg.SetStroke(2, Color.ParseColor("#F59E0B"));
                    else
                        bg.SetStroke(2, Color.ParseColor("#14B8A6"));
                    _overlayView.Background = bg;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Overlay] Update error: {ex.Message}");
                }
            });
        }

        private void HideOverlay()
        {
            if (!_isShowing) return;

            try
            {
                _windowManager.RemoveView(_overlayView);
                _isShowing = false;
                System.Diagnostics.Debug.WriteLine("[Overlay] Bubble hidden");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Overlay] Hide error: {ex.Message}");
            }
        }

        private void SetupDrag(WindowManagerLayoutParams layoutParams)
        {
            float initialX = 0, initialY = 0;
            float initialTouchX = 0, initialTouchY = 0;

            _overlayView.Touch += (s, e) =>
            {
                switch (e.Event.Action)
                {
                    case MotionEventActions.Down:
                        initialX = layoutParams.X;
                        initialY = layoutParams.Y;
                        initialTouchX = e.Event.RawX;
                        initialTouchY = e.Event.RawY;
                        e.Handled = true;
                        break;

                    case MotionEventActions.Move:
                        layoutParams.X = (int)(initialX - (e.Event.RawX - initialTouchX));
                        layoutParams.Y = (int)(initialY + (e.Event.RawY - initialTouchY));
                        _windowManager.UpdateViewLayout(_overlayView, layoutParams);
                        e.Handled = true;
                        break;
                }
            };
        }

        public override void OnDestroy()
        {
            HideOverlay();
            base.OnDestroy();
        }

        // Static helpers to control overlay from outside
        public static void Show(Context context)
        {
            var intent = new Intent(context, typeof(OverlayService));
            intent.SetAction("SHOW");
            context.StartService(intent);
        }

        public static void Update(Context context, int riskScore, int deepfakeScore, string status)
        {
            var intent = new Intent(context, typeof(OverlayService));
            intent.SetAction("UPDATE");
            intent.PutExtra("risk_score", riskScore);
            intent.PutExtra("deepfake_score", deepfakeScore);
            intent.PutExtra("status", status);
            context.StartService(intent);
        }

        public static void Hide(Context context)
        {
            var intent = new Intent(context, typeof(OverlayService));
            intent.SetAction("HIDE");
            context.StartService(intent);
        }
    }
}
