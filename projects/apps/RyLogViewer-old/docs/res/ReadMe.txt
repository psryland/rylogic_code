Files in this directory are added to the assembly as embedded resources.
The PyScript build action compiles the '.htm' files into '.html' files in the '/RyLogViewer/res/' directory

The build action for files in this folder should be:
	Build Action: PyScript
	Copy To Output: never
	Custom Tool: ..\..\script\HtmlExpand.py
	Custom Tool Namespace: ..\..\..\docs\ or ..\..\..\Resources\
	The PyScript build action is made available in the project by editing the csproj and adding
	"<Import Project="$(ProjectDir)..\..\build\props\python_script.targets" />".
	The PyScript build action passes the file to py.exe as follows:
		py.exe <Custom Tool> <file> <Custom Tool Namespace>
	So 'Custom Tool' should be the script to call
	'Custom Tool Namespace' should contain additional arguments


Read 'project_howto.txt'