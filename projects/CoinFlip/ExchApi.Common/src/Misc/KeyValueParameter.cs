using System.Diagnostics;

namespace ExchApi.Common
{
	/// <summary>Helper for passing Key/Value pair parameters</summary>
	[DebuggerDisplay("{Key}={Value}")]
	public struct KV
	{
		[DebuggerStepThrough]
		public KV(string key, object value)
		{
			Key = key;
			Value = value;
		}

		/// <summary>Parameter key value</summary>
		public string Key;

		/// <summary>Parameter value</summary>
		public object Value;
	}
}
