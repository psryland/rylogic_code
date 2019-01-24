using System.Windows.Controls;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	public partial class PinButton : Button
	{
		public PinButton()
		{
			InitializeComponent();
		}
		protected override void OnClick()
		{
			base.OnClick();
			IsPinned = !IsPinned;
		}

		/// <summary>The dock pane to be pinned/unpinned</summary>
		public DockPane DockPane => (Parent as TitleBar)?.DockPane;

		/// <summary>True when displaying the 'pinned' state</summary>
		public bool IsPinned
		{
			get { return DockPane != null && !DockPane.IsAutoHide; }
			set { if (DockPane != null) DockPane.IsAutoHide = !value; }
		}
	}
}
