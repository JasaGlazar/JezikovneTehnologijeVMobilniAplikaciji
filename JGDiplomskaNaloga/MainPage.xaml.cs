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
        public MainPage(IAudioManager audioManager)
        {
            InitializeComponent();

            BindingContext = new ViewModels.MainPageViewModel(Navigation, audioManager);

        }

        private void SwapLanguages_Clicked(object sender, EventArgs e)
        {
            ImageSource tmp = ImgTranslateTo.Source;
            ImgTranslateTo.Source = ImgTranslateFrom.Source;
            ImgTranslateFrom.Source = tmp;
        }
    }

}