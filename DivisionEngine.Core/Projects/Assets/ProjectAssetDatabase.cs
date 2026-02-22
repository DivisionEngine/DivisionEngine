using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivisionEngine.Projects.Assets
{
    public class ProjectAssetDatabase
    {
        public string ProjectPath { get; set; } = string.Empty;
        public Dictionary<string, FolderMetadata> Folders { get; set; } = []; // Key = relative folder path
        public Dictionary<string, AssetMetadata> AllAssetsByGuid { get; set; } = []; // Master GUID l
    }
}
