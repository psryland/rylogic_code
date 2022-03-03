using System;
using System.Collections.Generic;
using System.Text;

namespace Rylogic.Script
{
	public interface IEmbeddedCode
	{
	}

	/// <summary>Creates embedded code handlers for the given language</summary>
	public delegate IEmbeddedCode EmbeddedCodeFactory(string lang);
}
