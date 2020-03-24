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
using System.Data;

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
        public InArgument<string> OrganisationId { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> ProjectId { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> BearerToken { get; set; }

        [Category("Output")]
        public OutArgument<DataTable> JsonResult { get; set; }


        protected override void Execute(CodeActivityContext context)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken.Get(context));
            string url = "https://adp.faktion.com/gql/api/organisations/" + OrganisationId.Get(context) + "/projects/" + ProjectId.Get(context) + "/process";
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
                /* TODO: Add a retry count of 3*/
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
            HttpResponseMessage result;
            string jsonString = "";
            ProcessResponse pr;
            succesfullRequest = false;
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
                        throw new Exception("Intervention");
                    case "FAILED":
                        throw new Exception("Something went wrong during the processing process");
                    default:
                        counter++;
                        // check status every 7 seconds
                        Thread.Sleep(7000);
                        break;
                }
            } while (polling && counter <= 150);
            if (counter == 150)
            {
                throw new Exception("Request Timeout: try again later.");
            }
            if (!succesfullRequest)
            {
                throw new Exception("Something went wrong when asking for the result of the pipeline");
            }
            try
            {
                DataTable dataTable = new DataTable();
                dataTable.Clear();
                dataTable.Columns.Add("EntityName");
                dataTable.Columns.Add("Confidence");
                dataTable.Columns.Add("Threshold");

                // we make a data table with all the entities in it so that we can do whatever we want with them in the rest of our workflow
                foreach (var document in pr.Documents)
                {
                    foreach (var entity in document.Entities)
                    {
                        DataRow row = dataTable.NewRow();
                        row["EntityName"] = entity.Type.Name;
                        row["Confidence"] = entity.Confidence;
                        row["Threshold"] = document.DocumentType.Threshold;
                        dataTable.Rows.Add(row);
                    }
                }
                JsonResult.Set(context, dataTable);
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
