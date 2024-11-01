# Packages

Use nuget to install packages into the packages folder, then create your own 'targets' file.
Add sections like this: `<Import Project="..\..\..\build\targets\WinPixEventRuntime.targets" />` to your project files.

## Notes

- It seems like a project can only have one custom build step, so you can't create one in the targets file.

## Get Packages

From this directory:

```ps
$nuget = "..\..\tools\nuget\nuget.exe"
&$nuget install WinPixEventRuntime -OutputDirectory ../packages`
&$nuget install Microsoft.Direct3D.DXC -OutputDirectory ../packages`
```
