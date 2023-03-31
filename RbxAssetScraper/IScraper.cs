using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RbxAssetScraper
{
    internal interface IScraper
    {
        public Task Start(string input);
        public string BuildDefaultOutputPath(string input);
    }
}
