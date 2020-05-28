# Rylogic Lib project organisation

This repository contains the Rylogic Ltd code.

## Project structure

	pr
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
	|-- include         - Public headers and interfaces. Third party projects should add an include path
	|   |-- pr            to '/pr/include'. All pr lib code uses includes relative to '/pr/include', e.g. #include "pr/common/..."
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

- Pulling to a clean directory
- Create a _/pr/script/UserVars.py_ file based on the _UserVars.template.py_ file in the same directory.
- Use _/pr/script/Build.py_ to build projects from the command line, or, open _/pr/build/rylogic.sln_ in visual studio.

This repo is actively developed, often refactored, and frequently broken. It is public so that the source for my released projects is publicly available.

## License

- [licence](miscellaneous/licenses/license.txt)
