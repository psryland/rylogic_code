# Get the absolute path of this file
$script_path = $MyInvocation.MyCommand.Path | Split-Path -Parent

# Push the current directory to the stack
Push-Location

# Change the current directory to the directory of this file
Set-Location -Path $script_path

# if the 'dependencies' directory doesn't exist, download the zip
if (!(Test-Path -Path "dependencies")) {
	if (!(Test-Path -Path "Dependencies_x64_Release.zip")) {
		Invoke-WebRequest `
			-Uri "https://github.com/lucasg/Dependencies/releases/download/v1.11.1/Dependencies_x64_Release.zip" `
			-OutFile "Dependencies_x64_Release.zip"
	}
	Expand-Archive -Path "Dependencies_x64_Release.zip" -DestinationPath "dependencies"
}

# Pop the current directory from the stack
Pop-Location

