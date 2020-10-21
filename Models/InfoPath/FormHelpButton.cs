using System;
using System.Collections.Generic;
using System.Text;

namespace Altinn2Convert.Models.InfoPath
{
    /// <summary>
    /// Form help button
    /// </summary>
    public class FormHelpButton
    {
        /// <summary>
        /// Name of the help button
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Service Edition ID
        /// </summary>
        public int ServiceEditionID { get; set; }

        /// <summary>
        /// Page Name
        /// </summary>
        public string PageName { get; set; }
    }
}
