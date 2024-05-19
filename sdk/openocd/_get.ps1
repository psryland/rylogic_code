# Get the absolute path of this file
$script_path = $MyInvocation.MyCommand.Path | Split-Path -Parent

# Push the current directory to the stack
Push-Location

# Change the current directory to the directory of this file
Set-Location -Path $script_path

# if the 'openocd' directory doesn't exist, clone the nrf sdk repository
if (!(Test-Path -Path "openocd")) {
	if (!(Test-Path -Path "openocd.zip")) {
		Invoke-WebRequest `
			-Uri "https://github.com/xpack-dev-tools/openocd-xpack/releases/download/v0.12.0-3/xpack-openocd-0.12.0-3-win32-x64.zip" `
			-OutFile "openocd.zip" `
			-AllowInsecureRedirect
	}
	Expand-Archive -Path "openocd.zip" -DestinationPath "openocd"
}

# Pop the current directory from the stack
Pop-Location

