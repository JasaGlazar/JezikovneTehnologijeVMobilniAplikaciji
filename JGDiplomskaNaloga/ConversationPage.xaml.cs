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
    //Flags
    private bool _isPerson1Updating = false;
    private bool _isPerson2Updating = false;

    //Azure - Speech To Text
    static string speechKey = "980eafbab69a42c3bb241272a6b7316f";
    static string speechRegion = "westeurope";
    private SpeechRecognizer _speechRecognizer;
    private bool _isRecordingPerson1 = false;

    //Azure - Translation
    private static readonly string translatorKey = "e667c42999c84229af34f67658bb100f";
    private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com/";
    private static readonly string translatorRegion = "westeurope";
    private static readonly string sourceLanguage = "sl"; // Slovenian
    private static readonly string targetLanguage = "en"; // English

    //Google - Speech To Text / Text to Speech
    readonly IAudioManager _audioManager;
    readonly IAudioRecorder _audioRecorder;

    public ConversationPage(IAudioManager audioManager)
	{
		InitializeComponent();

        this._audioManager = audioManager;
        this._audioRecorder = audioManager.CreateRecorder();
	}

    private async void StartRecordingPerson2_Clicked(object sender, EventArgs e)
    {
        GoogleSpeechToText();
    }

    private async void StartRecordingPerson1_Clicked(object sender, EventArgs e)
    {
        AzureSpeechToText();
    }


    static string OutputSpeechRecognitionResult(Microsoft.CognitiveServices.Speech.SpeechRecognitionResult speechRecognitionResult)
    {
        switch (speechRecognitionResult.Reason)
        {
            case ResultReason.RecognizedSpeech:
                return speechRecognitionResult.Text;
            case ResultReason.NoMatch:
                return $"NOMATCH: Speech could not be recognized.";
            case ResultReason.Canceled:
                var cancellation = CancellationDetails.FromResult(speechRecognitionResult);
                return $"CANCELED: Reason={cancellation.Reason}";

              /*  if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                    Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                }*/
        }
        return String.Empty;
    }
    
    private async void AzureSpeechToText()
    {
        try
        {
            if (!_isRecordingPerson1)
            {
                if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted)
                {
                    await DisplayAlert("Opozorilo", "Aplikaciji omogočite uporabo mikforona", "OK");
                    return;
                }

                Debug.WriteLine("Starting speech recognition for Person 1");
                Person1TextEditor.Text = ""; // Ensure text editor is cleared correctly
                Person2TextEditor.Text = ""; // Clear Person 2 text editor

                var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
                speechConfig.SpeechRecognitionLanguage = "sl-SI";


                using var audioConfig = Microsoft.CognitiveServices.Speech.Audio.AudioConfig.FromDefaultMicrophoneInput();

                _speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

                _isRecordingPerson1 = true;
                var speechRecognitionResult = await _speechRecognizer.RecognizeOnceAsync();
                string recognizedText = OutputSpeechRecognitionResult(speechRecognitionResult);

                if (!string.IsNullOrEmpty(recognizedText))
                {
                    UpdatePerson1TextEditor(recognizedText);
                    // Directly call translation logic
                    TranslatePerson1ToPerson2(recognizedText);
                }


                _isRecordingPerson1 = false;

            }
            else
            {
                _speechRecognizer?.StopContinuousRecognitionAsync();
                _speechRecognizer = null;
                _isRecordingPerson1 = false;
            }
        }
        catch (Exception ex)
        {
            // Log or display the error
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void AzureTranslateText(string text, Action<string> onTranslationComplete)
    {
        string route = $"/translate?api-version=3.0&from={sourceLanguage}&to={targetLanguage}";

        try
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new[]
                {
                    new { Text = text }

                };

                string jsonData = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Post;
                    request.RequestUri =new Uri(endpoint + route);
                    request.Content = content;
                    request.Headers.Add("Ocp-Apim-Subscription-Key", translatorKey);
                    request.Headers.Add("Ocp-Apim-Subscription-Region", translatorRegion);

                    var response = await client.SendAsync(request).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        string translatedText = await response.Content.ReadAsStringAsync();

                        // Parse the JSON response to get the translated text
                        var jsonResponse = JArray.Parse(translatedText);
                        string translatedLine = jsonResponse[0]["translations"][0]["text"].ToString();

                        onTranslationComplete(translatedLine);

                    }
                    else
                    {
                        Debug.WriteLine($"Failed to translate text. Status code: {response.StatusCode}");
                    }

                }

            }
        }
        catch (HttpRequestException httpEx)
        {
            Debug.WriteLine($"HTTP error occurred: {httpEx.Message}");
        }
        catch (IOException ioEx)
        {
            Debug.WriteLine($"File I/O error occurred: {ioEx.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"An error occurred: {ex.Message}");
        }

    }

    private async void GoogleSpeechToText()
    {
        try
        {
            if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted)
            {
                await DisplayAlert("Opozorilo", "Aplikaciji omogočite uporabo mikforona", "OK");
                return;
            }

            if (!_audioRecorder.IsRecording)
            {
                await _audioRecorder.StartAsync();
            }
            else
            {
                var recordedAudio = await _audioRecorder.StopAsync();
                var audioStream = recordedAudio.GetAudioStream();

                var speech = SpeechClient.Create();
                var config = new RecognitionConfig
                {
                    LanguageCode = Google.Cloud.Speech.V1.LanguageCodes.English.UnitedStates
                };

                var audio = RecognitionAudio.FromStream(audioStream);
                var response = speech.Recognize(config, audio);

                foreach (var result in response.Results)
                {
                    foreach (var alternative in result.Alternatives)
                    {
                        string recognizedText = alternative.Transcript;
                        UpdatePerson2TextEditor(recognizedText);

                        // Immediately call translation logic after updating Person 2's text
                        TranslatePerson2ToPerson1(recognizedText);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void TextToSpeechPerson1_Clicked(object sender, EventArgs e)
    {
        if (Person1TextEditor.Text.Length != 0)
        {
            string InputText = Person1TextEditor.Text;
            AzureTextToSpeech(InputText);
        }
    }

    private async void AzureTextToSpeech(string Text)
    {
        var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);

        speechConfig.SpeechSynthesisVoiceName = "sl-SI-PetraNeural";
        speechConfig.SpeechSynthesisLanguage = "sl-SI";
        
        using (var speechSynthesizer = new SpeechSynthesizer(speechConfig))
        {
            var speechSynthesisResult = await speechSynthesizer.SpeakTextAsync(Text);
        }
    }

    private async void GoogleTextToSpeech(string Text)
    {
        try
        {
            var client = TextToSpeechClient.Create();

            var input = new SynthesisInput
            {
                Text = Text
            };

            var voiceSelection = new VoiceSelectionParams
            {
                LanguageCode = "en-US",
                SsmlGender = SsmlVoiceGender.Female
            };

            var audioConfig = new Google.Cloud.TextToSpeech.V1.AudioConfig
            {
                AudioEncoding = AudioEncoding.Mp3
            };

            var response = client.SynthesizeSpeech(input, voiceSelection, audioConfig);

            using (var memoryStream = new MemoryStream())
            {
                response.AudioContent.WriteTo(memoryStream);

                memoryStream.Position = 0;

                var audioPlayer = _audioManager.CreatePlayer(memoryStream);

                audioPlayer.Play();
            }



        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }
    private void GoogleTranslateText(string Text, Action<string> onTranslationComplete)
    {
        try
        {
            TranslationClient translationClient = TranslationClient.Create();
            TranslationResult result = translationClient.TranslateText(Text, Google.Cloud.Translation.V2.LanguageCodes.Slovenian, Google.Cloud.Translation.V2.LanguageCodes.English);

            string translatedText = result.TranslatedText;
            onTranslationComplete(translatedText);

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
        
    }
    
    private void TextToSpeechPerson2_Clicked(object sender, EventArgs e)
    {
        if(Person2TextEditor.Text.Length != 0)
        {
            GoogleTextToSpeech(Person2TextEditor.Text);
        }

    }


    private void UpdatePerson1TextEditor(string text)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _isPerson1Updating = true;
            Person1TextEditor.IsReadOnly = false;  // Temporarily enable editing to allow text change
            Person1TextEditor.Text = text;
            Person1TextEditor.IsReadOnly = true;   // Set back to read-only
            _isPerson1Updating = false;
        });
    }
    private void UpdatePerson2TextEditor(string text)
    {
        _isPerson2Updating = true;
        Person2TextEditor.IsReadOnly = false;  // Temporarily enable editing to allow text change
        Person2TextEditor.Text = text;
        Person2TextEditor.IsReadOnly = true;   // Set back to read-only
        _isPerson2Updating = false;
    }

    private void TranslatePerson1ToPerson2(string text)
    {
        Debug.WriteLine($"Translating for Person 2: {text}");
        AzureTranslateText(text, translatedText =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Debug.WriteLine($"Translated Text for Person 2: {translatedText}");
                UpdatePerson2TextEditor(translatedText);
            });
        });
    }

    private void TranslatePerson2ToPerson1(string text)
    {
        Debug.WriteLine($"Translating for Person 1: {text}");
        GoogleTranslateText(text, translatedText =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Debug.WriteLine($"Translated Text for Person 1: {translatedText}");
                UpdatePerson1TextEditor(translatedText);
            });
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var page = new MyBottomSheet();
        page.ShowAsync(Window, true);
    }
}
