using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PullRequestCopilot.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;

namespace PullRequestCopilot
{
    public static class PullRequestUpdatedFunctions
    {
        [FunctionName("PullRequestUpdated")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Incoming HTTP trigger for PullRequestUpdated");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var model = JsonConvert.DeserializeObject<PullRequestUpdatedModel>(requestBody);

            log.LogInformation("PullRequestUpdated for PR #{PrId} RequestBody: {requestBody}", model.resource.pullRequestId, requestBody);

            if (model.resource.status == "completed")
            {
                log.LogInformation("PullRequestUpdated PR #{PrId} was already completed", model.resource.pullRequestId);
                return new OkResult();
            }

            var adoPAT = System.Environment.GetEnvironmentVariable("AZUREDEVOPS_PAT", EnvironmentVariableTarget.Process);

            // URL like: https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repository}/pullRequests/{pullRequestId}
            var pullRequestUrl = model.resource.url;

            var threadId = 1337;
            var createThreadUrl = $"{pullRequestUrl}/threads/{threadId}/comments?api-version=7.0"; // https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-request-thread-comments/create?view=azure-devops-rest-7.0&tabs=HTTP

            // send post request to createThreadUrl
            try
            {
                var client = new HttpClient();
                var values = new Dictionary<string, string>
                  {
                      { "content", "Hello World from Azure Functions!" },
                  };

                var content = new FormUrlEncodedContent(values);
                content.Headers.TryAddWithoutValidation("Authorization", $"Basic {adoPAT}");

                var response = await client.PostAsync(createThreadUrl, content);

                var responseString = await response.Content.ReadAsStringAsync();
                log.LogInformation("PullRequestUpdated Azure DevOps Response: {responseBody}", responseString);

                return new OkResult();
            }
            catch (Exception e)
            {
                log.LogError(e.Message, e);
                return new InternalServerErrorResult();
            }
        }
    }
}
