# AllKeys

## Project Building

- Open the folder in Android Studio
- Run the `_project_setup.ps1` script if you don't have the `fluidsynth` SDK available.
  You also need to run the script to copy files into the 'jniLibs' folder in the project.


## Deploy to Play Store

1. Select Build -> 'Generate Signed App Bundle or APK'
1. Choose 'Android App Bundle'
1. Use `.\AllKeys\allkeys-keystore.jks` for the Key store path (Get latest code signing cert)
1. Enter password (get from password manager, not the normal one)
1. Select the `key0` Key alias
1. Enter that password (same as keystore)
1. Select 'release' and a directory to output to

In the play.google.com/console

1. Select 'Open testing' and create a new release
1. Drop the `allkeys.aab` into the App bundles, then next after successful upload
1. Keep following the steps until the "Send changes to Google" step.

## Deply to website

1. Select Build -> 'Generate Signed App Bundle or APK'
1. Choose 'APK'
1. Same as above, then copy the APK to the filestore for the web site
