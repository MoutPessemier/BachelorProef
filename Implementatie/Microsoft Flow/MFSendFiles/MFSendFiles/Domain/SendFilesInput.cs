using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MFSendFiles.Domain
{
    public class SendFilesInput
    {
        [JsonProperty("files")]
        [Required]
        public IEnumerable<string> Files { get; set; }

        [JsonProperty("organisationId")]
        [Required]
        public string OrganisationId { get; set; }

        [JsonProperty("projectId")]
        [Required]
        public string ProjectId { get; set; }

        [JsonProperty("bearerToken")]
        [Required]
        public string BearerToken { get; set; }
    }
}
