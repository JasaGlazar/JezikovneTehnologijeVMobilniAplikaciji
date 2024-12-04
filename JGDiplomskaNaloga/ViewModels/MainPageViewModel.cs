using Newtonsoft.Json.Linq;
using Plugin.Maui.Audio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JGDiplomskaNaloga.ViewModels
{
    internal class MainPageViewModel : INotifyPropertyChanged
    {
        readonly IAudioManager _audioManager;
        readonly IAudioRecorder _audioRecorder;

        public event PropertyChangedEventHandler? PropertyChanged;

        public INavigation _navigation;
        public Command StartRecordingBtn { get; }
        public Command SlovenianTTSBtn { get; }
        public Command EnglishTTSBtn { get; }
        public Command SaveTranslationBtn { get;  }

        private static readonly string apiUrlTranscription = "https://transcriber-hgyyhzqswq-ew.a.run.app/api/transcribe";
        private static readonly string apiUrlTranslation = "https://slovenetranslator-hgyyhzqswq-ew.a.run.app/api/translate";

        private static readonly string sourceLanguage = "sl"; // Slovenian
        private static readonly string targetLanguage = "en"; // English

        private static readonly string GovornikApiUrl = "https://s1.govornik.eu";

        private string sourceText;
        public string SourceText
        {
            get => sourceText;
            set
            {
                sourceText = value;
                RaisePropertyChanged("SourceText");
            }
        }

        private string translatedText;
        public string TranslatedText
        {
            get => translatedText;
            set
            {
                translatedText = value;
                RaisePropertyChanged("TranslatedText");
            }
        }

        private bool saveTranslationBtnIsVisible;

        public bool SaveTranslationBtnIsVisible
        {
            get => saveTranslationBtnIsVisible;
            set
            {
                saveTranslationBtnIsVisible = value;
                RaisePropertyChanged("SaveTranslationBtnIsVisible");
            }
        }

        private void RaisePropertyChanged(string v)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(v));
        }

        public MainPageViewModel(INavigation navigation, IAudioManager audioManager)
        {
            _navigation = navigation;
            StartRecordingBtn = new Command(StartRecordingBtnTappedAsync);
            SlovenianTTSBtn = new Command(SlovenianTTSBtnTappedAsync);
            EnglishTTSBtn = new Command(EnglishTTSBtnTappedAsync);
            SaveTranslationBtn = new Command(SaveTranslationBtnTappedAsync);

            this._audioManager = audioManager;
            this._audioRecorder = audioManager.CreateRecorder();
        }

        private async void SaveTranslationBtnTappedAsync(object obj)
        {
            throw new NotImplementedException();
        }

        private async void EnglishTTSBtnTappedAsync(object obj)
        {
            if (!String.IsNullOrEmpty(TranslatedText))
            {
                await EnglishTextToSpeech();
            }
        }

        private async void SlovenianTTSBtnTappedAsync(object obj)
        {
            if (!String.IsNullOrEmpty(SourceText))
            {
                await GovornikTextToSpeech();
            }
        }

        private async void StartRecordingBtnTappedAsync(object obj)
        {
            SourceText = "";
            TranslatedText = "";

            if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted)
            {
                await App.Current.MainPage.DisplayAlert("Opozorilo", "Aplikaciji omogočite uporabo mikforona", "OK");
                await Permissions.RequestAsync<Permissions.Microphone>();
            }
            if (!_audioRecorder.IsRecording)
            {
                await _audioRecorder.StartAsync();
            }
            else
            {
                var recordedAudio = await _audioRecorder.StopAsync();
                var audioStream = recordedAudio.GetAudioStream();
                await TranscribeRecordedAudio(audioStream);
            }
        }

        private async Task TranscribeRecordedAudio(Stream? stream)
        {
            //Uporabljamo odprtokodno rešitev Razpoznavalnik Slovenscina.eu - https://slovenscina.eu/razpoznavalnik
            if (stream == null)
            {
                Debug.WriteLine("Audio stream je prazen");
            }
            else
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        using (var FormData = new MultipartFormDataContent())
                        {
                            FormData.Add(new StreamContent(stream), "audio_file", "recording.wav");

                            HttpResponseMessage responseMessage = await client.PostAsync(apiUrlTranscription, FormData);

                            if (responseMessage.IsSuccessStatusCode)
                            {
                                string transcriptionResult = await responseMessage.Content.ReadAsStringAsync();
                                var jsonResponse = JObject.Parse(transcriptionResult);
                                string transcriptionText = jsonResponse["result"]?.ToString();
                                SourceText = transcriptionText;
                                await TranslateText(SourceText);

                                if (!String.IsNullOrEmpty(SourceText) && !String.IsNullOrEmpty(TranslatedText))
                                {
                                    SaveTranslationBtnIsVisible = true;
                                }
                            }
                            else
                            {
                                await App.Current.MainPage.DisplayAlert("Napaka", "Napaka pri transkripciji zvoka", "OK");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        private async Task TranslateText(string Text)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var RequestData = new
                    {
                        src_language = sourceLanguage,
                        tgt_language = targetLanguage,
                        text = Text

                    };

                    string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(RequestData);
                    var content = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(apiUrlTranslation, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string translationResult = await response.Content.ReadAsStringAsync();

                        var jsonResponse = JObject.Parse(translationResult);
                        string translatedText = jsonResponse["result"]?.ToString();

                        TranslatedText = translatedText;

                    }
                    else
                    {
                        Debug.WriteLine($"Failed to translate text. Status code: {response.StatusCode}");
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

        public async Task GovornikTextToSpeech()
        {
            //Uporabljamo Govornik Text to Speech API - https://www.govornik.eu/
            try
            {
                using (HttpClient client = new HttpClient())
                {

                    var queryParams = new Dictionary<string, string>
                    {
                        { "source", "mobileDiplomskaNalogaProjekt" },
                        { "text", SourceText },
                        { "voice", "lars" },
                        { "version", "2" },
                        { "format", "wav" }
                    };

                    string queryString = string.Join("&", queryParams.Select(param => $"{param.Key}={Uri.EscapeDataString(param.Value)}"));
                    string fullUrl = $"{GovornikApiUrl}?{queryString}";

                    var response = await client.GetAsync(fullUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await response.Content.CopyToAsync(memoryStream);
                            memoryStream.Position = 0;

                            var audioPlayer = _audioManager.CreatePlayer(memoryStream);
                            audioPlayer.Play();
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Text to Speech Failed. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
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
                Debug.WriteLine($"Error in TextToSpeech: {ex.Message}");
            }
        }

        public async Task EnglishTextToSpeech()
        {
            Debug.WriteLine("TTS Button pressed");

            try
            {
                IEnumerable<Locale> locales = await TextToSpeech.Default.GetLocalesAsync();

                Locale englishLocale = locales.FirstOrDefault(locale => locale.Language.Equals("en", StringComparison.OrdinalIgnoreCase) && locale.Country.Equals("US", StringComparison.OrdinalIgnoreCase));

                Debug.WriteLine($"Locale selected: {englishLocale.Language} - {englishLocale.Name}");

                SpeechOptions options = new SpeechOptions
                {
                    Locale = englishLocale
                };

                await TextToSpeech.Default.SpeakAsync(TranslatedText, options);
                Debug.WriteLine("Text-to-Speech completed.");
            }
            catch (Exception ex)
            {
                // Catch any exceptions for debugging
                Debug.WriteLine($"Error in TextToSpeech: {ex.Message}");
            }
        }
    }
}
