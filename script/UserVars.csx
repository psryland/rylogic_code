#! "net9.0"
#r "nuget: Rylogic.Core, 1.0.4"

public class UserVars
{
	// Version History:
	//  1 - initial version
	public int Version => 1;

	// Location of the root for the code library
	public string Root => Path_.CombinePath(Path_.Directory(Util.__FILE__()), "..");
}

