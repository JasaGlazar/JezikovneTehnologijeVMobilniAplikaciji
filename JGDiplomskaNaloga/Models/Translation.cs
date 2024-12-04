using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JGDiplomskaNaloga.Models
{
    internal class Translation
    {
        public string SourceText { get; set; }
        public string TranslatedText { get; set; }
        public string SourceLanguage { get; set; }
        public string TargetLanguage { get; set; }

        public Translation(string sourceText, string translatedText, string sourceLanguage, string targetLanguage)
        {
            SourceText = sourceText;
            TranslatedText = translatedText;
            SourceLanguage = sourceLanguage;
            TargetLanguage = targetLanguage;
        }
    }
}
