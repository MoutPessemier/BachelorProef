using Newtonsoft.Json;

namespace MFSendFiles.Domain
{
    public class Document
    {
        [JsonProperty("entities")]
        public Entity[] Entities { get; set; }

        [JsonProperty("documentType")]
        public DocumentType DocumentType { get; set; }
    }
}
