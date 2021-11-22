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
        readonly static XNamespace xd = "http://schemas.microsoft.com/office/infopath/2003";
        readonly static XNamespace xsl = "http://www.w3.org/1999/XSL/Transform";
        #pragma warning restore SA1311

        public List<Component> Components { get; } = new();

        public Queue<string> UnusedTexts { get; } = new();

        public Dictionary<string, RadioButtonsComponent> HandeledRadioNames { get; } = new();

        private int idCounter = 0;

        public XDocument Root { get; }

        public Page2Layout(XDocument root)
        {
            Root = root;
        }

        public void GetLayoutComponentRecurs(XElement element, List<Component> components, Queue<string> unusedTexts, ref string helpTextReference)
        {
            // Try to find relevant elements that we extract components from.
            if (HandleImg(element, components))
            {
            }
            else if (HandleSelect(element, components, unusedTexts, ref helpTextReference))
            {
            }
            else if (HandleRadio(element, components, unusedTexts, ref helpTextReference))
            {
            }
            else if (HandleInputText(element, components, unusedTexts, ref helpTextReference))
            {
            }
            else if (HandlePlainText(element, unusedTexts))
            {
            }
            else if (HandleHelpButton(element, ref helpTextReference))
            {
            }
            else if (HandleTemplate(element, components, unusedTexts, ref helpTextReference))
            {
            }
            else
            {
                // If no match, we just recurse over all child elements making a group when we find a <table tag
                foreach (var node in element.Elements())
                {
                    // Collect table nodes in an Altinn3 group.
                    if (node.Name == "table")
                    {
                        var tableContent = new List<Component>();
                        var tableUnusedTexts = new Queue<string>();
                        GetLayoutComponentRecurs(node, tableContent, tableUnusedTexts, ref helpTextReference);

                        // Create a Group if table contains more than 2 fields (and the first isn't a group)
                        if (tableContent.Count > 2 && tableContent[0]?.Type != ComponentType.Group)
                        {
                            components.Add(new GroupComponent()
                            {
                                Id = $"group-{idCounter++}",
                                Type = Models.Altinn3.layout.ComponentType.Group,
                                Children = tableContent.Select(c => c.Id).ToList(),
                                MaxCount = 1,
                            });
                        }

                        // A table with a single text element is a header
                        if (tableContent.Count == 0 && tableUnusedTexts.Count == 1)
                        {
                            components.Add(new HeaderComponent
                            {
                                Id = $"header-{idCounter++}",
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
                            unusedTexts.Enqueue(tableUnusedTexts.Dequeue());
                        }

                        components.AddRange(tableContent);
                    }
                    else if (node.Name == "tr")
                    {
                        // Make a new queue of unused texts
                        var trUnusedTexts = new Queue<string>();
                        GetLayoutComponentRecurs(node, components, trUnusedTexts, ref helpTextReference);
                        while (trUnusedTexts.Count > 0)
                        {
                            // Add unused texts to the parent element
                            unusedTexts.Enqueue(trUnusedTexts.Dequeue());
                        }
                    }
                    else
                    {
                        // Default recursion
                        GetLayoutComponentRecurs(node, components, unusedTexts, ref helpTextReference);
                    }
                }
            }
        }

        public void FillLayoutComponents()
        {
            var table = Root.Descendants("table").FirstOrDefault();
            string helpTextReference = null;
            GetLayoutComponentRecurs(table, Components, UnusedTexts, ref helpTextReference);
            
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

        public Dictionary<string, string> GetTextResouceBindings(Queue<string> unusedTexts, ref string helpTextReference, int keepCount = 0)
        {
            // Try to find a preceding ExpressionBox to get relevant text
            var textResourceBindings = new Dictionary<string, string>();
            if (unusedTexts.TryDequeue(out string title))
            {
                textResourceBindings["title"] = title;
            }
            
            // Join all other texts from the same row into the description
            if (unusedTexts.Count > keepCount)
            {
                var description = new List<string>();
                while (unusedTexts.Count > keepCount)
                {
                    description.Add(unusedTexts.Dequeue());
                }

                textResourceBindings["description"] = string.Join('\n', description);
            }

            if (helpTextReference != null)
            {
                textResourceBindings["help"] = helpTextReference;
                helpTextReference = null;
            }

            return textResourceBindings;
        }

        #region xmlToComponents

        public bool HandleTemplate(XElement element, List<Component> components, Queue<string> unusedTexts, ref string helpTextReference)
        {
            if (element.Name == xsl + "apply-templates")
            {
                var mode = element.Attribute("mode")?.Value;
                var select = element.Attribute("select");
                var template = Root.Descendants(xsl + "template").FirstOrDefault(el => el.Attribute("mode")?.Value == mode);
                if (template != null)
                {
                    GetLayoutComponentRecurs(template, components, unusedTexts, ref helpTextReference);
                }
                
                return true;
            }

            return false;
        }

        public bool HandleHelpButton(XElement element, ref string helpTextReference)
        {
            if (
                element.Name == "button" &&
                element.Attribute(xd + "xctname")?.Value == "PictureButton" &&
                element.Attribute(xd + "CtrlId") != null
            )
            {
                helpTextReference = element.Attribute(xd + "CtrlId").Value;
                return true;
            }

            return false;
        }

        public bool HandlePlainText(XElement element, Queue<string> unusedTexts)
        {
            if (element.Name == "a")
            {
                // Convert to markdown link
                unusedTexts.Enqueue($"[" + string.Concat(element.DescendantNodes().Where(n => n.NodeType == XmlNodeType.Text)) + "](" + element.Attribute("href")?.Value + ")");
                return true;
            }
            
            if (
                element.Name == "span" &&
                element.Attribute(xd + "xctname")?.Value == "ExpressionBox"
            )
            {
                var binding = element.Attribute(xd + "binding");
                if (binding != null)
                {
                    unusedTexts.Enqueue(StripQuotes(binding.Value));
                    return true;
                }
                
                var valueOf = string.Join(" ", element.Descendants(xsl + "value-of").Select(node => node.Attribute("select")?.Value).Where(v => v != null));
                if (!string.IsNullOrWhiteSpace(valueOf))
                {
                    unusedTexts.Enqueue(StripQuotes(valueOf));
                    return true;
                }
            }

            return false;
        }

        public bool HandleSelect(XElement element, List<Component> components, Queue<string> unusedTexts, ref string helpTextReference)
        {
            if (element.Name == "select" )
            {
                var component = new DropdownComponent()
                {
                    Id = XElementToId(element),
                    DataModelBindings = new Dictionary<string, string>()
                    {
                        { "simpleBinding", xPathToJsonPath(element.Attribute(xd + "binding").Value) }
                    },
                    TextResourceBindings = GetTextResouceBindings(unusedTexts, ref helpTextReference),
                };
                
                var xslForEach = element.Descendants(xsl + "for-each");
                if(xslForEach.Any())
                {
                    var selectAttr = xslForEach.FirstOrDefault()?.Attribute("select")?.Value;
                    if (selectAttr != null)
                    {
                        var match = Regex.Match(selectAttr, @".*""(.*)"".*");
                        if(match.Success)
                        {
                            component.OptionsId = match.Groups[1].Value;
                        }
                    }
                }
                else
                {
                    component.Options = element.Descendants("option").Select(option =>
                    {
                        var label = string.Join(" ", option.Nodes().Where(node => node.NodeType == XmlNodeType.Text ));
                        if (label != null)
                        {
                            return new Options
                            {
                                Label = label,
                                Value = option.Attribute("value")?.Value ?? string.Empty,
                            };
                        }

                        return null!; // Nulls are filtered on the next line.
                    }).Where(op => op != null).Select(op => op!).ToList(); 
                }

                components.Add(component);
                return true;
            }


            // TOOD: Implement
            return false;
        }

        /// <summary>
        /// Radio buttons are complicated, because there are no parent element I can stop the 
        /// depth first search and switch to parsing only radio button.
        /// thus I need to find the previous
        /// </summary>
        public bool HandleRadio(XElement element, List<Component> componetns, Queue<string> unusedTexts, ref string helpTextReference)
        {
            if (
                element.Name == "input" &&
                element.Attribute("type")?.Value == "radio" &&
                element.Attribute("name") != null
            )
            {
                var name = element.Attribute("name").Value;
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
                        TextResourceBindings = GetTextResouceBindings(unusedTexts, ref helpTextReference, keepCount: 0),
                        DataModelBindings = new Dictionary<string, string>()
                        {
                            { "simpleBinding", xPathToJsonPath(element.Attribute(xd + "binding").Value) }
                        }
                    };
                    HandeledRadioNames[name] = radio;
                    componetns.Add(radio);
                }

                // Find the text label
                string label = null;
                element.Ancestors("td").FirstOrDefault().NodesAfterSelf()?.OfType<XElement>()?.FirstOrDefault()?.Descendants(xsl + "value-of").ToList().ForEach((elm) =>
                {
                    label = StripQuotes(elm.Attribute("select")?.Value);
                });
                if (label == null)
                {
                    element.Ancestors("td").FirstOrDefault().NodesBeforeSelf()?.OfType<XElement>()?.FirstOrDefault()?.Descendants(xsl + "value-of").ToList().ForEach((elm) =>
                    {
                        label = StripQuotes(elm.Attribute("select")?.Value);
                    });
                }

                // Add this option
                radio.Options?.Add(new()
                {
                    Label = label ?? element.Attribute(xd + "onValue").Value,
                    Value = element.Attribute(xd + "onValue").Value,
                });

                return true;
            }

            return false;
        }

        public bool HandleInputText(XElement element, List<Component> components, Queue<string> unusedTexts, ref string helpTextReference)
        {
            if (
                element.Name != "span" ||
                element.Attribute(xd + "xctname")?.Value != "PlainText" ||
                element.Attribute(xd + "binding") == null)
            {
                return false;
            }

            var textResourceBindings = GetTextResouceBindings(unusedTexts, ref helpTextReference);

            components.Add(new InputComponent()
            {
                Id = XElementToId(element),
                TextResourceBindings = textResourceBindings,
                DataModelBindings = new Dictionary<string, string>()
                {
                    { "simpleBinding", xPathToJsonPath(element.Attribute(xd + "binding").Value) },
                },
                ReadOnly = element.Attribute(xd + "disableEditing")?.Value == "yes",
            });
            return true;
        }

        public bool HandleImg(XElement element, List<Component> components)
        {
            if (element.Name != "img")
            {
                return false;
            }

            var src = element.Attribute("src")?.Value;
            if (src != null && !src.StartsWith("res://"))
            {
                components.Add(new ImageComponent()
                {
                    Id = XElementToId(element),
                    Image = new ()
                    {
                        Src = new () { Nb = $"images/{ src }" },
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
                .Replace("/xsl:stylesheet/xsl:template[1]/html/body/", string.Empty)
                .Replace("/xsl:stylesheet/xsl:template", string.Empty)
                .Replace("xsl:", string.Empty)
                .Replace('/', '-')
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Replace(':', '-');
            if (id.StartsWith('-'))
            {
                return id.Substring(1);
            }

            return id;
        }

        public static string StripQuotes(string value)
        {
            return Regex.Replace(value, @"^""(.*)""$", "$1");
        }

        public static string xPathToJsonPath(string value)
        {
            return Utils.UnbundlePath(value)?.Replace('/', '.') + ".value";
        }
    }
}