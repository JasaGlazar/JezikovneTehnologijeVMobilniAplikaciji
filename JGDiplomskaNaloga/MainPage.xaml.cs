using CommunityToolkit.Maui.Views;
using Plugin.Maui.Audio;
using System.Diagnostics;

namespace JGDiplomskaNaloga
{
    public partial class MainPage : ContentPage
    {
        readonly IAudioManager _audioManager;
        readonly IAudioRecorder _audioRecorder;

        private static readonly string apiUrl = "https://transcriber-hgyyhzqswq-ew.a.run.app/api/transcribe";

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
            if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted)
            {
                //TODO: Userja opozori da nima vkloplenega mikrofona
            }
            if (!_audioRecorder.IsRecording)
            {
                await _audioRecorder.StartAsync();
            }
            else
            {
               var recordedAudio = await _audioRecorder.StopAsync();

               var audioStream = recordedAudio.GetAudioStream();
                

               var player = AudioManager.Current.CreatePlayer(recordedAudio.GetAudioStream());

               player.Play();
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


                            HttpResponseMessage responseMessage = await client.PostAsync(apiUrl, FormData);

                            if (responseMessage.IsSuccessStatusCode)
                            {
                                string transcriptionResult = await responseMessage.Content.ReadAsStringAsync();

                                InputTextEditor.Text = transcriptionResult;
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
    }

}
