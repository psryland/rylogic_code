using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rylogic.Common;

namespace RyLogViewer
{
	public class JsonLineFormatter : ILineFormatter
	{
		public JsonLineFormatter()
		{
		}

		/// <summary>A name for this formatter, for displaying in the UI</summary>
		public const string Name = "JSON Lines";
		string ILineFormatter.Name => Name;

		/// <summary>Return a line instance from a buffer of log data</summary>
		public ILine CreateLine(byte[] line_buf, int start, int count, Range file_byte_range)
		{
			throw new NotImplementedException();
		}
	}
}
