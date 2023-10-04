using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PullRequestCopilot.Models;

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
            log.LogInformation("PullRequestUpdated RequestBody: {requestBody}", requestBody);
            var model = JsonConvert.DeserializeObject<PullRequestUpdatedModel>(requestBody);

            if (model.resource.status == "completed")
            {
                log.LogInformation("PullRequestUpdated PR #{PrId} was already completed", model.resource.pullRequestId);
                return new OkResult();
            }

            return new OkResult();
        }
    }
}
