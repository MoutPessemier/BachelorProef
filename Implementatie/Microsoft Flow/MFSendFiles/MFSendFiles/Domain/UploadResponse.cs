using Newtonsoft.Json;

namespace MFSendFiles.Domain
{
    public class UploadResponse
    {
        [JsonProperty("uploadId")]
        public string UploadId { get; set; }
    }
}
