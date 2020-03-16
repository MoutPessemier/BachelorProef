using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace SendFiles
{
    class SendFiles : AsyncCodeActivity<string>
    {
        private readonly HttpClient client = new HttpClient();

        [Category("Input")]
        [RequiredArgument]
        public InArgument<List<string>> Files { get; set; }

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
        public OutArgument JsonResult { get; set; }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
            string url = "https://dev.adp.faktion.com/gql/api/organisations/" + OrganisationId + "/projects/" + ProjectId + "/process";
            HttpContent content;
            foreach (var filePath in Files.Get(context))
            {
                try
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
                    {
                        content = new StreamContent(fs);
                        var uploadId = client.PostAsync(url, content);
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }
            var polling = true;
            do
            {
                var response = client.GetAsync(url + "/${uploadId}").Result.Content;
                if(response != null)
                {
                    polling = false;
                }
            } while (polling);
            return client.GetAsync(url + "/${uploadId}");
        }

        protected override string EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = result as Task<string>;
            try
            {
                return task.Result;
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
            return null;
        }
    }
}
