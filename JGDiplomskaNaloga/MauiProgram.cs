using Microsoft.Extensions.Logging;
using UraniumUI;
using CommunityToolkit.Maui;
using Plugin.Maui.Audio;
using The49.Maui.BottomSheet;

namespace JGDiplomskaNaloga
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .UseBottomSheet()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Roboto-Regular.ttf", "Roboto");
                    fonts.AddFontAwesomeIconFonts();
                });
            builder.Services.AddSingleton(AudioManager.Current);
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ConversationPage>();

            string credentialsFilename = "diplomskanalogajg-9919ce64a9ec.json";
            string credentialsFilepath = Path.Combine(FileSystem.Current.AppDataDirectory, credentialsFilename);

            using (var stream = FileSystem.OpenAppPackageFileAsync(credentialsFilename).Result)
            using (var fileStream = File.Create(credentialsFilepath))
            {
                stream.CopyTo(fileStream);
            }

            // Set the environment variable to point to the copied credentials file
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsFilepath);

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
