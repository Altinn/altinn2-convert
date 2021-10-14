using System;
using System.Threading.Tasks;

using Altinn2Convert.Models.Altinn2;
using Altinn2Convert.Models.Altinn3;

namespace Altinn2Convert.Services
{
    public class ConvertService
    {
        public async Task<Altinn2AppData> ParseAltinn2File(string path)
        {
            var A2 = new Altinn2AppData();
            var tmpFile = $"tmpFile{(new Random()).Next(10000)}";
            
            return A2;
        }
        public async Task<Altinn3AppData> Convert(Altinn2AppData A2)
        {
            var A3 = new Altinn3AppData();
            return A3;
        }
        public async Task WriteAltinn3Files(Altinn3AppData A3, string path)
        {

        }
    }
}