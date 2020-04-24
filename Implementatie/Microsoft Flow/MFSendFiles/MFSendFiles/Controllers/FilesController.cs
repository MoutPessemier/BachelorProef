/*
 Based upon the official Microsoft docs about:
 https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase?view=aspnetcore-3.1
 https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/how-to-make-multiple-web-requests-in-parallel-by-using-async-and-await
 https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.postasync?view=netframework-4.8
 https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpresponsemessage?view=netframework-4.8
 https://docs.microsoft.com/en-us/dotnet/api/system.net.http?view=netframework-4.8
 https://docs.microsoft.com/en-us/dotnet/api/system.net.http.streamcontent?view=netframework-4.8
 https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream?view=netframework-4.8
 https://docs.microsoft.com/en-us/dotnet/api/system.io.bufferedstream?view=netframework-4.8
 */

using System;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.ExceptionServices;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using MFSendFiles.Domain;

namespace MFSendFiles.Controllers
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Sends Files to MetaMaze and waits for a response. Parses that response into a data table.
        /// </summary>
        /// <param name="files">A collection of paths to files</param>
        /// <param name="organisationId">The id of the organisation</param>
        /// <param name="projectId">The id of the project</param>
        /// <param name="bearerToken">Your account's bearer token</param>
        /// <returns></returns>
        [HttpPost]
        public void SendFiles(SendFilesInput input)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", input.BearerToken);
            string url = "https://adp.faktion.com/gql/api/organisations/" + input.OrganisationId + "/projects/" + input.ProjectId + "/process";
            string response = null;
            bool succesfullRequest = false;
            MultipartFormDataContent formdata = new MultipartFormDataContent();
            try
            {
                foreach (var filePath in input.Files)
                {
                    // create filestream content
                    FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
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
                        break;
                }
                // check status every 7 seconds
                Thread.Sleep(7000);
            } while (polling && counter <= 75); // 7 sec * 75 = 525 seconds = 8:45 min
            if (counter == 75)
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

                //return dataTable;
            }
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
}