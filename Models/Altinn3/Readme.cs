using System.Collections.Generic;

namespace Altinn2Convert.Models.Altinn3
{
    public class Readme
    {
        public string Org { get; set; }

        public string App { get; set; }

        public List<string> Authenticationlevels { get; set; }

        public List<string> RoleCodes { get; set; }
    }
}