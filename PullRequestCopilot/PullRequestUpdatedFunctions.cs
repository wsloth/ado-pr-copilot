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
using System.Net.Http.Headers;
using Azure.AI.OpenAI;
using Azure;
using System.Linq;
using System.Threading;

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

            // var diff = await GetDiff(model);


            var openAiResponse = await GenerateOpenAIPullRequestReview(model);
            log.LogInformation("PullRequestUpdated PR #{PrId} GenerateOpenAIPullRequestReview response: {response}", model.resource.pullRequestId, openAiResponse);



            return new OkObjectResult(openAiResponse);

            // URL like: https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repository}/pullRequests/{pullRequestId}
            var pullRequestUrl = model.resource.url;

            var threadId = 1;
            var createThreadUrl = $"{pullRequestUrl}/threads/{threadId}/comments?api-version=7.0"; // https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-request-thread-comments/create?view=azure-devops-rest-7.0&tabs=HTTP

            // send post request to createThreadUrl
            try
            {
                using var client = GetDevOpsClient();

                var values = new Dictionary<string, string>
                  {
                      { "content", "Hello World from Azure Functions!" },
                      { "commentType", "1" },
                      { "parentCommentId", "1" },
                  };

                var content = new FormUrlEncodedContent(values);

                using var response = await client.PostAsync(
                            createThreadUrl, content);

                response.EnsureSuccessStatusCode();

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

        // Get the diff of the PR
        // private static async Task GetDiff(PullRequestUpdatedModel model)
        // {
        //     using var devOpsClient = GetDevOpsClient();

        //     var getGitCommitUrl = $"{baseUrl}/threads/comments?api-version=7.0";
        //     var response = await devOpsClient.GetAsync(getGitCommitUrl);

        //     return response;
        // }

        private static HttpClient GetDevOpsClient()
        {
            var adoPAT = Environment.GetEnvironmentVariable("AZUREDEVOPS_PAT", EnvironmentVariableTarget.Process);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(
                    System.Text.Encoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", "", adoPAT))));

            return client;
        }

        private static async Task<string> GenerateOpenAIPullRequestReview(PullRequestUpdatedModel model)
        {
            OpenAIClient client = new OpenAIClient("<openapi-api-key>");

            var systemPrompt = "You are a virtual agent that when presented the phrase 'tl;dr:' to the end of a text passage, you summarize it.";
            var userPrompt = "A neutron star is the collapsed core of a massive supergiant star, which had a total mass of between 10 and 25 solar masses, possibly more if the star was especially metal-rich.[1] Neutron stars are the smallest and densest stellar objects, excluding black holes and hypothetical white holes, quark stars, and strange stars.[2] Neutron stars have a radius on the order of 10 kilometres (6.2 mi) and a mass of about 1.4 solar masses.[3] They result from the supernova explosion of a massive star, combined with gravitational collapse, that compresses the core past white dwarf star density to that of atomic nuclei";


            var completionOptions = new ChatCompletionsOptions
            {
                MaxTokens = 60,
                Temperature = 0.7f,
                FrequencyPenalty = 0.0f,
                PresencePenalty = 1.0f,
                NucleusSamplingFactor = 1 // Top P
            };

            completionOptions.Messages.Add(new ChatMessage(ChatRole.System, systemPrompt));

            completionOptions.Messages.Add(new ChatMessage(ChatRole.User, userPrompt));

            ChatCompletions response = await client.GetChatCompletionsAsync("gpt-3.5-turbo", completionOptions);

            return response.Choices.First().Message.Content;
        }
    }
}
