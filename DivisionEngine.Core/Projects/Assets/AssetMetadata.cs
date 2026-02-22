namespace DivisionEngine.Projects.Assets
{
    public class AssetMetadata
    {
        public string ID { get; set; } = Guid.CreateVersion7().ToString();
        public string FileName { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public AssetType Type { get; set; } = AssetType.None;
        public DateTime LastModified { get; set; }
        public long FileSize { get; set; }
        public List<string> Tags { get; set; } = [];
        public Dictionary<string, object> CustomProperties { get; set; } = [];
    }
}
