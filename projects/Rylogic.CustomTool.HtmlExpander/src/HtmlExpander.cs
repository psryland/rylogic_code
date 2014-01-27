using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using pr.script;

// http://mnaoumov.wordpress.com/2012/09/26/developing-custom-tool-aka-single-file-generators-for-visual-studio-2012/

namespace Rylogic.CustomTool
{
	[ComVisible(true)]
	[Guid("a72434a4-0a4d-4cc2-aba2-42e1fcd61db4")]
	public class HtmlExpander :BaseCodeGeneratorWithSite
	{
		/// <summary>Gets the default extension for this generator</summary>
		public override string GetDefaultExtension()
		{
			return ".html";
		}

		/// <summary>Generate the code-behind</summary>
		protected override byte[] GenerateCode(string src_filepath, string src)
		{
			try
			{
				var current_dir = Path.GetDirectoryName(src_filepath) ?? string.Empty;
				var result = Expand.Html(new StringSrc(src), current_dir);
				return Encoding.UTF8.GetBytes(result);
			}
			catch (Exception ex)
			{
				var msg = "HTML Expansion failed" + Environment.NewLine + "Exception:" + Environment.NewLine + ex.Message;
				return Encoding.UTF8.GetBytes(msg);
			}
		}
	}
}
