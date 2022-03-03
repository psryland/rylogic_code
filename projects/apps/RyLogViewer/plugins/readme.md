## Plugin Support

RyLogViewer may be extended through dynamically loaded assemblies.

	```CSharp
	[Rylogic.Plugin.Plugin(typeof(RyLogViewer.ILineFormatter))]
	public class MyCustomLineFormatter : ILineFormatter
	{
		// ...
	}
	```
