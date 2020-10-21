using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altinn2Convert.Helpers
{
    /// <summary>
    /// Collection of utility methods
    /// </summary>
    public class Utils
    {
        /// <summary>
        /// Method to nest up relative paths (XPaths).
        /// <para>
        /// Example:
        /// input:  "/root/group1/child1/../../group2/field1"
        /// output: "/root/group2/field1" 
        /// </para>  
        /// <para>
        /// Empty or NULL strings will be returned as-is
        /// </para>
        /// </summary>
        /// <param name="path">The string with a relative path, e.g. "/message/delivery/form/entity/employeeTax/@my:container/../@my:section/../@my:attribute"</param>
        /// <returns>The un-nested path, e.g. "/message/delivery/form/entity/employeeTax/@my:attribute"</returns>
        public static string UnbundlePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            string[] parts = path.Split('/');

            Stack<string> pathElements = new Stack<string>();

            foreach (string item in parts)
            {
                if ("..".Equals(item) && pathElements.Count > 0 && !string.IsNullOrEmpty(pathElements.Peek()))
                {
                    pathElements.Pop();
                }
                else if ("..".Equals(item))
                {
                    // don't add unwanted items to the stack
                }
                else if (".".Equals(item))
                {
                    // don't add unwanted items to the stack
                }
                else
                {
                    pathElements.Push(item);
                }
            }

            StringBuilder absolutePath = new StringBuilder();
            pathElements.Reverse();

            foreach (string item in pathElements.Reverse())
            {
                absolutePath.Append(item + "/");
            }

            absolutePath.Remove(absolutePath.Length - 1, 1); // remove the last "/" since we allways add an extra "/" at the end in the construction phase.            

            return absolutePath.ToString();
        }
    }
}
