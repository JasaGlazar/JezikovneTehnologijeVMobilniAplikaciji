using Android.App;
using Android.Runtime;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;

namespace JGDiplomskaNaloga
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp()
        {
            // Remove Editor control underline
            Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("NoUnderline", (h, v) =>
            {
                h.PlatformView.BackgroundTintList =
                    Android.Content.Res.ColorStateList.ValueOf(Colors.White.ToAndroid());
            });

            return MauiProgram.CreateMauiApp();
        }
    }
}
