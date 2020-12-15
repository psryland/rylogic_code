using Rylogic.Common;

namespace Rylogic.Gui.WPF.TextEditor
{
	public class OptionsData :SettingsBase<OptionsData>
	{
		public OptionsData()
		{
			EnableVirtualSpace = false;
			WrapText = false;
		}
		public OptionsData(string filepath, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
			:base(filepath, flags)
		{}

		/// <summary>True if virtual whitespace is used beyond the end of each line</summary>
		public bool EnableVirtualSpace
		{
			get => get<bool>(nameof(EnableVirtualSpace));
			set => set(nameof(EnableVirtualSpace), value);
		}

		/// <summary>True if text wraps at the right window edge</summary>
		public bool WrapText
		{
			get => get<bool>(nameof(WrapText));
			set => set(nameof(WrapText), value);
		}
	}
}
