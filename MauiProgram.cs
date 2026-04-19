using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;

namespace LocalBookManager
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseLocalNotification()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("arial.ttf", "Arial");
                    fonts.AddFont("meiryo.ttf", "Meiryo");
                    fonts.AddFont("dotum.ttf", "Dotum");
                    fonts.AddFont("aaa.ttf", "AAA");
                    fonts.AddFont("sans_serif.ttf", "SansSerif");
                    fonts.AddFont("century_gothic.ttf", "CenturyGothic");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}