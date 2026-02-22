using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivisionEngine.Projects.Assets
{
    public class FolderMetadata
    {
        public string FolderPath { get; set; } = string.Empty;
        public Dictionary<string, AssetMetadata> Assets { get; set; } = [];
        public DateTime LastScanTime { get; set; }
    }
}
