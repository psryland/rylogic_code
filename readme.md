# Rylogic Lib project organisation

```txt
pr
|-- art
|   |-- icon        - Art assets
|   |-- etc...
|
|-- build           - Project files and property sheets
|   |-- props       - Property sheets
|   |-- rylogic.sln - "Everything" solution file
|   |-- cex.vcxproj - Project files
|   |-- etc...
|
|-- include         - Public headers and interfaces. Third party projects should add an include path
|   |-- pr            to '/pr/include'. All pr lib code uses includes such as #include "pr/common/..."
|       |-- common
|       |-- etc...
|
|-- projects        - Projects that compile to libs, dlls, or exe's.
|   |-- view3d
|   |-- etc...
|
|-- script          - Python scripts used in the build process. Only the variables in 'UserVars.py'
|   |-- UserVars.py    should need to be set to have the library build
|   |-- Rylogic.py
|   |-- etc...
|
|-- sdk             - Third party libraries and source
|-- tools           - Handy binaries
|-- miscellaneous   - Howtos, binary templates, licenses, and other random stuff
|
|-- obj             - Generated directory containing all object files for native projects
|-- lib             - Generated directory containing compiled libraries
|-- bin             - Generated directory containing compiled executables
```

## License

- [licence](miscellaneous/licenses/license.txt)

