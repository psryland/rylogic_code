# Rylogic Lib project organisation

This repository contains the Rylogic Ltd code.

## Project structure

	rylogic_code
	|-- art
	|   |-- icons       - Art assets
	|   |-- pngs        - Art assets
	|   |-- etc...
	|
	|-- build           - Project files and property sheets
	|   |-- props       - Property sheets
	|   |-- rylogic.sln - "Everything" solution file
	|   |-- cex.vcxproj - Project files
	|   |-- etc...
	|
	|-- include         - Public headers and interfaces. Users of this library should add this directory as an include path.
	|   |-- pr            All rylogic library code uses includes relative to '/rylogic_code/include', e.g. #include "pr/common/..."
	|       |-- common
	|       |-- etc...
	|
	|-- projects        - C/C++/C# Projects
	|   |-- view3d
	|   |-- Rylogic.Core
	|   |-- Rylogic.TextAligner
	|   |-- etc...
	|
	|-- typescript      - Typescript Projects
	|   |-- Rylogic.TextAligner
	|   |-- etc...
	|
	|-- script          - Python scripts used in the build process. Only the variables in 'UserVars.py'
	|   |-- UserVars.py   should need to be set to have the library build.
	|   |-- Rylogic.py
	|   |-- etc...
	|
	|-- sdk             - Third party libraries and source
	|-- tools           - Handy binaries
	|-- miscellaneous   - HowTos, binary templates, licenses, and other random stuff
	|
	|-- obj             - Generated directory containing all object files for native projects
	|-- lib             - Generated directory containing compiled libraries
	|-- bin             - Generated directory containing compiled executables

## Building

This project is only used on windows. Compiling requires MSBuild and Python 3.
Follow these steps to build:

- Pull to a clean directory
- Create a _/script/UserVars.py_ file based on the _UserVars.template.py_ file in the same directory.
- Use _/script/Build.py_ to build projects from the command line, or, open _/build/rylogic.sln_ in Visual Studio 2022.

This repo is actively developed, often refactored, and frequently broken. It is public so that the source for my released projects is publicly available.

## License

- [licence](miscellaneous/licenses/license.txt)
