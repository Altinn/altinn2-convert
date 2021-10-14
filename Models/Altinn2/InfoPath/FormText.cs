using System;
using System.Collections.Generic;
using System.Text;

using Altinn2Convert.Enums;

namespace Altinn2Convert.Models.Altinn2.InfoPath
{
    /// <summary>
    /// Form text
    /// </summary>
    public class FormText
    {
        /// <summary>
        /// Text type for the text query.
        /// </summary>        
        public TextType TextType { get; set; }

        /// <summary>
        /// Text code of the text query.
        /// </summary>        
        public string TextCode { get; set; }

        /// <summary>
        /// Text content.
        /// </summary>        
        public string TextContent { get; set; }

        /// <summary>
        /// Name of the Page
        /// </summary>        
        public string Page { get; set; }
    }
}
