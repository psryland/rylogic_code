# Get the absolute path of this file
$script_path = $MyInvocation.MyCommand.Path | Split-Path -Parent

# Push the current directory to the stack
Push-Location

# Change the current directory to the directory of this file
Set-Location -Path $script_path

# if the 'dependencies' directory doesn't exist, download the zip
if (!(Test-Path -Path "nuget.exe")) {
	Invoke-WebRequest `
		-Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" `
		-OutFile "nuget.exe"
}

# Pop the current directory from the stack
Pop-Location

