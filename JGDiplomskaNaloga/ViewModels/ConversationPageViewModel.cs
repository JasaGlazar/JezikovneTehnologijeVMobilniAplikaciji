using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Plugin.Maui.Audio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Speech.V1;
using Google.Cloud.TextToSpeech.V1;
using Google.Cloud.Translation.V2;

namespace JGDiplomskaNaloga.ViewModels
{
    internal class ConversationPageViewModel : INotifyPropertyChanged
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

        public INavigation _navigation;

        private string person1Text;
        public string Person1Text
        {
            get => person1Text;
            set
            {
                person1Text = value;
                RaisePropertyChanged("Person1Text");
            }

        }
        private string person2Text;
        public string Person2Text
        {
            get => person2Text;
            set
            {
                person2Text = value;
                RaisePropertyChanged("Person2Text");
            }

        }

        public Command Person1StartRecordingBtn { get; }

        public Command Person2StartRecordingBtn { get; }

        public Command Person1TTSBtn { get; }
        public Command Person2TTSBtn { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void RaisePropertyChanged(string v)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(v));
        }

        public ConversationPageViewModel(INavigation navigation ,IAudioManager audioManager)
        {

            _navigation = navigation;

            Person1StartRecordingBtn = new Command(Person1StartRecordingBtnTappedAsync);
            Person2StartRecordingBtn = new Command(Person2StartRecordingBtnTappedAsync);
            Person1TTSBtn = new Command(Person1TTSBtnTappedAsync);
            Person2TTSBtn = new Command(Person2TTSBtnTappedAsync);

            this._audioManager = audioManager;
            this._audioRecorder = audioManager.CreateRecorder();
        }

        private void Person2TTSBtnTappedAsync(object obj)
        {
            if (!String.IsNullOrEmpty(Person2Text))
            {
                GoogleTextToSpeech(Person2Text);
            }
        }

        private void Person1TTSBtnTappedAsync(object obj)
        {
            if (!String.IsNullOrEmpty(Person1Text))
            {
                AzureTextToSpeech(Person1Text);
            }
        }

        private async void Person2StartRecordingBtnTappedAsync(object obj)
        {
            await GoogleSpeechToText();
        }

        private async void Person1StartRecordingBtnTappedAsync(object obj)
        {
            await AzureSpeechToText();
        }

        private async Task AzureSpeechToText()
        {
            try
            {
                if (!_isRecordingPerson1)
                {
                    if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted)
                    {
                        await App.Current.MainPage.DisplayAlert("Opozorilo", "Aplikaciji omogočite uporabo mikforona", "OK");
                        return;
                    }

                    Debug.WriteLine("Starting speech recognition for Person 1");
                    Person1Text = "";
                    Person2Text = "";


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
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async Task AzureTranslate(string text, Action<string> onTranslationComplete)
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
                        request.RequestUri = new Uri(endpoint + route);
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

        private async Task GoogleSpeechToText()
        {
            try
            {
                if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted)
                {
                    await App.Current.MainPage.DisplayAlert("Opozorilo", "Aplikaciji omogočite uporabo mikforona", "OK");
                    return;
                }

                Person1Text  = "";
                Person2Text = "";


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
                        LanguageCode = Google.Cloud.Speech.V1.LanguageCodes.Slovenian.Slovenia
                    };

                    var audio = RecognitionAudio.FromStream(audioStream);
                    var response = speech.Recognize(config, audio);

                    foreach (var result in response.Results)
                    {
                        foreach (var alternative in result.Alternatives)
                        {
                            string recognizedText = alternative.Transcript;
                            UpdatePerson2TextEditor(recognizedText);
                            TranslatePerson2ToPerson1(recognizedText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
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

        private void GoogleTextToSpeech(string Text)
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

        private void GoogleTranslate(string Text, Action<string> onTranslationComplete)
        {
            try
            {
                TranslationClient translationClient = TranslationClient.Create();
                TranslationResult result = translationClient.TranslateText(Text, Google.Cloud.Translation.V2.LanguageCodes.English, Google.Cloud.Translation.V2.LanguageCodes.Slovenian);

                string translatedText = result.TranslatedText;
                onTranslationComplete(translatedText);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

        }

        private void UpdatePerson1TextEditor(string text)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _isPerson1Updating = true;
                Person1Text = text;
                _isPerson1Updating = false;
            });
        }
        private void UpdatePerson2TextEditor(string text)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _isPerson2Updating = true;
                Person2Text = text;
                _isPerson2Updating = false;
            });
        }

        private async void TranslatePerson1ToPerson2(string text)
        {
            await AzureTranslate(text, translatedText =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdatePerson2TextEditor(translatedText);
                });
            });
        }

        private void TranslatePerson2ToPerson1(string text)
        {
            GoogleTranslate(text, translatedText =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Debug.WriteLine($"Translated Text for Person 1: {translatedText}");
                    UpdatePerson1TextEditor(translatedText);
                });
            });
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
            }
            return String.Empty;
        }
    }
}
