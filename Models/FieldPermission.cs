using System;
using System.Collections.Generic;
using System.Text;

namespace Altinn2Convert.Models
{
    /// <summary>
    /// Encapsulates a business entity representing  a FieldPermission.
    /// </summary>
    public class FieldPermission
    {
        /// <summary>
        /// Gets or sets XPath for the field.
        /// </summary>
        public string XPath { get; set; }

        /// <summary>
        /// Gets or sets Permission for the field.
        /// </summary>
        public string Permission { get; set; }

        /// <summary>
        /// Gets or sets Operation for the field.
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// Gets or sets Permission on Operation for the field.
        /// </summary>
        public int PermissionOnOperation { get; set; }
    }

    /// <summary>
    /// List of FieldPermissionBEs.
    /// </summary>
    public class FieldPermissionList : List<FieldPermission>
    {
    }
}
