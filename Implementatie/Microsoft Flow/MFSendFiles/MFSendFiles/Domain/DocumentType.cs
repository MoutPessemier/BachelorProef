using Newtonsoft.Json;

namespace MFSendFiles.Domain
{
    public class DocumentType
    {
        [JsonProperty("threshold")]
        public double Threshold { get; set; }
    }
}
