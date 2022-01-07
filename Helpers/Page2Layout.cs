#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using Altinn2Convert.Models.Altinn2;
using Altinn2Convert.Models.Altinn3;
using Altinn2Convert.Models.Altinn3.layout;

namespace Altinn2Convert.Helpers
{
    public class Page2Layout
    {
        #pragma warning disable SA1311
        private readonly static XNamespace xd = "http://schemas.microsoft.com/office/infopath/2003";
        private readonly static XNamespace xsl = "http://www.w3.org/1999/XSL/Transform";
        #pragma warning restore SA1311

        public List<Component> Components { get; } = new ();

        public Queue<string> UnusedTexts { get; } = new ();

        public Dictionary<string, RadioButtonsComponent> HandeledRadioNames { get; } = new ();

        public XDocument Root { get; }

        public string Language { get; set; }

        public Page2Layout(XDocument root, string language)
        {
            Root = root;
            Language = language;
        }

        public class ConverterState
        {
            public ConverterState()
            {
            }

            public ConverterState(ConverterState state)
            {
                components = state.components;
                unusedTexts = state.unusedTexts;
                helpTextReference = state.helpTextReference;
                xPathPrefix = state.xPathPrefix;
            }

            public List<Component> components { get; set; } = new ();
            
            public Queue<string> unusedTexts { get; set; } = new ();
            
            public string? helpTextReference { get; set; }
            
            public string xPathPrefix { get; set; } = "";
        }

        public void GetLayoutComponentRecurs(XElement element, ConverterState state)
        {
            if (element.Attribute(xd + "binding")?.Value?.StartsWith("@my:") ?? false)
            {
                return;
            }

            // Try to find relevant elements that we extract components from.
            if (HandleImg(element, state))
            {
            }
            else if (HandleSelect(element, state))
            {
            }
            else if (HandleRadio(element, state))
            {
            }
            else if (HandleInputText(element, state))
            {
            }
            else if (HandlePlainText(element, state))
            {
            }
            else if (HandleHelpButton(element, state))
            {
            }
            else if (HandleTemplate(element, state))
            {
            }
            else if (HandleTable(element, state))
            {
            }
            else if (HandleTableRow(element, state))
            {
            }
            else
            {
                // If no match, we just recurse over all child elements making a group when we find a <table tag
                foreach (var node in element.Elements())
                {
                    // Default recursion
                    GetLayoutComponentRecurs(node, state);
                }
            }
        }

        public void FillLayoutComponents()
        {
            var table = Root.Descendants("table").FirstOrDefault();
            var state = new ConverterState()
            {
                components = Components,
                unusedTexts = UnusedTexts,
            };
            foreach (var element in table.Elements())
            {
                // Don't add a group for the outermost table
                GetLayoutComponentRecurs(element, state);
            }
            
            // Add next page button on the bottom
            Components.Add(new NavigationButtonsComponent
            {
                Id = "nav",
                TextResourceBindings = new Dictionary<string, string>
                {
                    { "next", "next" },
                    { "back", "back" }
                }
            });
        }

        public Dictionary<string, string> GetTextResouceBindings(ConverterState state, int keepCount = 0)
        {
            // Try to find a preceding ExpressionBox to get relevant text
            var textResourceBindings = new Dictionary<string, string>();
            if (state.unusedTexts.TryDequeue(out string? title))
            {
                textResourceBindings["title"] = title;
            }
            
            // Join all other texts from the same row into the description
            if (state.unusedTexts.Count > keepCount)
            {
                var description = new List<string>();
                while (state.unusedTexts.Count > keepCount)
                {
                    description.Add(state.unusedTexts.Dequeue());
                }

                textResourceBindings["description"] = string.Join('\n', description);
            }

            if (state.helpTextReference != null)
            {
                textResourceBindings["help"] = state.helpTextReference;
                state.helpTextReference = null;
            }

            return textResourceBindings;
        }

        #region xmlToComponents

        public bool HandleTableRow(XElement node, ConverterState state)
        {
            if (node.Name == "tr")
            {
                // Make a new queue of unused texts
                var trState = new ConverterState(state)
                {
                    unusedTexts = new Queue<string>()
                };
                foreach (var n in node.Elements())
                {
                    GetLayoutComponentRecurs(n, state);
                }

                while (trState.unusedTexts.Count > 0)
                {
                    // Add unused texts to the parent element
                    state.unusedTexts.Enqueue(trState.unusedTexts.Dequeue());
                }

                return true;
            }
            
            return false;
        }

