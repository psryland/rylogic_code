# Project Setup Script for 'AllKeys'

# Get the absolute path of this file
$script_path = $MyInvocation.MyCommand.Path | Split-Path -Parent

# Push the current directory to the stack and to the directory of this file
Push-Location
Set-Location -Path $script_path

$sdk_path = "../../../sdk"

# Run the '_get.ps1' script in the sdk/fluidsynth directory
# to make sure the FluidSynth binaries are downloaded
& "$sdk_path/fluidsynth/_get.ps1"

# Copy the folders in the 'lib' directory to the 'jniLibs' directory in the project
Copy-Item -Path "$sdk_path/fluidsynth/fluidsynth/lib/*" -Destination "./app/src/main/jniLibs" -Recurse -Force

# Copy the headers in the 'include' directory to the 'cpp' directory in the project
Copy-Item -Path "$sdk_path/fluidsynth/fluidsynth/include" -Destination "./app/src/main/cpp" -Recurse -Force

# Locate the Android NDK version >= ndk_version
$ndk_version = "27"
$pre_build_version = "18"
$ndk_path = Get-ChildItem -Path "C:\Users\$Env:USERNAME\AppData\Local\Android\Sdk\ndk" -Directory | Where-Object { $_.Name -ge $ndk_version } | Select-Object -First 1
$ndk_prebuilt_dir = "$ndk_path\toolchains\llvm\prebuilt\windows-x86_64\lib\clang\$pre_build_version\lib\linux"
if (-not (Test-Path -Path $ndk_prebuilt_dir)) {
    Write-Host "Error: Could not find the NDK path. Make sure the NDK is installed. Update versions in this script if necessary"
    exit
}

# Copy required .so files from the NDK to the 'jniLibs' directory in the project
$ndk_libs = @("libomp.so")#, "libunwind.so", "libc++_shared.so")
foreach ($lib in $ndk_libs) {
    Copy-Item -Path "$ndk_prebuilt_dir\aarch64\$lib" -Destination "./app/src/main/jniLibs/arm64-v8a/" -Force
    Copy-Item -Path "$ndk_prebuilt_dir\arm\$lib"     -Destination "./app/src/main/jniLibs/armeabi-v7a/" -Force
    Copy-Item -Path "$ndk_prebuilt_dir\i386\$lib"    -Destination "./app/src/main/jniLibs/x86/" -Force
    Copy-Item -Path "$ndk_prebuilt_dir\x86_64\$lib"  -Destination "./app/src/main/jniLibs/x86_64/" -Force
}

# Pop the current directory from the stack
Pop-Location

