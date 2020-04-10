using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;

namespace AFSendFiles
{
    public static class TestPost
    {
        [FunctionName("TestPost")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            log.LogInformation("TestPost Triggered");

            var input = await req.Content.ReadAsStringAsync();
            Message m = JsonConvert.DeserializeObject<Message>(input);
            return req.CreateResponse(HttpStatusCode.OK, m);
        }
    }

    class Message
    {
        [JsonProperty("message")]
        public string MessageText { get; set; }
    }

}