        public bool HandleTable(XElement element, ConverterState state)
        {
            if (element.Name == "table")
            {
                var tableContent = new List<Component>();
                var tableUnusedTexts = new Queue<string>();
                var tableState = new ConverterState(state)
                {
                    components = tableContent,
                    unusedTexts = tableUnusedTexts,
                };
                foreach (var n in element.Elements())
                {
                    GetLayoutComponentRecurs(n, tableState);
                }

                // Create a Group if table contains more than 2 fields (and the first isn't a group)
                if (tableContent.Count > 2 && tableContent[0]?.Type != ComponentType.Group)
                {
                    state.components.Add(new GroupComponent()
                    {
                        Id = XElementToId(element),
                        Type = Models.Altinn3.layout.ComponentType.Group,
                        Children = tableContent.Select(c => c.Id).ToList(),
                        MaxCount = 1,
                    });
                }

                // A table with a single text element is a header
                if (tableContent.Count == 0 && tableUnusedTexts.Count == 1)
                {
                    state.components.Add(new HeaderComponent
                    {
                        Id = XElementToId(element),
                        TextResourceBindings = new Dictionary<string, string>
                        {
                            { "title", tableUnusedTexts.Dequeue() }
                        },
                        // Size = Size.L
                        AdditionalProperties = new Dictionary<string, object>
                        {
                            { "size", "L" },
                        }
                    });
                }

                while (tableUnusedTexts.Count > 0)
                {
                    // Add unused texts to the parent element
                    state.unusedTexts.Enqueue(tableUnusedTexts.Dequeue());
                }

                state.components.AddRange(tableContent);
                return true;
            }

            return false;
        }

        public bool HandleTemplate(XElement element, ConverterState state)
        {
            if (element.Name == xsl + "apply-templates")
            {
                var mode = element.Attribute("mode")?.Value;
                var select = element.Attribute("select")?.Value;
                var template = Root.Descendants(xsl + "template").FirstOrDefault(el => el.Attribute("mode")?.Value == mode);
                if (template != null)
                {
                    var child = template.Element("div");
                    // Repeating group
                    if (child != null && child.Attribute(xd + "xctname")?.Value == "RepeatingSection")
                    {
                        var templateState = new ConverterState(state)
                        {
                            xPathPrefix = "",
                            components = new (),
                        };
                        GetLayoutComponentRecurs(template, templateState);
                        state.components.Add(new GroupComponent()
                        {
                            Id = XElementToId(element),
                            Children = templateState.components.Select(c => c.Id).ToList(),
                            DataModelBindings = new Dictionary<string, string>
                            {
                                { "group", addXpathPrefix(state.xPathPrefix, select) }
                            },
                        });
                        state.components.AddRange(templateState.components);
                    }
                    else
                    {
                        // Non repeating group
                        var templateState = new ConverterState(state)
                        {
                            xPathPrefix = addXpathPrefix(state.xPathPrefix, select),
                        };
                        GetLayoutComponentRecurs(template, templateState);
                    }
                }
                
                return true;
            }

            return false;
        }

        public bool HandleHelpButton(XElement element, ConverterState state)
        {
            if (
                element.Name == "button" &&
                element.Attribute(xd + "xctname")?.Value == "PictureButton" &&
                element.Attribute(xd + "CtrlId") != null)
            {
                state.helpTextReference = element.Attribute(xd + "CtrlId").Value;
                return true;
            }

            return false;
        }

        public bool HandlePlainText(XElement element, ConverterState state)
        {
            if (element.Name == "a")
            {
                // Convert to markdown link
                state.unusedTexts.Enqueue($"[" + string.Concat(element.DescendantNodes().Where(n => n.NodeType == XmlNodeType.Text)) + "](" + element.Attribute("href")?.Value + ")");
                return true;
            }
            
            if (
                element.Name == "span" &&
                element.Attribute(xd + "xctname")?.Value == "ExpressionBox")
            {
                var binding = element.Attribute(xd + "binding");
                if (binding != null)
                {
                    state.unusedTexts.Enqueue(StripQuotes(binding.Value));
                    return true;
                }
                
                var valueOf = string.Join(" ", element.Descendants(xsl + "value-of").Select(node => node.Attribute("select")?.Value).Where(v => v != null));
                if (!string.IsNullOrWhiteSpace(valueOf))
                {
                    state.unusedTexts.Enqueue(StripQuotes(valueOf));
                    return true;
                }
            }

            return false;
        }

