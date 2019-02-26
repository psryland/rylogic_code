using System;

namespace Rylogic.Gui.WPF
{
	using ChartDetail;

	public partial class ChartControl
	{
		/// <summary>Create and display a context menu</summary>
		public void ShowContextMenu(System.Windows.Point location, HitTestResult hit_result)
		{
			//var cmenu = new ContextMenuStrip { Renderer = new ContextMenuRenderer() };
			//
			//using (this.ChangeCursor(Cursors.WaitCursor))
			//using (cmenu.SuspendLayout(true))
			//{
			//	#region Objects
			//	{
			//		var objects_menu = cmenu.Items.Add2(new ToolStripMenuItem("Objects") { Name = CMenu.Objects });
			//		{
			//			var opt = objects_menu.DropDownItems.Add2(new ToolStripMenuItem("Origin") { Name = CMenu.ObjectsMenu.Origin });
			//			opt.Checked = Scene.Window.OriginPointVisible;
			//			opt.Click += (s, a) =>
			//			{
			//				Scene.Window.OriginPointVisible = !Scene.Window.OriginPointVisible;
			//				Invalidate();
			//			};
			//		}
			//		{
			//			var opt = objects_menu.DropDownItems.Add2(new ToolStripMenuItem("Focus") { Name = CMenu.ObjectsMenu.Focus });
			//			opt.Checked = Scene.Window.FocusPointVisible;
			//			opt.Click += (s, a) =>
			//			{
			//				Scene.Window.FocusPointVisible = !Scene.Window.FocusPointVisible;
			//				Invalidate();
			//			};
			//		}
			//		{
			//			var opt = objects_menu.DropDownItems.Add2(new ToolStripMenuItem("Grid Lines") { Name = CMenu.ObjectsMenu.GridLines });
			//			opt.Checked = Options.ShowGridLines;
			//			opt.Click += (s, a) =>
			//			{
			//				Options.ShowGridLines = !Options.ShowGridLines;
			//				Invalidate();
			//			};
			//		}
			//		{
			//			var opt = objects_menu.DropDownItems.Add2(new ToolStripMenuItem("Axes") { Name = CMenu.ObjectsMenu.GridLines });
			//			opt.Checked = Options.ShowAxes;
			//			opt.Click += (s, a) =>
			//			{
			//				Options.ShowAxes = !Options.ShowAxes;
			//				Invalidate();
			//			};
			//		}
			//	}
			//	#endregion
			//	#region Tools
			//	{
			//		var tools_menu = cmenu.Items.Add2(new ToolStripMenuItem("Tools") { Name = CMenu.Tools });
			//		#region Show Value
			//		{
			//			var opt = tools_menu.DropDownItems.Add2(new ToolStripMenuItem("Show Value") { Name = CMenu.ToolsMenu.ShowValue });
			//			opt.Checked = m_show_value;
			//			opt.Click += (s, a) =>
			//			{
			//				if (m_show_value)
			//				{
			//					MouseMove -= OnMouseMoveTooltip;
			//					MouseWheel -= OnMouseWheelTooltip;
			//				}
			//				m_show_value = !m_show_value;
			//				m_tt_show_value.Size = m_tt_show_value.GetPreferredSize(Size.Empty);
			//				m_tt_show_value.Visible = m_show_value;
			//				if (m_show_value)
			//				{
			//					MouseMove += OnMouseMoveTooltip;
			//					MouseWheel += OnMouseWheelTooltip;
			//				}
			//
			//				/// <summary>Handle mouse move events while the tooltip is visible</summary>
			//				void OnMouseMoveTooltip(object sender, MouseEventArgs e)
			//				{
			//					SetValueToolTip(e.Location);
			//				}
			//				void OnMouseWheelTooltip(object sender, MouseEventArgs e)
			//				{
			//					SetValueToolTip(e.Location);
			//				}
			//			};
			//		}
			//		#endregion
			//		#region Show Cross Hair
			//		{
			//			var opt = tools_menu.DropDownItems.Add2(new ToolStripMenuItem("Show Cross Hair") { Name = CMenu.ToolsMenu.ShowXHair });
			//			opt.Checked = CrossHairVisible;
			//			opt.Click += (s, a) =>
			//			{
			//				CrossHairVisible = !CrossHairVisible;
			//				Invalidate();
			//			};
			//		}
			//		#endregion
			//	}
			//	#endregion
			//	#region Zoom Menu
			//	{
			//		var zoom_menu = cmenu.Items.Add2(new ToolStripMenuItem("Zoom") { Name = CMenu.Zoom });
			//		{
			//			var opt = zoom_menu.DropDownItems.Add2(new ToolStripMenuItem("Default") { Name = CMenu.ZoomMenu.Default });
			//			opt.Click += (s, a) =>
			//			{
			//				AutoRange();
			//			};
			//		}
			//		{
			//			var opt = zoom_menu.DropDownItems.Add2(new ToolStripMenuItem("Aspect 1:1") { Name = CMenu.ZoomMenu.Aspect1to1 });
			//			opt.Click += (s, a) =>
			//			{
			//				Aspect = 1.0f;
			//			};
			//		}
			//		{
			//			var opt = zoom_menu.DropDownItems.Add2(new ToolStripMenuItem("Lock Aspect") { Name = CMenu.ZoomMenu.LockAspect });
			//			opt.Checked = Options.LockAspect != null;
			//			opt.Click += (s, a) =>
			//			{
			//				LockAspect = !LockAspect;
			//			};
			//		}
			//		{
			//			var opt = zoom_menu.DropDownItems.Add2(new ToolStripMenuItem("Perpendicular Z Translation") { Name = CMenu.ZoomMenu.PerpZTrans });
			//			opt.Checked = Options.PerpendicularZTranslation;
			//			opt.Click += (s, a) =>
			//			{
			//				Options.PerpendicularZTranslation = !Options.PerpendicularZTranslation;
			//			};
			//		}
			//	}
			//	#endregion
			//	#region Rendering
			//	{
			//		var rendering_menu = cmenu.Items.Add2(new ToolStripMenuItem("Rendering") { Name = CMenu.Rendering });
			//		{
			//			var opt = rendering_menu.DropDownItems.Add2(new ToolStripComboBox("Navigation Mode") { Name = CMenu.RenderingMenu.NavigationMode });
			//			opt.DropDownStyle = ComboBoxStyle.DropDownList;
			//			opt.ComboBox.DataSource = Enum<ENavMode>.ValuesArray;
			//			opt.SelectedItem = Options.NavigationMode;
			//			opt.ComboBox.Format += (s, a) =>
			//			{
			//				a.Value = ((ENavMode)a.ListItem).Desc();
			//			};
			//			opt.SelectedIndexChanged += (s, a) =>
			//			{
			//				Options.NavigationMode = (ENavMode)opt.SelectedItem;
			//				Invalidate();
			//			};
			//		}
			//		{
			//			var opt = rendering_menu.DropDownItems.Add2(new ToolStripMenuItem("Orthographic") { Name = CMenu.RenderingMenu.Orthographic });
			//			opt.Checked = Options.Orthographic;
			//			opt.Click += (s, a) =>
			//			{
			//				Options.Orthographic = !Options.Orthographic;
			//				Invalidate();
			//			};
			//		}
			//		{
			//			var fillmode = Enum<View3d.EFillMode>.Cycle(Scene.Window.FillMode);
			//			var opt = rendering_menu.DropDownItems.Add2(new ToolStripMenuItem(fillmode.ToString()) { Name = CMenu.RenderingMenu.Wireframe });
			//			opt.Click += (s, a) =>
			//			{
			//				Options.FillMode = fillmode;
			//				Invalidate();
			//			};
			//		}
			//		{
			//			var opt = rendering_menu.DropDownItems.Add2(new ToolStripMenuItem("Anti-Aliasing") { Name = CMenu.RenderingMenu.AntiAliasing });
			//			opt.Checked = Options.AntiAliasing;
			//			opt.Click += (s, a) =>
			//			{
			//				Options.AntiAliasing = !Options.AntiAliasing;
			//				Scene.Window.MultiSampling = Options.AntiAliasing ? 4 : 1;
			//				Invalidate();
			//			};
			//		}
			//	}
			//	#endregion
			//	cmenu.Items.AddSeparator();
			//	#region Properties
			//	{
			//		var opt = cmenu.Items.Add2(new ToolStripMenuItem("Properties") { Name = CMenu.Properties });
			//		opt.Click += (s, a) =>
			//		{
			//			new RdrOptionsUI(this, Options).Show(this);
			//		};
			//	}
			//	#endregion
			//
			//	// Allow users to add menu options
			//	OnAddUserMenuOptions(new AddUserMenuOptionsEventArgs(AddUserMenuOptionsEventArgs.EType.Chart, cmenu, hit_result));
			//}
			//cmenu.Items.TidySeparators();
			//cmenu.Closed += (s, a) => Refresh();
			//cmenu.Show(this, location);
		}

