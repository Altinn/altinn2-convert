using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Altinn2Convert.Models.Altinn2;
using Azure.Core;
using Azure.Identity;

namespace Altinn2Convert.Services
{
    /// <summary>
    /// Service that handles extraction of texts.
    /// </summary>
    public interface ITextService
    {
        /// <summary>
        /// Gets the form texts from an InfoPath xsn file
        /// </summary>
        /// <param name="formFiles">The InfoPath XSN files</param>
        /// <param name="translationFiles">The files with translations outside InfoPath</param>
        /// <returns>A collection of text resources</returns>
        public Dictionary<string, List<TextResourceItem>> GetTexts(List<ServiceFile> formFiles, List<ServiceFile> translationFiles);

    }
}
