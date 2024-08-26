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
        
        private string _currentGlyph = FA.Solid.Microphone;

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
            string tmp = TranslateTo.Text;
            TranslateTo.Text = TranslateFrom.Text;
            TranslateFrom.Text = tmp;
        }

        private async void StartRecording_Clicked(object sender, EventArgs e)
        {
            InputTextEditor.Text = String.Empty;
            OutputEditor.Text = String.Empty;

            if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted)
            {
                //TODO: Userja opozori da nima vkloplenega mikrofona
                await DisplayAlert("Opozorilo", "Aplikaciji omogočite uporabo mikforona", "OK");
                await Permissions.RequestAsync<Permissions.Microphone>();
            }
            if (!_audioRecorder.IsRecording)
            {
                await _audioRecorder.StartAsync();

                StartRecording.BackgroundColor = Color.FromRgb(red: 255, green: 0, blue: 0);
                _currentGlyph = FA.Solid.Square;
                StartRecordingImage.Glyph = _currentGlyph;
                StartRecordingImage.Color = Color.FromRgb(red: 255, green: 255, blue: 255);


            }
            else
            {
               var recordedAudio = await _audioRecorder.StopAsync();

                StartRecording.BackgroundColor = Color.FromArgb("#2796F2");
                _currentGlyph = FA.Solid.Microphone;
                StartRecordingImage.Glyph = _currentGlyph;
                StartRecordingImage.Color = Color.FromRgb(red: 0, green: 0, blue: 0);

                var audioStream = recordedAudio.GetAudioStream();
                
               TranscribeRecordedAudio(audioStream);

               var player = AudioManager.Current.CreatePlayer(recordedAudio.GetAudioStream());

               //player.Play();
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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            StartRecordingImage.Glyph = FA.Solid.Microphone;
        }

        private void InputTextEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            string InputText = string.Empty;

            if (InputTextEditor.Text.Length != 0)
            {
                InputText = InputTextEditor.Text;
                //TranslateTextInput(InputText);
            }
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
    }

}