        public bool HandleSelect(XElement element, ConverterState state)
        {
            if (element.Name == "select")
            {
                var component = new DropdownComponent()
                {
                    Id = XElementToId(element),
                    DataModelBindings = new Dictionary<string, string>()
                    {
                        { "simpleBinding", xPathToJsonPath(state.xPathPrefix, element.Attribute(xd + "binding").Value) }
                    },
                    TextResourceBindings = GetTextResouceBindings(state),
                };
                
                var xslForEach = element.Descendants(xsl + "for-each");
                if (xslForEach.Any())
                {
                    var selectAttr = xslForEach.FirstOrDefault()?.Attribute("select")?.Value;
                    if (selectAttr != null)
                    {
                        var match = Regex.Match(selectAttr, @".*""(.*)"".*");
                        if (match.Success)
                        {
                            component.OptionsId = match.Groups[1].Value;
                        }
                    }
                }
                else
                {
                    component.Options = element.Descendants("option").Select(option =>
                    {
                        var label = string.Join(" ", option.Nodes().Where(node => node.NodeType == XmlNodeType.Text));
                        if (label != null)
                        {
                            return new Options
                            {
                                Label = label,
                                Value = option.Attribute("value")?.Value ?? "",
                            };
                        }

                        return null!; // Nulls are filtered on the next line.
                    }).Where(op => op != null).Select(op => op!).ToList(); 
                }

                state.components.Add(component);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Radio buttons are complicated, because there are no parent element I can stop the 
        /// depth first search and switch to parsing only radio button.
        /// thus I need to find the previous
        /// </summary>
        public bool HandleRadio(XElement element, ConverterState state)
        {
            if (
                element.Name == "input" &&
                element.Attribute("type")?.Value == "radio" &&
                element.Attribute("name") != null)
            {
                var name = element.Attribute("name")!.Value;
                // Get or initialize component
                RadioButtonsComponent radio;
                if (HandeledRadioNames.TryGetValue(name, out radio))
                {
                }
                else
                {
                    radio = new RadioButtonsComponent()
                    {
                        Id = XElementToId(element),
                        Options = new List<Options>(),
                        TextResourceBindings = GetTextResouceBindings(state, keepCount: 0),
                        DataModelBindings = new Dictionary<string, string>()
                        {
                            { "simpleBinding", xPathToJsonPath(state.xPathPrefix, element.Attribute(xd + "binding").Value) }
                        }
                    };
                    HandeledRadioNames[name] = radio;
                    state.components.Add(radio);
                }

                // Find the text label
                string? label = null;
                element.Ancestors("td").FirstOrDefault()?.NodesAfterSelf()?.OfType<XElement>()?.FirstOrDefault()?.Descendants(xsl + "value-of").ToList().ForEach((elm) =>
                {
                    label = StripQuotes(elm.Attribute("select")?.Value);
                });
                if (label == null)
                {
                    element.Ancestors("td").FirstOrDefault()?.NodesBeforeSelf()?.OfType<XElement>()?.FirstOrDefault()?.Descendants(xsl + "value-of").ToList().ForEach((elm) =>
                    {
                        label = StripQuotes(elm.Attribute("select")?.Value);
                    });
                }

                // Add this option
                radio.Options?.Add(new ()
                {
                    Label = label ?? element.Attribute(xd + "onValue")?.Value ?? "UKJENT",
                    Value = element.Attribute(xd + "onValue")?.Value ?? "",
                });

                return true;
            }

            return false;
        }

        public bool HandleInputText(XElement element, ConverterState state)
        {
            if (
                element.Name != "span" ||
                element.Attribute(xd + "xctname")?.Value != "PlainText" ||
                element.Attribute(xd + "binding") == null)
            {
                return false;
            }

            var textResourceBindings = GetTextResouceBindings(state);

            state.components.Add(new InputComponent()
            {
                Id = XElementToId(element),
                TextResourceBindings = textResourceBindings,
                DataModelBindings = new Dictionary<string, string>()
                {
                    { "simpleBinding", xPathToJsonPath(state.xPathPrefix, element.Attribute(xd + "binding").Value) },
                },
                ReadOnly = element.Attribute(xd + "disableEditing")?.Value == "yes",
            });
            return true;
        }

        public bool HandleImg(XElement element, ConverterState state)
        {
            if (element.Name != "img")
            {
                return false;
            }

            var src = element.Attribute("src")?.Value;
            if (src != null && !src.StartsWith("res://"))
            {
                var imageSrc = new Src();
                imageSrc[this.Language] = $"wwwroot/images/{src}";
                state.components.Add(new ImageComponent()
                {
                    Id = XElementToId(element),
                    Image = new ()
                    {
                        Src = imageSrc,
                        Align = ImageAlign.Center,
                        Width = "100%",
                    }
                });
            }
            
            return true;
        }

        #endregion

        public static string XElementToId(XElement element)
        {
            var id = element.GetAbsoluteXPath()
                .Replace("/xsl:stylesheet/xsl:template[1]/html/body/", "")
                .Replace("/xsl:stylesheet/xsl:template", "")
                .Replace("xsl:", "")
                .Replace('/', '-')
                .Replace("[", "")
                .Replace("]", "")
                .Replace(':', '-');
            if (id.StartsWith('-'))
            {
                return id.Substring(1);
            }

            return id;
        }

        public static string StripQuotes(string? value)
        {
            return Regex.Replace(value, @"^""(.*)""$", "$1");
        }

        public static string addXpathPrefix(string? xPathPrefix, string? value)
        {
            if (string.IsNullOrWhiteSpace(xPathPrefix))
            {
                return value ?? "";
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return xPathPrefix ?? "";
            }

            return xPathPrefix + "/" + value;
        }

        public static string xPathToJsonPath(string? xPathPrefix, string? value)
        {
            return Utils.UnbundlePath(addXpathPrefix(xPathPrefix, value))?.Replace('/', '.') + ".value";
        }
    }
}