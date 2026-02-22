# Deploy Azure OpenAI resource + model for the AI DLL test program.
#
# Prerequisites:
#   - Azure CLI installed (winget install Microsoft.AzureCLI)
#   - Logged in: az login
#
# This script:
#   1. Ensures the resource group exists (creates in eastus if needed)
#   2. Deploys the Bicep template (OpenAI resource + model)
#   3. Retrieves the API key
#   4. Sets environment variables for the test program
#
# GPT-5.2 is currently only available in eastus and centralus.
# If you get quota errors, try changing $Location to 'centralus'.

param(
	[string]$ResourceGroup = "rg-rylogic-ai-dll",
	[string]$Location = "eastus",
	[string]$OpenAIName = "rylogic-ai-dll",
	[string]$DeploymentName = "gpt-4o-mini",
	[string]$ModelName = "gpt-4o-mini"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "`n=== Azure OpenAI Deployment ===" -ForegroundColor Cyan

# Check Azure CLI is available
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
	Write-Error "Azure CLI not found. Install with: winget install Microsoft.AzureCLI"
	exit 1
}

# Ensure logged in
$account = az account show 2>&1
if ($LASTEXITCODE -ne 0) {
	Write-Host "Not logged in. Running 'az login'..." -ForegroundColor Yellow
	az login
}

# Ensure resource group exists in the correct region
Write-Host "`nEnsuring resource group '$ResourceGroup' exists in '$Location'..." -ForegroundColor Yellow
az group create --name $ResourceGroup --location $Location --output none

# Deploy the Bicep template
Write-Host "`nDeploying Azure OpenAI resource + model..." -ForegroundColor Yellow
$result = az deployment group create `
	--resource-group $ResourceGroup `
	--template-file "$ScriptDir\main.bicep" `
	--parameters location=$Location openai_name=$OpenAIName deployment_name=$DeploymentName model_name=$ModelName `
	--query "properties.outputs" `
	--output json | ConvertFrom-Json

if ($LASTEXITCODE -ne 0) {
	Write-Error "Deployment failed. If you see a quota error, try: -Location centralus"
	exit 1
}

$endpoint = $result.endpoint.value
$deployment = $result.deployment.value

Write-Host "`nEndpoint:   $endpoint" -ForegroundColor Green
Write-Host "Deployment: $deployment" -ForegroundColor Green

# Retrieve the API key
Write-Host "`nRetrieving API key..." -ForegroundColor Yellow
$apiKey = az cognitiveservices account keys list `
	--name $OpenAIName `
	--resource-group $ResourceGroup `
	--query "key1" --output tsv

if ($LASTEXITCODE -ne 0) {
	Write-Error "Failed to retrieve API key."
	exit 1
}

Write-Host "API Key:    $($apiKey.Substring(0, 8))..." -ForegroundColor Green

# Set environment variables (user-level, persistent)
Write-Host "`nSetting environment variables (user-level)..." -ForegroundColor Yellow
[System.Environment]::SetEnvironmentVariable("AZURE_OPENAI_API_KEY", $apiKey, "User")
[System.Environment]::SetEnvironmentVariable("AZURE_OPENAI_ENDPOINT", $endpoint, "User")
[System.Environment]::SetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT", $deployment, "User")

# Also set for current session
$env:AZURE_OPENAI_API_KEY = $apiKey
$env:AZURE_OPENAI_ENDPOINT = $endpoint
$env:AZURE_OPENAI_DEPLOYMENT = $deployment

Write-Host "`n=== Done ===" -ForegroundColor Cyan
Write-Host "Environment variables set. You can now run:" -ForegroundColor White
Write-Host "  .\projects\tests\ai-test\obj\x64\Release\ai-test.exe" -ForegroundColor White
Write-Host "`nTo tear down later:" -ForegroundColor Gray
Write-Host "  az group delete --name $ResourceGroup --yes" -ForegroundColor Gray
