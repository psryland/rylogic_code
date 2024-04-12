# Packages

Use nuget to install packages into this folder, but create your own 'targets' file.
Add sections like this: `<Import Project="..\..\..\build\packages\WinPixEventRuntime.targets" />` to your project files.

## Notes

- It seems like a project can only have one custom build step, so you can't create one in the targets file.

