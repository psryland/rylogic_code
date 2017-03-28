using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using pr.common;
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
			Model = model;

			Name                          = "Scene";
			BorderStyle                   = BorderStyle.FixedSingle;
			AllowDrop                     = true;
			DefaultMouseControl           = true;
			DefaultKeyboardShortcuts      = true;

			// Apply settings
			Window.FocusPointVisible  = Model.Settings.Camera.FocusPointVisible;
			Window.OriginPointVisible = Model.Settings.Camera.OriginPointVisible;
			Window.FocusPointSize     = Model.Settings.Camera.FocusPointSize;
			Window.OriginPointSize    = Model.Settings.Camera.OriginPointSize;
			Scene.Window.LightProperties  = new View3d.Light(Model.Settings.Light);

			DockControl = new DockControl(this, Name) { TabText = Name };
		}
		protected override void Dispose(bool disposing)
		{
			Model = null;
			DockControl = null;
			base.Dispose(disposing);
		}
		protected override void OnRdrOptionsChanged()
		{
			base.OnRdrOptionsChanged();
			Model.Settings.Save();
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
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			Model.Owner.PointerLocationStatus.Text = PointerLocationText;
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
					m_model.Settings.SettingChanged -= HandleSettingChanged;
					Scene.View3d.OnSourcesChanged -= HandleSourcesChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.DragDrop.Attach(this);
					m_model.Settings.SettingChanged += HandleSettingChanged;
					Scene.View3d.OnSourcesChanged += HandleSourcesChanged;
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

		/// <summary>Handle notification that the script sources have changed</summary>
		private void HandleSourcesChanged(object sender, View3d.SourcesChangedEventArgs e)
		{
			if (e.Reason == View3d.ESourcesChangedReason.Reload)
			{
				// Just after reloading sources
				if (!e.Before && Model.Settings.ResetOnLoad)
				{
					Populate();
					AutoRange(View3d.ESceneBounds.All);
				}
			}
		}

		/// <summary>Apply setting changes</summary>
		private void HandleSettingChanged(object sender, SettingChangedEventArgs e)
		{
			switch (e.Key)
			{
			case nameof(CameraSettings.FocusPointVisible):
				Window.FocusPointVisible = Model.Settings.Camera.FocusPointVisible;
				break;
			case nameof(CameraSettings.FocusPointSize):
				Window.FocusPointSize = Model.Settings.Camera.FocusPointSize;
				break;
			case nameof(CameraSettings.OriginPointVisible):
				Window.OriginPointVisible = Model.Settings.Camera.OriginPointVisible;
				break;
			case nameof(CameraSettings.OriginPointSize):
				Window.OriginPointSize = Model.Settings.Camera.OriginPointSize;
				break;
			case nameof(Settings.Light):
				Scene.Window.LightProperties = new View3d.Light(Model.Settings.Light);
				break;
			}
			Invalidate();
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
