using System;
using System.Activities;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SendFiles
{
    public class SendFiles : CodeActivity
    {
        private readonly HttpClient client = new HttpClient();

        [Category("Input")]
        [RequiredArgument]
        public InArgument<IEnumerable<string>> Files { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public string OrganisationId { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public string ProjectId { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public string BearerToken { get; set; }

        [Category("Output")]
        public OutArgument<bool> SendMail { get; set; }

        [Category("Output")]
        public OutArgument<string> UncertainEntities { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
            string url = "https://adp.faktion.com/gql/api/organisations/" + OrganisationId + "/projects/" + ProjectId + "/process";
            string response = null;
            bool succesfullRequest = false;
            MultipartFormDataContent formdata = new MultipartFormDataContent();
            try
            {
                foreach (var filePath in Files.Get(context))
                {
                    // create filestream content
                    FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    HttpContent content = new StreamContent(fs);
                    string name = GetFileName(filePath);
                    content.Headers.Add("Content-Type", GetFileType(name));
                    formdata.Add(content, "files", name);
                }
                // send content to the backend and parse result
                var resultPost = client.PostAsync(url, formdata).Result;
                response = resultPost.Content.ReadAsStringAsync().Result;
                succesfullRequest = resultPost.IsSuccessStatusCode;
            }
            catch (TimeoutException e)
            {
                /* Add a retry count of 3*/
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw;
            }
            // I absolutely want to catch every exception and pass these along to the workflow
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
            // if something went wrong in the backend, throw an error
            if (!succesfullRequest)
            {
                throw new Exception("Something went wrong during the upload process");
            }

            UploadResponse r = JsonConvert.DeserializeObject<UploadResponse>(response);

            // dirty solution: since we don't know how long the pipeline needs to process the upload, we'll be polling for a result
            // since this is a poc this is a temporary solution, if this gets released this needs to be rewritten (maybe with webhooks)
            var polling = true;
            int counter = 1;
            do
            {
                response = client.GetAsync(url + "/" + r.UploadId).Result.Content.ReadAsStringAsync().Result;
                ProcessResponse pre = JsonConvert.DeserializeObject<ProcessResponse>(response);
                switch (pre.Status)
                {
                    case "DONE":
                        polling = false;
                        break;
                    case "DOCUMENT_CLASSIFICATION_INTERVENTION":
                    case "ENTITY_EXTRACTION_INTERVENTION":
                        throw new Exception("Intervention");
                    case "FAILED":
                        throw new Exception("Something went wrong during the processing process");
                    default:
                        counter++;
                        break;
                }
                // check status every 7 seconds
                Thread.Sleep(7000);
            } while (polling && counter <= 150);
            if (counter == 150)
            {
                throw new Exception("Request Timeout: try again later.");
            }
            // Because we know that there is a response now, actually execute the request
            var result = client.GetAsync(url + "/" + r.UploadId).Result;
            string jsonString = result.Content.ReadAsStringAsync().Result;
            succesfullRequest = result.IsSuccessStatusCode;
            if (!succesfullRequest)
            {
                throw new Exception("Something went wrong when asking for the result of the pipeline");
            }
            ProcessResponse pr = JsonConvert.DeserializeObject<ProcessResponse>(jsonString);
            try
            {
                // for this demo, check if a entity confidence is under the threshold
                // if so, send a mail and gather all entity names
                IList<string> uncertain = new List<string>();
                foreach (var document in pr.Documents)
                {
                    foreach (var entity in document.Entities)
                    {
                        if (entity.Confidence < document.DocumentType.Threshold)
                        {
                            uncertain.Add(entity.Type.Name);
                        }
                    }
                }
                if (uncertain.Count != 0)
                {
                    SendMail.Set(context, true);
                    UncertainEntities.Set(context, String.Join(",", uncertain));
                }
                else
                {
                    SendMail.Set(context, false);
                    UncertainEntities.Set(context, "");
                }
            }
            // again, I absolutely want to catch every exception and pass these along to the workflow
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private string GetFileName(string path)
        {
            char[] charSeparators = new char[] { '\\' };
            var splitPath = path.Split(charSeparators);
            return splitPath[splitPath.Length - 1];
        }

        private string GetFileType(string name)
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

    class UploadResponse
    {
        [JsonProperty("uploadId")]
        public string UploadId { get; set; }
    }

    class ProcessResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("documents")]
        public Document[] Documents { get; set; }
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
