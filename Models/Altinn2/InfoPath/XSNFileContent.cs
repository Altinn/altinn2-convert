using System;
using System.Collections.Generic;

namespace Altinn2Convert.Models.Altinn2.InfoPath
{
    public class XSNFileContent
    {
        public List<FormView> Views { get; set; }

        public List<FormView> CodeListXML { get; set; }

        public List<FormField> FormFields { get; set; }

        public List<FormText> FormTexts { get; set; }

        public string MySchemaXsd { get; set; }
        
        public byte[] PrimaryXsd { get; set; } 
    }

}