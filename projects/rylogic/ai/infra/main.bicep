// Azure OpenAI resource + model deployment for the AI DLL.
// Usage:
//   az deployment group create `
//     --resource-group rg-rylogic-ai-dll `
//     --template-file main.bicep `
//     --parameters location=eastus

@description('Azure region. GPT-5.2 is currently only available in eastus and centralus.')
param location string = 'eastus'

@description('Name of the Azure OpenAI resource.')
param openai_name string = 'rylogic-ai-dll'

@description('Model to deploy.')
param model_name string = 'gpt-5.2-chat'

@description('Deployment name (used in API calls as the deployment parameter).')
param deployment_name string = 'gpt-5.2-chat'

@description('Model version. Use empty string for latest.')
param model_version string = ''

@description('Tokens-per-minute capacity (in thousands). 1 = 1K TPM.')
param capacity int = 1

// Azure OpenAI Cognitive Services account
resource openai 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: openai_name
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openai_name
    publicNetworkAccess: 'Enabled'
  }
}

// Model deployment
resource model 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openai
  name: deployment_name
  sku: {
    name: 'Standard'
    capacity: capacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: model_name
      version: model_version
    }
  }
}

// Outputs needed by the test program
output endpoint string = openai.properties.endpoint
output deployment string = model.name

@description('Run this to get your API key.')
output key_command string = 'az cognitiveservices account keys list --name ${openai_name} --resource-group rg-rylogic-ai-dll --query key1 -o tsv'
