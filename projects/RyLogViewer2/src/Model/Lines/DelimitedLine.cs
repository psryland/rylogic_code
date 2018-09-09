using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rylogic.Common;

namespace RyLogViewer
{
	public class DelimitedLine : ILine
	{
		public Range FileByteRange
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public string Value(int column)
		{
			throw new NotImplementedException();
		}
	}
}
