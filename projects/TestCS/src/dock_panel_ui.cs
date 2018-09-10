using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Util = Rylogic.Utility.Util;

namespace TestCS
{
	public class DockPanelUI :Form
	{
		private class Dockable :Panel, IDockable
		{
			public Dockable(string text)
			{
				BorderStyle = BorderStyle.None;
				Text = text;
				BackgroundImage = SystemIcons.Error.ToBitmap();
				BackgroundImageLayout = ImageLayout.Stretch;
				DockControl = new DockControl(this, text) {TabIcon = SystemIcons.Exclamation.ToBitmap()};

				Controls.Add2(new TextBox {Location = new Point(0, 0) });
				Controls.Add2(new TextBox {Location = new Point(0, 24) });
				Controls.Add2(new TextBox {Location = new Point(0, 48) });
			}

			/// <summary>Implements docking functionality</summary>
			public DockControl DockControl { [DebuggerStepThrough] get; private set; }

			/// <summary>Human friendly name</summary>
			public override string ToString() { return Text; }
		}
		private class TextDockable :TextBox ,IDockable
		{
			public TextDockable(string text)
			{
				Multiline = true;
				DockControl = new DockControl(this, text) {TabText = text, TabIcon = SystemIcons.Question.ToBitmap()};
			}

			/// <summary>Implements docking functionality</summary>
			public DockControl DockControl { [DebuggerStepThrough] get; private set; }
		}

		private MenuStrip m_menu;
		private DockContainer m_dock;

		public DockPanelUI()
		{
			InitializeComponent();
			StartPosition = FormStartPosition.Manual;
			Location = new Point(2150,150);

			m_dock.ActiveContentChanged += (s,a) =>
			{
				Debug.WriteLine($"ActiveContentChanged: {a.ContentOld?.DockControl.PersistName} -> {a.ContentNew?.DockControl.PersistName}");
			};
			m_dock.ActivePaneChanged += (s,a) =>
			{
				Debug.WriteLine($"ActivePaneChanged: {a.PaneOld?.CaptionText} -> {a.PaneNew?.CaptionText}");
			};
			m_dock.DockableMoved += (s,a) =>
			{
				Debug.WriteLine($"DockableMoved: {a.Action} {a.Dockable.DockControl.PersistName}");
			};

			var bs = BorderStyle.None;//FixedSingle;
			var d0 = new Dockable("Dockable 0") { BorderStyle = bs };
			var d1 = new Dockable("Dockable 1") { BorderStyle = bs };
			var d2 = new Dockable("Dockable 2") { BorderStyle = bs };
			var d3 = new Dockable("Dockable 3") { BorderStyle = bs };
			var d4 = new Dockable("Dockable 4") { BorderStyle = bs };
			var d5 = new Dockable("Dockable 5") { BorderStyle = bs };
			var d6 = new Dockable("Dockable 6") { BorderStyle = bs };
			var d7 = new Dockable("Dockable 7") { BorderStyle = bs };
			var d8 = new Dockable("Dockable 8") { BorderStyle = bs };
			var d9 = new Dockable("Dockable 9") { BorderStyle = bs };
			var d10 = new TextDockable("TextDockable 10") { BorderStyle = bs };

			m_dock.Add(d0); d0.DockControl.DockSite = EDockSite.Centre;
			m_dock.Add(d1);	d1.DockControl.DockSite = EDockSite.Left;
			m_dock.Add(d2);	d2.DockControl.DockSite = EDockSite.Top;
			m_dock.Add(d3);	d3.DockControl.DockSite = EDockSite.Right;
			m_dock.Add(d4);	d4.DockControl.DockSite = EDockSite.Right;
			m_dock.Add(d5);	d5.DockControl.DockSite = EDockSite.Bottom;
			m_dock.Add(d6);	d6.DockControl.DockSite = EDockSite.Bottom;
			m_dock.Add(d7);	d7.DockControl.DockSite = EDockSite.Centre;
			m_dock.Add(d8);	d8.DockControl.DockSite = EDockSite.Top;
			m_dock.Add(d9, EDockSite.Centre, EDockSite.Left);
			m_dock.Add(d10);

			d0.DockControl.DockPane.TabStripCtrl.StripLocation = EDockSite.Left;
			d2.DockControl.DockPane.TabStripCtrl.StripLocation = EDockSite.Top;
			d3.DockControl.DockPane.TabStripCtrl.StripLocation = EDockSite.Right;

			m_menu.Items.Add(m_dock.WindowsMenu());
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);
			var xml = m_dock.SaveLayout();
			xml.Save("P:\\dump\\dock_layout.xml");
		}

		#region Windows Form Designer generated code

		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_dock = new Rylogic.Gui.WinForms.DockContainer();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.SuspendLayout();
			// 
			// m_dock
			// 
			this.m_dock.ActiveContent = null;
			this.m_dock.ActivePane = null;
			this.m_dock.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_dock.Location = new System.Drawing.Point(3, 27);
			this.m_dock.Name = "m_dock";
			this.m_dock.Size = new System.Drawing.Size(748, 546);
			this.m_dock.TabIndex = 0;
			// 
			// m_menu
			// 
			this.m_menu.Location = new System.Drawing.Point(3, 3);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(748, 24);
			this.m_menu.TabIndex = 1;
			this.m_menu.Text = "menuStrip1";
			// 
			// DockPanelUI
			// 
			this.ClientSize = new System.Drawing.Size(754, 576);
			this.Controls.Add(this.m_dock);
			this.Controls.Add(this.m_menu);
			this.MainMenuStrip = this.m_menu;
			this.Name = "DockPanelUI";
			this.Padding = new System.Windows.Forms.Padding(3);
			this.Text = "Dock Container UI";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
