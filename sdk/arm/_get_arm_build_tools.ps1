# Get the absolute path of this file
$script_path = $MyInvocation.MyCommand.Path | Split-Path -Parent

# Push the current directory to the stack
Push-Location

# Change the current directory to the directory of this file
Set-Location -Path $script_path

# Download the latest ARM build tools
if (!(Test-Path -Path "build-tools")) {

    if (!(Test-Path -Path "arm_build_tools.zip")) {
        Invoke-WebRequest `
            -Uri "https://developer.arm.com/-/media/Files/downloads/gnu/13.2.rel1/binrel/arm-gnu-toolchain-13.2.rel1-mingw-w64-i686-arm-none-eabi.zip?rev=93fda279901c4c0299e03e5c4899b51f&hash=99EF910A1409E119125AF8FED325CF79" `
            -OutFile "arm_build_tools.zip"
    }

    Expand-Archive -Path "arm_build_tools.zip" -DestinationPath "build-tools"
}

# Pop the current directory from the stack
Pop-Location
