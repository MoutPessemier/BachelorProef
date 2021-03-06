using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System;
using System.Threading;
using Dropbox.Api;

namespace AFSendFiles
{
    public static class SendFiles
    {

        private static readonly HttpClient client = new HttpClient();

        [FunctionName("SendFiles")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            log.LogInformation("SendFiles Triggered");

            var token = Environment.GetEnvironmentVariable("DropboxKey");

            DropboxClient dropbox = new DropboxClient(token);

            string jsonInput = await req.Content.ReadAsStringAsync();
            SendFilesInput input = JsonConvert.DeserializeObject<SendFilesInput>(jsonInput);

            string url = "https://adp.faktion.com/gql/api/organisations/" + input.OrganisationId + "/projects/" + input.ProjectId + "/process";
            string response = null;
            bool succesfullRequest = false;
            MultipartFormDataContent formdata = new MultipartFormDataContent();
            log.LogInformation("Adding files to MultiPartFormData and sending the files");
            try
            {
                foreach (var filePath in input.Files)
                {
                    // create filestream content
                    string name = GetFileName(filePath);
                    var res = await dropbox.Files.DownloadAsync(filePath);
                    HttpContent content = new StreamContent(await res.GetContentAsStreamAsync());
                    content.Headers.Add("Content-Type", GetFileType(name));
                    formdata.Add(content, "files", name);
                }
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", input.BearerToken);
                // send content to the backend and parse result
                var resultPost = await client.PostAsync(url, formdata);
                response = await resultPost.Content.ReadAsStringAsync();
                succesfullRequest = resultPost.IsSuccessStatusCode;
            }
            // I absolutely want to catch every exception and pass these along to the workflow
            catch (Exception ex)
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }
            // if something went wrong in the backend, throw an error
            if (!succesfullRequest)
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Something went wrong during the upload process");
            }

            UploadResponse r = JsonConvert.DeserializeObject<UploadResponse>(response);

            // dirty solution: since we don't know how long the pipeline needs to process the upload, we'll be polling for a result
            // since this is a poc this is a temporary solution, if this gets released this needs to be rewritten (maybe with webhooks)
            var polling = true;
            int counter = 1;
            HttpResponseMessage result;
            string jsonString = "";
            ProcessResponse pr;
            succesfullRequest = false;
            log.LogInformation("Polling...");
            do
            {
                result = await client.GetAsync(url + "/" + r.UploadId);
                jsonString = await result.Content.ReadAsStringAsync();
                pr = JsonConvert.DeserializeObject<ProcessResponse>(jsonString);
                switch (pr.Status)
                {
                    case "DONE":
                        polling = false;
                        succesfullRequest = result.IsSuccessStatusCode;
                        break;
                    case "DOCUMENT_CLASSIFICATION_INTERVENTION":
                    case "ENTITY_EXTRACTION_INTERVENTION":
                        return req.CreateResponse(HttpStatusCode.BadRequest, "Intervention");
                    case "FAILED":
                        return req.CreateResponse(HttpStatusCode.BadRequest, "Something went wrong during the processing process");
                    default:
                        counter++;
                        // check status every 7 seconds
                        Thread.Sleep(7000);
                        break;
                }
            } while (polling && counter <= 75); // 7 sec * 75 = 525 seconds = 8:45 min
            if (counter == 75)
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Request Timeout: try again later.");
            }
            if (!succesfullRequest)
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Something went wrong when asking for the result of the pipeline");
            }

            return req.CreateResponse(HttpStatusCode.OK, pr);
        }

        private static string GetFileName(string path)
        {
            char[] charSeparators = new char[] { '/' };
            var splitPath = path.Split(charSeparators);
            return splitPath[splitPath.Length - 1];
        }

        private static string GetFileType(string name)
        {
            char[] charSeparators = new char[] { '.' };
            var splitName = name.Split(charSeparators);
            string extension = splitName[1].ToLower();
            switch (extension)
            {
                case "pdf":
                    return "application/pdf";
                case "png":
                    return "image/png";
                case "doc":
                    return "application/msword";
                case "docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case "txt":
                    return "text/plain";
                case "tif":
                    return "image/tif";
                case "jpg":
                    return "image/jpg";
                case "rtf":
                    return "application/rtf";
                // default == not supported type, we don't set content type
                default:
                    return "";
            }
        }
    }

    class SendFilesInput
    {
        [JsonProperty("files")]
        public IEnumerable<string> Files { get; set; }

        [JsonProperty("bearerToken")]
        public string BearerToken { get; set; }

        [JsonProperty("organisationId")]
        public string OrganisationId { get; set; }

        [JsonProperty("projectId")]
        public string ProjectId { get; set; }
    }

    class ProcessResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("documents")]
        public Document[] Documents { get; set; }
    }

    class UploadResponse
    {
        [JsonProperty("uploadId")]
        public string UploadId { get; set; }
    }

    class Document
    {
        [JsonProperty("entities")]
        public Entity[] Entities { get; set; }

        [JsonProperty("documentType")]
        public DocumentType DocumentType { get; set; }
    }

    class DocumentType
    {
        [JsonProperty("threshold")]
        public double Threshold { get; set; }
    }

    class Entity
    {
        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        [JsonProperty("entityType")]
        public EntityType Type { get; set; }
    }

    class EntityType
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
