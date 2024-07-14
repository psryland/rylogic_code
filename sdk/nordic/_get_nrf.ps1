# Get the absolute path of this file
$script_path = $MyInvocation.MyCommand.Path | Split-Path -Parent

# Push the current directory to the stack
Push-Location

# Change the current directory to the directory of this file
Set-Location -Path $script_path

# if the 'nrf' directory doesn't exist, clone the nrf sdk repository
if (!(Test-Path -Path "nrf")) {
	if (!(Test-Path -Path "nrf5_sdk_17.1.0_ddde560.zip")) {
		Invoke-WebRequest `
			-Uri "https://nsscprodmedia.blob.core.windows.net/prod/software-and-other-downloads/sdks/nrf5/binaries/nrf5_sdk_17.1.0_ddde560.zip" `
			-OutFile "nrf5_sdk_17.1.0_ddde560.zip"
	}
	Expand-Archive -Path "nrf5_sdk_17.1.0_ddde560.zip" -DestinationPath "nrf"
}

if (!(Test-Path -Path "s140nrf52720")) {
	if (!(Test-Path -Path "s140nrf52720.zip")) {
		Invoke-WebRequest `
			-Uri "https://nsscprodmedia.blob.core.windows.net/prod/software-and-other-downloads/softdevices/s140/s140nrf52720.zip" `
			-OutFile "s140nrf52720.zip"
	}
	Expand-Archive -Path "s140nrf52720.zip" -DestinationPath "s140nrf52720"
}

# Pop the current directory from the stack
Pop-Location

