using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace MetaMaze {
    public static class TestGet {
        [FunctionName ("TestGet")]
        public static async Task<HttpResponseMessage> Run (
            [HttpTrigger (AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestMessage req,
            ILogger log) {
            log.LogInformation ("C# HTTP trigger function processed a request.");
            log.LogInformation ("TestGet Triggered");

            return req.CreateResponse (HttpStatusCode.OK, "TestGet");
        }
    }
}