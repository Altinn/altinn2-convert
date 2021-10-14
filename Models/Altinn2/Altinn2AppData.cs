using System.Collections.Generic;

using Altinn2Convert.Models.Altinn2.InfoPath;

namespace Altinn2Convert.Models.Altinn2
{
    public class Altinn2AppData
    {
        public ServiceEditionVersion ServiceEditionVersion { get; set; }

        public Dictionary<LanguageEnum, XSNFileContent> XSNFiles { get; set; } = new Dictionary<LanguageEnum, XSNFileContent>();

        public enum LanguageEnum
        {
            en = 1033,
            nb = 1044,
            nn = 2068,
        }
    }
}