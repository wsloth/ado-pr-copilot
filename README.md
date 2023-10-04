# ado-pr-copilot

## TL;DR
- Analyzes your Azure DevOps Pull Request automatically as they are created/updated
- Provides AI-backed suggestions as comments on your PR!

## TODO
- Actually analyzing the contents using OpenAI
- Being able to have a "system" user provide these suggestions
- Being able to easily install this as an extension for Azure DevOps

## Installation
- Deploy this solution to your Function App
- Set up a new Service Hook in your Azure DevOps project
- Select "Web Hooks"
- Select the "Pull Request Updated" trigger type
- Point the url to your function app endpoint
- No additional configuration is necessary (testing will not work as the function tries to connect back to the fake URL in the test message)
- Save & enjoy!

## Development
- A function app to deploy to
- The `AZUREDEVOPS_PAT` environment variable with a Personal Access Token configured (read scopes)