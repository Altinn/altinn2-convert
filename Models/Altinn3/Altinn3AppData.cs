using System;
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
        
        public LayoutSettings LayoutSettings { get; set; } = new(){ Pages= new(){ Order = new() }};

        #region helper functions

        public void AddLayout(string page, Models.Altinn3.layout.Layout layout)
        {
            Layouts[page] = new(){ Data = new(){ Layout = layout }};
            LayoutSettings?.Pages?.Order?.Add(page);
        }

        public void AddText(string lang, string id, string value)
        {
            if (value == null)
            {
                return;
            }
            
            if (!Texts.ContainsKey(lang))
            {
                Texts[lang] = new TextResource {Language = lang, Resources = new ()};
            }

            Texts[lang].Resources.Add(new TextResourceItem {Id = id, Value = value});
        }

        /// <summary>Add texts</summary>
        /// <parameter>Texts[lang][key] = text</parameter>
        public void AddTexts(Dictionary<string, Dictionary<string, string>> texts)
        {
            foreach (var (lang, keyText) in texts)
            {
                foreach (var (key, text) in keyText)
                {
                    AddText(lang, key, text);
                }
            }
        }

        #endregion
    }
}