using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RbxAssetScraper.Models
{
    internal class CommandInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public CommandInfo(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
