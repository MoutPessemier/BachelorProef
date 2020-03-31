using Newtonsoft.Json;

namespace MFSendFiles.Domain
{
    public class Entity
    {
        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        [JsonProperty("entityType")]
        public EntityType Type { get; set; }
    }
}
