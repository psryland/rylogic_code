# rylogic.co.nz

## Publish Steps

1. Bump the `Versions.SiteVersion` constant in `Source/Constants.cs` before publishing.
1. Right click on the project 'rylogic.co.nz' and select publish
1. Select the 'FolderProfile.pubxml' config and publish
1. Run the 'SetPermissions.ps1' script on the web server after publishing completes. This sets permissions on the 'rylogic.co.nz' folder for IUSR and IIS_IUSRS to have read access.

## Gotcha's

- Check that `<TargetFramework>net9.0-windows</TargetFramework>` is set to the correct target in `.\www\Properties\PublishProfiles\FolderProfile.pubxml`
- The web server needs the ASP.Net Core runtimes installed (https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/9.0.7/dotnet-hosting-9.0.7-win.exe)
