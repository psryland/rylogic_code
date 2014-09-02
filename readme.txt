PR Lib project organisation

/root
	/build           - Project files and property sheets
		/packages    - Nuget created directory for nuget packages
		/.nugget     - Nuget created directory for nuget packages
		rylogic.sln  - "Everything" solution file
		cex.vcxproj
		etc
	/include         - Public headers and interfaces. Third party projects should add an include path
		/pr            to '/root/include'. All pr lib code uses includes such as "pr/common/..."
			/common
			etc...
	/projects        - PR lib projects that compile to libs, dlls, or exes
		/renderer11    these projects should build where ever /root happens to be in the file system.
		/view3d        
		etc...
	/script          - Python scripts used in the build process. Only the variables in 'UserVars.py'
		UserVars.py    should need to be set to have the library build
		Rylogic.py
		etc
	/sdk             - Third party libraries and source
	/tools           - Binaries used to support scripts

	*Generated directories*
	/obj             - All object files for native projects
	/lib             - Compiled libraries
	/bin             - Compiled executables

