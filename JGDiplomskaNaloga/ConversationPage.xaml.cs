using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using Plugin.Maui.Audio;
using Google.Cloud.Speech.V1;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;
using Grpc.Core;
using Google.Cloud.Translation.V2;
using Google.Cloud.TextToSpeech.V1;
using CommunityToolkit.Maui.Storage;

namespace JGDiplomskaNaloga;

public partial class ConversationPage : ContentPage
{
    
    public ConversationPage(IAudioManager audioManager)
	{
		InitializeComponent();

        BindingContext = new ViewModels.ConversationPageViewModel(Navigation, audioManager);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var page = new MyBottomSheet();
        page.ShowAsync(Window, true);
    }
}
