using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Altinn2Convert.Models.Altinn2;

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

            if (absolutePath.Length == 0)
            {
                return "";
            }

            absolutePath.Remove(absolutePath.Length - 1, 1); // remove the last "/" since we allways add an extra "/" at the end in the construction phase.            

            return absolutePath.ToString();
        }

        /// <summary>
        ///  Set up InfoPath parser, extract 
        /// </summary>
        /// <param name="zipPath">Path to service zip file</param>
        /// <param name="outputPath">Path to store output files</param>
        /// <param name="command">The command that was used</param>
        /// <param name="tmpDir">The temporary directory where extracted files are stored</param>
        public static ServiceEditionVersion RunSetup(string zipPath, string outputPath, string command, string tmpDir)
        {
            if (File.Exists(zipPath))
            {
                ZipFile.ExtractToDirectory(zipPath, tmpDir);
                SetupOutputDir(outputPath, command);
                using var fileStream = File.Open(Path.Join(tmpDir, "manifest.xml"), FileMode.Open);
                XmlSerializer serializer = new XmlSerializer(typeof(ServiceEditionVersion));
                ServiceEditionVersion serviceEditionVersion = (ServiceEditionVersion)serializer.Deserialize(fileStream);
                return serviceEditionVersion;
            }

            throw new Exception("Unable to extract service from provided zip file path. Please check that the path is correct.");
        }

        private static void SetupOutputDir(string outputPath, string command)
        {
            string fullPath = command switch
            {
                "texts" => Path.Join(outputPath, "config", "texts"),
                "layout" => Path.Join(outputPath, "ui", "layouts"),
                _ => outputPath
            };

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }
    }
}
