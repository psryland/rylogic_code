using System.Windows.Controls;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>A button for closing/hiding a dock pane</summary>
	public partial class CloseButton : Button
	{
		public CloseButton()
		{
			InitializeComponent();
		}

		/// <summary>The dock pane to be pinned/unpinned</summary>
		public DockPane DockPane => (Parent as TitleBar)?.DockPane;
	}
}
