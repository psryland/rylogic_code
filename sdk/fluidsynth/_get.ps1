# Get the absolute path of this file
$script_path = $MyInvocation.MyCommand.Path | Split-Path -Parent

# Push the current directory to the stack
Push-Location

# Change the current directory to the directory of this file
Set-Location -Path $script_path

$fluidsynth_zip = "fluidsynth-2.3.5-android24.zip"
$fluidsynth_dir = "fluidsynth"

# If the sdk directory doesn't exist, download
if (!(Test-Path -Path $fluidsynth_dir)) {
	if (!(Test-Path -Path $fluidsynth_zip)) {
		Invoke-WebRequest `
			-Uri "https://github.com/FluidSynth/fluidsynth/releases/download/v2.3.5/$fluidsynth_zip" `
			-OutFile $fluidsynth_zip
	}
	Expand-Archive -Path $fluidsynth_zip -DestinationPath $fluidsynth_dir
}

# Pop the current directory from the stack
Pop-Location