		/// <summary>Event allowing callers to add options to the context menu</summary>
		public event EventHandler<AddUserMenuOptionsEventArgs> AddUserMenuOptions;
		protected virtual void OnAddUserMenuOptions(AddUserMenuOptionsEventArgs args)
		{
			AddUserMenuOptions?.Invoke(this, args);
		}

		/// <summary>Names for context menu items to allow users to identify them</summary>
		public static class CMenu
		{
			public const string Tools = "tools";
			public static class ToolsMenu
			{
				public const string ShowValue = "show_value";
				public const string ShowXHair = "show_cross_hair";
			}
			public const string Zoom = "zoom";
			public static class ZoomMenu
			{
				public const string Default = "default";
				public const string Aspect1to1 = "aspect1:1";
				public const string LockAspect = "lock_aspect";
				public const string PerpZTrans = "perp_z_trans";
			}
			public const string Objects = "objects";
			public static class ObjectsMenu
			{
				public const string Origin = "origin";
				public const string Focus = "focus";
				public const string GridLines = "grid_lines";
				public const string Axes = "axes";
			}
			public const string Rendering = "rendering";
			public static class RenderingMenu
			{
				public const string NavigationMode = "nav_mode";
				public const string Orthographic = "orthographic";
				public const string Wireframe = "wireframe";
				public const string AntiAliasing = "anti_aliasing";
			}
			public const string Properties = "appearance";
			public static class AppearanceMenu
			{
				public const string BkColour = "bk_colour";
				public const string ChartBkColour = "chart_bk_colour";
			}
			public const string Axes = "axes";
			public static class AxesMenu
			{
				public const string XAxis = "x_axis";
				public static class XAxisMenu
				{
					public const string Min = "min";
					public const string Max = "max";
					public const string AllowScroll = "allow_scroll";
					public const string AllowZoom = "allow_zoom";
				}
				public const string YAxis = "y_axis";
				public static class YAxisMenu
				{
					public const string Min = "min";
					public const string Max = "max";
					public const string AllowScroll = "allow_scroll";
					public const string AllowZoom = "allow_zoom";
				}
			}
		}
	}
}
