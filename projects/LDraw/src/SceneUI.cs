using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using pr.gfx;
using pr.gui;
using pr.util;

namespace LDraw
{
	public class SceneUI :ChartControl ,IDockable
	{
		private SceneUI() { InitializeComponent(); }
		public SceneUI(Model model)
			:base(string.Empty, model.Settings.Scene)
		{
			InitializeComponent();

			Name                          = "Scene";
			BorderStyle                   = BorderStyle.FixedSingle;
			AllowDrop                     = true;
			DefaultMouseControl           = true;
			DefaultKeyboardShortcuts      = true;
			Options.LockAspect            = 1.0f;
			Options.NavigationMode        = ENavMode.Scene3D;
			Window.FocusPointVisible      = true;
			Window.FocusPointSize         = 0.03f;
			Scene.Window.LightProperties  = new View3d.Light(model.Settings.Light);

			DockControl = new DockControl(this, Name) { TabText = Name };
			Model = model;
		}
		protected override void Dispose(bool disposing)
		{
			Model = null;
			DockControl = null;
			base.Dispose(disposing);
		}
		protected override void OnChartRendering(ChartRenderingEventArgs args)
		{
			Populate();
			base.OnChartRendering(args);
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
			case Keys.F5:
				#region
				{
					View3d.ReloadScriptSources();
					e.Handled = true;
					break;
				}
				#endregion
			}

			base.OnKeyDown(e);
		}

		/// <summary>The app logic</summary>
		protected Model Model
		{
			[DebuggerStepThrough] get { return m_model; }
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.DragDrop.Detach(this);
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.DragDrop.Attach(this);
				}
			}
		}
		private Model m_model;

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DockControl DockControl
		{
			[DebuggerStepThrough] get { return m_impl_dock_control; }
			private set
			{
				if (m_impl_dock_control == value) return;
				Util.Dispose(ref m_impl_dock_control);
				m_impl_dock_control = value;
			}
		}
		private DockControl m_impl_dock_control;

		/// <summary>Add objects to the scene just prior to rendering</summary>
		public void Populate()
		{
			// Add all objects to the window's drawlist
			foreach (var id in Model.ContextIds)
				Window.AddObjects(id);

			// Add bounding boxes
			if (Model.Settings.ShowBBoxes)
				foreach (var id in Model.ContextIds)
					Window.AddObjects(id);
		}

		#region Component Designer generated code
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// SceneUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.Name = "SceneUI";
			this.Size = new System.Drawing.Size(477, 333);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
