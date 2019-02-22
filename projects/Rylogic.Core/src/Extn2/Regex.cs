namespace Rylogic.Extn
{
	public static class Regex_
	{
		/// <summary>A pattern for matching full file paths, optionally in quotes</summary>
		public const string FullPathPattern =
			@"(?n)"+                                 // Only capture named groups
			@"(?<![""'])"+                           // Is not the next character after a quote
			@"(?<path>"+                             // Capture group for the whole path
			@"(?('|"")"+                             // If the path starts with a quote
			@"("+                                    //
				@"(?<q>['""])"+                      // Capture the quote type
				@"(?<drive>[A-Za-z]:)"+              // Match a drive letter followed by :
				@"(?<dir>[\\/]([^""<>|]+[\\/])*)"+   // Match 0 or more directory paths, starting and ending with \ or /
				@"(?<file>[^""<>|:*?\\/]+?)"+        // Match a filename (with extension) up to the quote character
				@"\k<q>"+                            // Match the closing quote
			@")|("+                                  //
				@"(?<drive>[A-Za-z]:)"+              // Match a drive letter followed by :
				@"(?<dir>[\\/]([^\s""<>|]+[\\/])*)"+ // Match 0 or more directory paths (excluding whitespace), starting and ending with \ or /
				@"(?<file>[^\s""<>|:*?\\/]+?)"+      // Match a filename (with extension) (excluding whitespace)
				@"(?=[\s""<>|:*?\\/]|$)"+            // Match the first whitespace, invalid filename character, or string end (but don't include in match result)
			@")))";                                  //
	}
}
