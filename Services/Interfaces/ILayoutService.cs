using System;
using System.Collections.Generic;
using System.Text;

using Altinn2Convert.Models;

namespace Altinn2Convert.Services
{
    /// <summary>
    /// Service that handles extraction of layout
    /// </summary>
    public interface ILayoutService
    {
        /// <summary>
        /// Gets the form layout from infopath views
        /// </summary>
        /// <param name="xsnPath">Path to xsn file</param>
        /// <returns>The layout</returns>
        public Dictionary<string, FormLayout> GetLayout(string xsnPath);
    }
}
