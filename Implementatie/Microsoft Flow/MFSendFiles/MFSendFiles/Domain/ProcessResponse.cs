using Newtonsoft.Json;

namespace MFSendFiles.Domain
{
    public class ProcessResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("documents")]
        public Document[] Documents { get; set; }
    }
}
