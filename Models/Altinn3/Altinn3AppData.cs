using System.Collections.Generic;
using Layout = Altinn2Convert.Models.Altinn3.layout.Test;

namespace Altinn2Convert.Models.Altinn3
{
    public class Altinn3AppData
    {
        /// <summary>
        /// Name of the app
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>Layouts of the app</summary>
        public Dictionary<string, Layout> Layouts { get; set; }
        
        /// <summary>Texts of the app</summary>
        public Dictionary<string, TextResource> Texts { get; set; }
    }
}