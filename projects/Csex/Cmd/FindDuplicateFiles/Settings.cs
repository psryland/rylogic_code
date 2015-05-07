using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pr.common;

namespace Csex
{
	public class Settings :SettingsBase<Settings>
	{
		public string[] SearchPaths { get { return get(x => x.SearchPaths); } set { set(x => x.SearchPaths, value); } }
		public Settings()
		{
			SearchPaths = new string[0];
		}
		public Settings(string filepath) :base(filepath) {}
	}
}
