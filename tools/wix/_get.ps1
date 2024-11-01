# Get the absolute path of this file
$script_path = $MyInvocation.MyCommand.Path | Split-Path -Parent

# Push the current directory to the stack
Push-Location

# Change the current directory to the directory of this file
Set-Location -Path $script_path

# if the 'dependencies' directory doesn't exist, download the zip
if (!(Test-Path -Path "wix")) {
	if (!(Test-Path -Path "wix311-binaries.zip")) {
		Invoke-WebRequest `
			-Uri "https://github.com/wixtoolset/wix3/releases/download/wix3112rtm/wix311-binaries.zip" `
			-OutFile "wix311-binaries.zip"
	}
	Expand-Archive -Path "wix311-binaries.zip" -DestinationPath "wix"
}

# Pop the current directory from the stack
Pop-Location

