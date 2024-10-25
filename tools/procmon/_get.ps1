# Get the absolute path of this file
$script_path = $MyInvocation.MyCommand.Path | Split-Path -Parent

# Push the current directory to the stack
Push-Location

# Change the current directory to the directory of this file
Set-Location -Path $script_path

# if the 'dependencies' directory doesn't exist, download the zip
if (!(Test-Path -Path "procmon")) {
	if (!(Test-Path -Path "ProcessMonitor.zip")) {
		Invoke-WebRequest `
			-Uri "https://download.sysinternals.com/files/ProcessMonitor.zip" `
			-OutFile "ProcessMonitor.zip"
	}
	Expand-Archive -Path "ProcessMonitor.zip" -DestinationPath "procmon"
}

# Pop the current directory from the stack
Pop-Location

