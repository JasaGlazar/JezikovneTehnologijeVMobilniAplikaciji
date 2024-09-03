using CommunityToolkit.Maui.Views;
using Plugin.Maui.Audio;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FA = UraniumUI.Icons.FontAwesome;
using System.Text;

namespace JGDiplomskaNaloga
{
    public partial class MainPage : ContentPage
    {
        readonly IAudioManager _audioManager;
        readonly IAudioRecorder _audioRecorder;
        
        private static readonly string apiUrlTranscription = "https://transcriber-hgyyhzqswq-ew.a.run.app/api/transcribe";
        private static readonly string apiUrlTranslation = "https://slovenetranslator-hgyyhzqswq-ew.a.run.app/api/translate";

        private static readonly string sourceLanguage = "sl"; // Slovenian
        private static readonly string targetLanguage = "en"; // English

        public MainPage(IAudioManager audioManager)
        {            
            InitializeComponent();

            this._audioManager = audioManager;
            this._audioRecorder = audioManager.CreateRecorder();
        }

        private void SwapLanguages_Clicked(object sender, EventArgs e)
        {
            ImageSource tmp = ImgTranslateTo.Source;
            ImgTranslateTo.Source = ImgTranslateFrom.Source;
            ImgTranslateFrom.Source = tmp;
        }

        private async void StartRecording_Clicked(object sender, EventArgs e)
        {
            InputTextEditor.Text = String.Empty;
            OutputEditor.Text = String.Empty;

            if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted)
            {
                await DisplayAlert("Opozorilo", "Aplikaciji omogočite uporabo mikforona", "OK");
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
                
               TranscribeRecordedAudio(audioStream);
            }
        }

        private async void TranscribeRecordedAudio(Stream? stream)
        {
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

                                InputTextEditor.Text = transcriptionText;

                                TranslateTextInput(transcriptionText);
                            }
                            else
                            {
                                await DisplayAlert("Napaka", "Napaka pri transkripciji zvoka", "OK");
                            }
                            
                       }
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

            }
        }

        private void InputTextEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            //string InputText = string.Empty;

            //if (string.IsNullOrWhiteSpace(InputTextEditor.Text))
            //{
            //    InputText = InputTextEditor.Text;
            //    TranslateTextInput(InputText);
            //}
        }


        private async void TranslateTextInput(string Text)
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

                        OutputEditor.Text = translatedText;
                        
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

        private async void TextToSpeechSlovenian_Clicked(object sender, EventArgs e)
        {
            Debug.WriteLine("TTS Button pressed");

            try
            {
                if (string.IsNullOrWhiteSpace(InputTextEditor.Text))
                {
                    Debug.WriteLine("No text available for speech.");
                    return;
                }

                IEnumerable<Locale> locales = await TextToSpeech.Default.GetLocalesAsync();

                Locale slovenianLocale = locales.FirstOrDefault(locale => locale.Language.Equals("sl", StringComparison.OrdinalIgnoreCase) && locale.Country.Equals("SI", StringComparison.OrdinalIgnoreCase));

                Debug.WriteLine($"Locale selected: {slovenianLocale.Language} - {slovenianLocale.Name}");

                SpeechOptions options = new SpeechOptions
                {
                    Locale = slovenianLocale
                };

                await TextToSpeech.Default.SpeakAsync(InputTextEditor.Text, options);
                Debug.WriteLine("Text-to-Speech completed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in TextToSpeech: {ex.Message}");
            }
        }

        private async void TextToSpeechEnglish_Clicked(object sender, EventArgs e)
        {
            Debug.WriteLine("TTS Button pressed");

            try
            {
                if (string.IsNullOrWhiteSpace(OutputEditor.Text))
                {
                    Debug.WriteLine("No text available for speech.");
                    return;
                }

                IEnumerable<Locale> locales = await TextToSpeech.Default.GetLocalesAsync();

                Locale englishLocale = locales.FirstOrDefault(locale => locale.Language.Equals("en", StringComparison.OrdinalIgnoreCase) && locale.Country.Equals("US", StringComparison.OrdinalIgnoreCase));

                Debug.WriteLine($"Locale selected: {englishLocale.Language} - {englishLocale.Name}");

                SpeechOptions options = new SpeechOptions
                {
                    Locale = englishLocale
                };

                await TextToSpeech.Default.SpeakAsync(OutputEditor.Text, options);
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