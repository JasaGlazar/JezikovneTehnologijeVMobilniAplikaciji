namespace JGDiplomskaNaloga
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void SwapLanguages_Clicked(object sender, EventArgs e)
        {
            string tmp = TranslateTo.Text;
            TranslateTo.Text = TranslateFrom.Text;
            TranslateFrom.Text = tmp;
        }

        private void StartRecording_Clicked(object sender, EventArgs e)
        {

        }
    }

}
