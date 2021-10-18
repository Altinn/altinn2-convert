using System.Collections.Generic;
using Layout = Altinn2Convert.Models.Altinn3.layout.Test;
using LayoutSettings = Altinn2Convert.Models.Altinn3.layoutSettings.Test;

namespace Altinn2Convert.Models.Altinn3
{
    public class Altinn3AppData
    {
        /// <summary>
        /// Name of the app
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>Layouts of the app</summary>
        public Dictionary<string, Layout> Layouts { get; set; } = new Dictionary<string, Layout>();
        
        /// <summary>Texts of the app</summary>
        public Dictionary<string, TextResource> Texts { get; set; } = new Dictionary<string, TextResource>();
        
        public LayoutSettings LayoutSettings { get; set; } = new LayoutSettings();

        #region helper functions

        public void AddText(string lang, string id, string value)
        {
            if (!Texts.ContainsKey(lang))
            {
                Texts[lang] = new TextResource{ Language = lang, Resources = new ()};
            }

            Texts[lang].Resources.Add(new TextResourceItem{Id = id, Value = value});
        }

        public void AddPage(string id)
        {
            Layouts[id] = new Layout()
            {
                Data = new layout.Data(),
            };
        }

        #endregion
    }
}