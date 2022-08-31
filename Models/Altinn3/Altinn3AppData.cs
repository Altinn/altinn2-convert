#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Altinn.Platform.Storage.Interface.Models;
using Layout = Altinn2Convert.Models.Altinn3.layout.Test;
using LayoutSettings = Altinn2Convert.Models.Altinn3.layoutSettings.Test;
using Prefill = Altinn2Convert.Models.Altinn3.prefill.Test;

namespace Altinn2Convert.Models.Altinn3
{
    public class Altinn3AppData
    {
        /// <summary>Layouts of the app</summary>
        public Dictionary<string, Layout> Layouts { get; set; } = new Dictionary<string, Layout>();

        /// <summary>Texts of the app</summary>
        public Dictionary<string, TextResource> Texts { get; set; } = new Dictionary<string, TextResource>();

        public LayoutSettings LayoutSettings { get; set; } = new() { Pages = new() { Order = new(), ExcludeFromPdf = new(), Triggers = new() } };

        public Prefill? Prefill { get; set; }

        /// <summary>map of filename in /App/models/[filename] and the string content of the file</summary>
        public Dictionary<string, string> ModelFiles { get; set; } = new();

        public string? ModelName { get; set; }

        public Application ApplicationMetadata { get; set; } = new();

        public PolicyUpdates PolicyUpdates { get; set; } = new();

        public Readme Readme { get; set; } = new();

        #region helper functions

        public void AddLayout(string page, Models.Altinn3.layout.Layout layout)
        {
            Layouts[page] = new() { Data = new() { Layout = layout } };
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
                Texts[lang] = new TextResource { Language = lang, Resources = new() };
            }

            Texts[lang].Resources.Add(new TextResourceItem { Id = id, Value = StripUselessHtml(value) });
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

        private Regex _htmlRegexWrappingDiv = new Regex(@"^<div>([^<]*)<\/div>$", RegexOptions.IgnoreCase);

        public string StripUselessHtml(string input)
        {
            // TODO: Find some better way to do (more of) this.
            var match = _htmlRegexWrappingDiv.Match(input);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return input;
        }

        public List<string> GetOptionIds()
        {
            return Layouts.Values.SelectMany(layout =>
                layout?.Data?.Layout?.Select(component => (component as Altinn3.layout.SelectionComponents)?.OptionsId) ?? new List<string>())
                .Where(c => c is not null).Select(c => c!) // Strip nulls and nullability
                .Distinct().ToList();
        }

        #endregion
    }
}