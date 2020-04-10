using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.IO;
using System;
using System.Threading;
using Microsoft.SharePoint.Client;

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
            log.LogInformation("Work with SharePoint");

            //ClientContext context = new ClientContext("https://factionxyz0.sharepoint.com/sites/faktion-devs");
            
            string jsonInput = req.Content.ReadAsStringAsync().Result;
            SendFilesInput input = JsonConvert.DeserializeObject<SendFilesInput>(jsonInput);

            string url = "https://adp.faktion.com/gql/api/organisations/" + input.OrganisationId + "/projects/" + input.ProjectId + "/process";
            string response = null;
            bool succesfullRequest = false;
            MultipartFormDataContent formdata = new MultipartFormDataContent();
            log.LogInformation("Adding files to MultiPartFormData and sending the files");
            try
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IllNRUxIVDBndmIwbXhvU0RvWWZvbWpxZmpZVSIsImtpZCI6IllNRUxIVDBndmIwbXhvU0RvWWZvbWpxZmpZVSJ9.eyJhdWQiOiJodHRwczovL2ZhY3Rpb254eXowLW15LnNoYXJlcG9pbnQuY29tIiwiaXNzIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvMzhiMmNjZjEtMjliYS00Nzc0LWJmM2QtZDFkODE1M2M2OWVlLyIsImlhdCI6MTU4NjUwNzEyNCwibmJmIjoxNTg2NTA3MTI0LCJleHAiOjE1ODY1MTEwMjQsImFjciI6IjEiLCJhaW8iOiJBU1FBMi84UEFBQUFyN1pMU0pxOTBKREp4bnpOU0dMTVdDbk1CV3UvYVFtN3RLWUdtSGhMeklVPSIsImFtciI6WyJwd2QiXSwiYXBwX2Rpc3BsYXluYW1lIjoiU2hhcmVQb2ludCBPbmxpbmUgV2ViIENsaWVudCBFeHRlbnNpYmlsaXR5IiwiYXBwaWQiOiIwOGUxODg3Ni02MTc3LTQ4N2UtYjhiNS1jZjk1MGMxZTU5OGMiLCJhcHBpZGFjciI6IjAiLCJmYW1pbHlfbmFtZSI6IlBlc3NlbWllciIsImdpdmVuX25hbWUiOiJNb3V0IiwiaXBhZGRyIjoiNzguMjMuODAuMzUiLCJuYW1lIjoiTW91dCBQZXNzZW1pZXIiLCJvaWQiOiI1MGRkMDI2My02NDlkLTQ4OWEtOTUzMS1hNjM2NTg3N2U1YzYiLCJwdWlkIjoiMTAwMzIwMDA5QjkwRkU3NCIsInNjcCI6IkZpbGVzLlJlYWRXcml0ZS5BbGwgU2l0ZXMuRnVsbENvbnRyb2wuQWxsIFNpdGVzLlJlYWRXcml0ZS5BbGwgVGVybVN0b3JlLlJlYWRXcml0ZS5BbGwiLCJzaWQiOiI2ZDFiMTAyMi05OWZmLTRmZjAtOTVhZC04ZWRkYTI1Njk0OTAiLCJzdWIiOiJVYlh1RTNpUzhFaGFmclVWS0xfZ1NOYkdSMEx0Y0g3Uzc4eWR6OEgxMXZJIiwidGlkIjoiMzhiMmNjZjEtMjliYS00Nzc0LWJmM2QtZDFkODE1M2M2OWVlIiwidW5pcXVlX25hbWUiOiJtLnBlc3NlbWllckBmYWt0aW9uLmNvbSIsInVwbiI6Im0ucGVzc2VtaWVyQGZha3Rpb24uY29tIiwidXRpIjoiekVRNE1HQ3lIazY2M3k5elJjZ2VBQSIsInZlciI6IjEuMCJ9.LOhTUiZihPeW8UFLpp1us7C3acp5jAnCFQ_koDDkbnGiih7j0ZQJIDx66K-WjFP2HPWB0OaCKCzc7MA0JqNRlszfahuog5TDg4BrPpKbI_doQmIIw1inNAh8UB4jQ6c-s5W1sFoM20B3Clg5BN03C_MYkf-rjzEgob-IljTNaQ97fMpFSj7LyvCf8sbZI0ow5H1yo3Gqv2pfVsH4dNpDilN2UESQS497GlPjoi-dG1224F-cbMj_0IzPSDICekzWxBOBF6p4lsJ7Z-OO8rYb7CzjA8Lnt_kTojqmex47UEaGAlJU1uRNfes0sY6aT_t69Gcc-xRodNUwhP4ULniSWA");
                foreach (var filePath in input.Files)
                {
                    // create filestream content
                    //var res = await client.GetAsync("https://factionxyz0.sharepoint.com/sites/faktion-devs" + "/" + filePath);
                    //var stream = await res.Content.ReadAsStreamAsync();
                    //var tempfile = Path.GetTempFileName();

                    //Microsoft.SharePoint.Client.File temp = context.Web.GetFileByServerRelativeUrl(filePath);
                    //ClientResult<Stream> crstream = temp.OpenBinaryStream();
                    //context.Load(temp);
                    //context.ExecuteQuery();

                    //var tempfile = Path.GetTempFileName();
                    FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    //if(crstream.Value != null)
                    //{
                    //    crstream.Value.CopyTo(fs);
                    //}
                    HttpContent content = new StreamContent(fs);
                    string name = GetFileName(filePath);
                    content.Headers.Add("Content-Type", GetFileType(name));
                    formdata.Add(content, "files", name);
                    //File.Decrypt(tempfile);
                }
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", input.BearerToken);
                // send content to the backend and parse result
                var resultPost = client.PostAsync(url, formdata).Result;
                response = resultPost.Content.ReadAsStringAsync().Result;
                succesfullRequest = resultPost.IsSuccessStatusCode;
            }
            // I absolutely want to catch every exception and pass these along to the workflow
            catch (Exception ex)
            {
                req.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }
            // if something went wrong in the backend, throw an error
            if (!succesfullRequest)
            {
                req.CreateErrorResponse(HttpStatusCode.BadRequest, "Something went wrong during the upload process");
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
                result = client.GetAsync(url + "/" + r.UploadId).Result;
                jsonString = result.Content.ReadAsStringAsync().Result;
                pr = JsonConvert.DeserializeObject<ProcessResponse>(jsonString);
                switch (pr.Status)
                {
                    case "DONE":
                        polling = false;
                        succesfullRequest = result.IsSuccessStatusCode;
                        break;
                    case "DOCUMENT_CLASSIFICATION_INTERVENTION":
                    case "ENTITY_EXTRACTION_INTERVENTION":
                        req.CreateResponse(HttpStatusCode.BadRequest, "Intervention");
                        throw new Exception("Intervention");
                    case "FAILED":
                        req.CreateResponse(HttpStatusCode.BadRequest, "Something went wrong during the processing process");
                        throw new Exception("Something went wrong during the processing process");
                    default:
                        counter++;
                        break;
                }
                // check status every 7 seconds
                Thread.Sleep(7000);
            } while (polling && counter <= 50);
            if (counter == 50)
            {
                req.CreateErrorResponse(HttpStatusCode.BadRequest, "Request Timeout: try again later.");
            }
            if (!succesfullRequest)
            {
                req.CreateErrorResponse(HttpStatusCode.BadRequest, "Something went wrong when asking for the result of the pipeline");
            }

            return req.CreateResponse(HttpStatusCode.OK, pr);
        }

        private static string GetFileName(string path)
        {
            char[] charSeparators = new char[] { '\\' };
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
