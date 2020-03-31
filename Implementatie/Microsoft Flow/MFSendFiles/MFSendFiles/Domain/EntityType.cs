using Newtonsoft.Json;

namespace MFSendFiles.Domain
{
    public class EntityType
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
