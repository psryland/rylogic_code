using System.Text.RegularExpressions;
using System.Windows;
using Rylogic.Gui.WPF;
using SolarHotWater.Common;

namespace SolarHotWater.UI
{
	public partial class LogUI :Window
	{
		public LogUI(Window owner)
		{
			InitializeComponent();
			PinState = new PinData(this, EPin.TopRight);
			Owner = owner;
			DataContext = this;
		}

		/// <summary>Window pinning support</summary>
		private PinData PinState { get; } = null!;

		/// <summary></summary>
		public string Filepath => Log.Filepath;

		/// <summary></summary>
		public Regex? Pattern => Log.LogEntryPattern;
	}
}
