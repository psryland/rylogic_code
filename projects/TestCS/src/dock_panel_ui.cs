using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
//using pr.gui.dockpanel;
using pr.util;

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
			}

			/// <summary>Implements docking functionality</summary>
			public DockControl DockControl { [DebuggerStepThrough] get; private set; }

			/// <summary>Human friendly name</summary>
			public override string ToString() { return Text; }
		}

		private pr.gui.DockContainer m_dock;

		public DockPanelUI()
		{
			InitializeComponent();
			StartPosition = FormStartPosition.Manual;
			Location = new Point(2150,150);

			//m_dock.ActiveContentChanged += (s,a) => Debug.WriteLine("ActiveContentChanged: {0} -> {1}".Fmt(a.ContentOld?.DockControl.PersistName, a.ContentNew?.DockControl.PersistName));
			//m_dock.ActivePaneChanged += (s,a) => Debug.WriteLine("ActivePaneChanged: {0} -> {1}".Fmt(a.PaneOld?.CaptionText, a.PaneNew?.CaptionText));
			//m_dock.ContentChanged += (s,a) =>
			//{
			//	Debug.WriteLine("ContentChanged: {0} {1}".Fmt(a.Change, a.Content.DockControl.PersistName));
			//	a.Content.DockControl.DockStateChanged += (ss,aa) =>
			//	{
			//		Debug.WriteLine("ContentDockStateChanged: {0} changed from {1} -> {2}".Fmt(ss.As<Dockable>().DockControl.PersistName, aa.StateOld, aa.StateNew));
			//	};
			//};
			//m_dock.PanesChanged += (s,a) =>
			//{
			//	Debug.WriteLine("PanesChanged: {0} {1}".Fmt(a.Change, a.Pane?.CaptionText));
			//	if (a.Change == PanesChangedEventArgs.EChg.Added)
			//	{
			//		a.Pane.ContentChanged += (ss,aa) => Debug.WriteLine("PaneContentChaged: {0} {1}".Fmt(aa.Change, aa.Content?.DockControl.PersistName));
			//		a.Pane.ActivatedChanged += (ss,aa) => Debug.WriteLine("PaneActivatedChanged: {0} is {1}".Fmt(ss.As<DockPane>().CaptionText, ss.As<DockPane>().Activated ? "Active" : "Inactive"));
			//	}
			//};

			var d0 = new Dockable("Dockable 0") { BorderStyle = BorderStyle.FixedSingle };
			var d1 = new Dockable("Dockable 1") { BorderStyle = BorderStyle.FixedSingle };
			var d2 = new Dockable("Dockable 2") { BorderStyle = BorderStyle.FixedSingle };
			var d3 = new Dockable("Dockable 3") { BorderStyle = BorderStyle.FixedSingle };
			var d4 = new Dockable("Dockable 4") { BorderStyle = BorderStyle.FixedSingle };
			var d5 = new Dockable("Dockable 5") { BorderStyle = BorderStyle.FixedSingle };
			var d6 = new Dockable("Dockable 6") { BorderStyle = BorderStyle.FixedSingle };
			var d7 = new Dockable("Dockable 7") { BorderStyle = BorderStyle.FixedSingle };
			var d8 = new Dockable("Dockable 8") { BorderStyle = BorderStyle.FixedSingle };
			var d9 = new Dockable("Dockable 9") { BorderStyle = BorderStyle.FixedSingle };

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

			d0.DockControl.DockPane.TabStripCtrl.StripLocation = EDockSite.Left;
			d2.DockControl.DockPane.TabStripCtrl.StripLocation = EDockSite.Top;
			d3.DockControl.DockPane.TabStripCtrl.StripLocation = EDockSite.Right;
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_dock = new pr.gui.DockContainer();
			this.SuspendLayout();
			// 
			// m_dock
			// 
			this.m_dock.ActiveContent = null;
			this.m_dock.ActivePane = null;
			this.m_dock.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_dock.Location = new System.Drawing.Point(3, 3);
			this.m_dock.Name = "m_dock";
			this.m_dock.Size = new System.Drawing.Size(748, 570);
			this.m_dock.TabIndex = 0;
			// 
			// DockPanelUI
			// 
			this.ClientSize = new System.Drawing.Size(754, 576);
			this.Controls.Add(this.m_dock);
			this.Name = "DockPanelUI";
			this.Padding = new System.Windows.Forms.Padding(3);
			this.Text = "Dock Container UI";
			this.ResumeLayout(false);

		}

		#endregion
	}

	/*
	public class DockPanelUI_Old :Form
	{
		public DockPanelUI_Old()
		{
			InitializeComponent();

			var dc0 = new DockContent("dc0"){Text = "Content0"};
			var dc1 = new DockContent("dc1"){Text = "Content1"};
			var dc2 = new DockContent("dc2"){Text = "Content2"};

			dc0.Show(m_dock, DockState.Document);
			dc1.Show(m_dock, DockState.Document);
			dc2.Show(m_dock, DockState.Document);
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		private pr.gui.dockpanel.DockPanel m_dock;

		#region Windows Form Designer generated code

		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			pr.gui.dockpanel.DockPanelSkin dockPanelSkin1 = new pr.gui.dockpanel.DockPanelSkin();
			pr.gui.dockpanel.AutoHideStripSkin autoHideStripSkin1 = new pr.gui.dockpanel.AutoHideStripSkin();
			pr.gui.dockpanel.DockPanelGradient dockPanelGradient1 = new pr.gui.dockpanel.DockPanelGradient();
			pr.gui.dockpanel.TabGradient tabGradient1 = new pr.gui.dockpanel.TabGradient();
			pr.gui.dockpanel.DockPaneStripSkin dockPaneStripSkin1 = new pr.gui.dockpanel.DockPaneStripSkin();
			pr.gui.dockpanel.DockPaneStripGradient dockPaneStripGradient1 = new pr.gui.dockpanel.DockPaneStripGradient();
			pr.gui.dockpanel.TabGradient tabGradient2 = new pr.gui.dockpanel.TabGradient();
			pr.gui.dockpanel.DockPanelGradient dockPanelGradient2 = new pr.gui.dockpanel.DockPanelGradient();
			pr.gui.dockpanel.TabGradient tabGradient3 = new pr.gui.dockpanel.TabGradient();
			pr.gui.dockpanel.DockPaneStripToolWindowGradient dockPaneStripToolWindowGradient1 = new pr.gui.dockpanel.DockPaneStripToolWindowGradient();
			pr.gui.dockpanel.TabGradient tabGradient4 = new pr.gui.dockpanel.TabGradient();
			pr.gui.dockpanel.TabGradient tabGradient5 = new pr.gui.dockpanel.TabGradient();
			pr.gui.dockpanel.DockPanelGradient dockPanelGradient3 = new pr.gui.dockpanel.DockPanelGradient();
			pr.gui.dockpanel.TabGradient tabGradient6 = new pr.gui.dockpanel.TabGradient();
			pr.gui.dockpanel.TabGradient tabGradient7 = new pr.gui.dockpanel.TabGradient();
			this.m_dock = new pr.gui.dockpanel.DockPanel();
			this.SuspendLayout();
			// 
			// m_dock
			// 
			this.m_dock.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_dock.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_dock.Mode = pr.gui.dockpanel.DockPanelMode.DockingWindow;
			this.m_dock.Location = new System.Drawing.Point(0, 0);
			this.m_dock.Name = "m_dock";
			this.m_dock.Size = new System.Drawing.Size(897, 828);
			dockPanelGradient1.EndColor = System.Drawing.SystemColors.ControlLight;
			dockPanelGradient1.StartColor = System.Drawing.SystemColors.ControlLight;
			autoHideStripSkin1.DockStripGradient = dockPanelGradient1;
			tabGradient1.EndColor = System.Drawing.SystemColors.Control;
			tabGradient1.StartColor = System.Drawing.SystemColors.Control;
			tabGradient1.TextColor = System.Drawing.SystemColors.ControlDarkDark;
			autoHideStripSkin1.TabGradient = tabGradient1;
			autoHideStripSkin1.TextFont = new System.Drawing.Font("Segoe UI", 9F);
			dockPanelSkin1.AutoHideStripSkin = autoHideStripSkin1;
			tabGradient2.EndColor = System.Drawing.SystemColors.ControlLightLight;
			tabGradient2.StartColor = System.Drawing.SystemColors.ControlLightLight;
			tabGradient2.TextColor = System.Drawing.SystemColors.ControlText;
			dockPaneStripGradient1.ActiveTabGradient = tabGradient2;
			dockPanelGradient2.EndColor = System.Drawing.SystemColors.Control;
			dockPanelGradient2.StartColor = System.Drawing.SystemColors.Control;
			dockPaneStripGradient1.DockStripGradient = dockPanelGradient2;
			tabGradient3.EndColor = System.Drawing.SystemColors.ControlLight;
			tabGradient3.StartColor = System.Drawing.SystemColors.ControlLight;
			tabGradient3.TextColor = System.Drawing.SystemColors.ControlText;
			dockPaneStripGradient1.InactiveTabGradient = tabGradient3;
			dockPaneStripSkin1.DocumentGradient = dockPaneStripGradient1;
			dockPaneStripSkin1.TextFont = new System.Drawing.Font("Segoe UI", 9F);
			tabGradient4.EndColor = System.Drawing.SystemColors.ActiveCaption;
			tabGradient4.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
			tabGradient4.StartColor = System.Drawing.SystemColors.GradientActiveCaption;
			tabGradient4.TextColor = System.Drawing.SystemColors.ActiveCaptionText;
			dockPaneStripToolWindowGradient1.ActiveCaptionGradient = tabGradient4;
			tabGradient5.EndColor = System.Drawing.SystemColors.Control;
			tabGradient5.StartColor = System.Drawing.SystemColors.Control;
			tabGradient5.TextColor = System.Drawing.SystemColors.ControlText;
			dockPaneStripToolWindowGradient1.ActiveTabGradient = tabGradient5;
			dockPanelGradient3.EndColor = System.Drawing.SystemColors.ControlLight;
			dockPanelGradient3.StartColor = System.Drawing.SystemColors.ControlLight;
			dockPaneStripToolWindowGradient1.DockStripGradient = dockPanelGradient3;
			tabGradient6.EndColor = System.Drawing.SystemColors.InactiveCaption;
			tabGradient6.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Vertical;
			tabGradient6.StartColor = System.Drawing.SystemColors.GradientInactiveCaption;
			tabGradient6.TextColor = System.Drawing.SystemColors.InactiveCaptionText;
			dockPaneStripToolWindowGradient1.InactiveCaptionGradient = tabGradient6;
			tabGradient7.EndColor = System.Drawing.Color.Transparent;
			tabGradient7.StartColor = System.Drawing.Color.Transparent;
			tabGradient7.TextColor = System.Drawing.SystemColors.ControlDarkDark;
			dockPaneStripToolWindowGradient1.InactiveTabGradient = tabGradient7;
			dockPaneStripSkin1.ToolWindowGradient = dockPaneStripToolWindowGradient1;
			dockPanelSkin1.DockPaneStripSkin = dockPaneStripSkin1;
			this.m_dock.Skin = dockPanelSkin1;
			this.m_dock.TabIndex = 0;
			// 
			// DockPanelUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(897, 828);
			this.Controls.Add(this.m_dock);
			this.Name = "DockPanelUI";
			this.Text = "dock_panel_ui";
			this.ResumeLayout(false);

		}

		#endregion
	}
	*/
}
