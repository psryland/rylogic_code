using pr.common;

namespace Csex
{
	public class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			SearchPaths = new string[0];
		}
		public Settings(string filepath)
			:base(filepath)
		{ }

		public string[] SearchPaths
		{
			get { return get<string[]>(nameof(SearchPaths)); }
			set { set(nameof(SearchPaths), value); }
		}
	}
}
