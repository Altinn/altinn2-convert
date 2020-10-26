using System;
using System.Collections.Generic;
using System.Text;

namespace Altinn2Convert.Configuration
{
    /// <summary>
    /// General settings for the application
    /// </summary>
    public class GeneralSettings
    {
        /// <summary>
        /// The name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The temporary directory containing extracted files
        /// </summary>
        public string TmpDir { get; set; }
    }
}
