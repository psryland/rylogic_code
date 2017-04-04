using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;
using pr.win32;

namespace LDraw
{
	[DebuggerDisplay("{SceneName}")]
	public class SceneUI :ChartControl ,IDockable
	{
		private SceneUI() { InitializeComponent(); }
		public SceneUI(string name, Model model)
			:base(string.Empty, model.SceneSettings(name).Options)
		{
			InitializeComponent();
			Settings    = model.SceneSettings(name);
			Model       = model;
			ContextIds  = new HashSet<Guid>();
			DragDropCtx = new DragDrop();

			DockControl = new DockControl(this, name)
			{
				TabText = name,
				ShowTitle = false,
				TabCMenu = CreateTabCMenu(),
			};

			SceneName                = name;
			BorderStyle              = BorderStyle.FixedSingle;
			AllowDrop                = true;
			DefaultMouseControl      = true;
			DefaultKeyboardShortcuts = true;

			// Apply settings
			Window.FocusPointVisible  = Settings.Camera.FocusPointVisible;
			Window.OriginPointVisible = Settings.Camera.OriginPointVisible;
			Window.FocusPointSize     = Settings.Camera.FocusPointSize;
			Window.OriginPointSize    = Settings.Camera.OriginPointSize;
			Scene.Window.LightProperties  = new View3d.Light(Settings.Light);

		}
		protected override void Dispose(bool disposing)
		{
			CameraUI = null;
			LightingUI = null;
			DragDropCtx = null;
			DockControl = null;
			Model = null;
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

		/// <summary>The name of this scene</summary>
		public string SceneName
		{
			get { return m_name; }
			set
			{
				if (m_name == value) return;
				m_name = value;
				DockControl.TabText = m_name;
				Settings = Model.SceneSettings(m_name);
				Options = Settings.Options;
			}
		}
		private string m_name;

		/// <summary>App settings</summary>
		public SceneSettings Settings
		{
			[DebuggerStepThrough] get;
			private set;
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
					Settings.SettingChanged -= HandleSettingChanged;
					Scene.View3d.OnSourcesChanged -= HandleSourcesChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					Settings.SettingChanged += HandleSettingChanged;
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
				if (m_impl_dock_control != null)
				{
					m_impl_dock_control.ActiveChanged -= HandleSceneActive;
					Util.Dispose(ref m_impl_dock_control);
				}
				m_impl_dock_control = value;
				if (m_impl_dock_control != null)
				{
					m_impl_dock_control.ActiveChanged += HandleSceneActive;
				}
			}
		}
		private DockControl m_impl_dock_control;

		/// <summary>Context Ids of objects added to this scene</summary>
		public HashSet<Guid> ContextIds
		{
			get;
			private set;
		}

		/// <summary>Get/Set whether the focus point is visible in this scene</summary>
		public bool FocusPointVisible
		{
			get { return Window?.FocusPointVisible ?? false; }
			set { if (Window != null) Window.FocusPointVisible = value; }
		}

		/// <summary>Get/Set whether the origin point is visible in this scene</summary>
		public bool OriginPointVisible
		{
			get { return Window?.OriginPointVisible ?? false; }
			set { if (Window != null) Window.OriginPointVisible = value; }
		}

		/// <summary>Get/Set whether bounding boxes are visible in this scene</summary>
		public bool BBoxesVisible
		{
			get { return Window?.BBoxesVisible ?? false; }
			set { if (Window != null) Window.BBoxesVisible = value; }
		}

		/// <summary>Clear the instances from this scene</summary>
		public void Clear()
		{
			ContextIds.Clear();
			Invalidate();
		}

		/// <summary>Add a file source to this scene</summary>
		public void OpenFile(string filepath, bool additional, bool auto_range = true)
		{
			// If the file has already been loaded in another scene, just add instances to this scene
			var ctx_id = View3d.ContextIdFromFilepath(filepath);
			if (ctx_id != null)
			{
				if (!additional) Clear();
				ContextIds.Add(ctx_id.Value);
				if (auto_range)
				{
					Populate();
					AutoRange();
				}
				return;
			}

			// Otherwise, load the source in a background thread
			ThreadPool.QueueUserWorkItem(x =>
			{
				// Load a source file and save the context id for that file
				var id = View3d.LoadScriptSource(filepath, true, include_paths: Model.IncludePaths.ToArray());
				this.BeginInvoke(() =>
				{
					if (!additional) Clear();
					Model.ContextIds.Add(id);
					ContextIds.Add(id);
					if (auto_range)
					{
						Populate();
						AutoRange();
					}
				});
			});
		}

		/// <summary>Reset the camera to view objects in the scene</summary>
		public void ResetView(View3d.ESceneBounds bounds)
		{
			var bb = Window.SceneBounds(bounds);
			Camera.ResetView(bb, Settings.Camera.ResetForward, Settings.Camera.ResetUp);
		}

		/// <summary>Set the camera looking in the direction of 'fwd'</summary>
		public void CamForwardAxis(v4 fwd)
		{
			var c2w   = Camera.O2W;
			var focus = Camera.FocusPoint;
			var cam   = focus - fwd * Camera.FocusDist;

			Settings.Camera.ResetForward = fwd;
			Settings.Camera.ResetUp      = v4.Perpendicular(fwd, Settings.Camera.ResetUp);
			Options.ResetUp              = Settings.Camera.ResetUp;
			Options.ResetForward         = Settings.Camera.ResetForward;

			Camera.SetPosition(cam, focus, Settings.Camera.ResetUp);
		}

		/// <summary>Align the camera to the given axis</summary>
		public void AlignCamera(v4 axis)
		{
			Camera.AlignAxis = axis;
			Settings.Camera.AlignAxis = axis;
			if (axis != v4.Zero)
			{
				Settings.Camera.ResetForward = v4.Perpendicular(axis, Settings.Camera.ResetForward);
				Settings.Camera.ResetUp      = axis;
				Options.ResetUp              = Settings.Camera.ResetUp;
				Options.ResetForward         = Settings.Camera.ResetForward;
			}
		}

		/// <summary>Add objects to the scene just prior to rendering</summary>
		public void Populate()
		{
			// Add all objects to the window's drawlist
			foreach (var id in ContextIds)
				Window.AddObjects(id);

		//??	// Add bounding boxes
		//??	if (Settings.ShowBBoxes)
		//??		foreach (var id in ContextIds)
		//??			Window.AddObjects(id);
		}

		/// <summary>Show the object manager for this scene</summary>
		public void ShowObjectManager(bool show)
		{
			Window.ShowObjectManager(show);
		}

		/// <summary>Lazy created camera properties UI</summary>
		public CameraUI CameraUI
		{
			get { return m_camera_ui ?? (m_camera_ui = new CameraUI(this)); }
			set { Util.Dispose(ref m_camera_ui); }
		}
		private CameraUI m_camera_ui;

		/// <summary>Lazy creating lighting UI</summary>
		public LightingUI LightingUI
		{
			get { return m_lighting_ui ?? (m_lighting_ui = new LightingUI(this)); }
			set { Util.Dispose(ref m_lighting_ui); }
		}
		private LightingUI m_lighting_ui;

		/// <summary>Drag drop handler</summary>
		private DragDrop DragDropCtx
		{
			get { return m_dd; }
			set
			{
				if (m_dd == value) return;
				if (m_dd != null)
				{
					m_dd.DoDrop -= HandleDrop;
					m_dd.Detach(this);
				}
				m_dd = value;
				if (m_dd != null)
				{
					m_dd.Attach(this);
					m_dd.DoDrop += HandleDrop;
				}
			}
		}
		private DragDrop m_dd;
		private bool HandleDrop(object sender, DragEventArgs args, DragDrop.EDrop mode)
		{
			// File drop only
			if (!args.Data.GetDataPresent(DataFormats.FileDrop))
				return false;

			// The drop filepaths
			var files = (string[])args.Data.GetData(DataFormats.FileDrop);
			var extns = new string[] { ".ldr", ".csv", ".p3d", ".x" };
			if (!files.All(x => extns.Contains(Path_.Extn(x).ToLowerInvariant())))
				return false;

			// Set the drop effect to indicate what will happen if the item is dropped here
			// e.g. args.Effect = Ctrl is down ? DragDropEffects.Move : DragDropEffects.Copy;
			var shift_down = (args.KeyState & Win32.MK_SHIFT) != 0;
			args.Effect = shift_down ? DragDropEffects.Copy : DragDropEffects.Move;

			// 'mode' == 'DragDrop.EDrop.Drop' when the item is actually dropped
			if (mode == pr.util.DragDrop.EDrop.Drop)
			{
				var add = shift_down;
				foreach (var file in files)
				{
					OpenFile(file, add, false);
					add = true;
				}
				AutoRange();
			}

			// Return true because dropping is allowed/supported by this handler
			return true;
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
				Window.FocusPointVisible = Settings.Camera.FocusPointVisible;
				break;
			case nameof(CameraSettings.FocusPointSize):
				Window.FocusPointSize = Settings.Camera.FocusPointSize;
				break;
			case nameof(CameraSettings.OriginPointVisible):
				Window.OriginPointVisible = Settings.Camera.OriginPointVisible;
				break;
			case nameof(CameraSettings.OriginPointSize):
				Window.OriginPointSize = Settings.Camera.OriginPointSize;
				break;
			case nameof(Settings.Light):
				Scene.Window.LightProperties = new View3d.Light(Settings.Light);
				break;
			}
			Invalidate();
		}

		/// <summary>Handle this scene gaining or losing focus</summary>
		private void HandleSceneActive(object sender, ActiveContentChangedEventArgs e)
		{
			Options.BkColour = e.ContentNew == this ? Color.LightSteelBlue : Color.LightGray;
			Invalidate();
		}

		/// <summary>Create a context menu for the tab</summary>
		private ContextMenuStrip CreateTabCMenu()
		{
			var cmenu = new ContextMenuStrip();
			using (cmenu.SuspendLayout(true))
			{
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Rename"));
					opt.Click += (s,a) =>
					{
						using (var dlg = new PromptForm { Title = "Rename", PromptText = "Enter a name for the scene", Value = DockControl.TabText })
						{
							dlg.ShowDialog(this);
							SceneName = dlg.Value;
						}
					};
				}
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Clear"));
					opt.Click += (s,a) =>
					{
						Clear();
					};
				}
				cmenu.Items.AddSeparator();
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Close"));
					cmenu.Opening += (s,a) => opt.Enabled = Model.Scenes.Count > 1;
					opt.Click += (s,a) =>
					{
						DockControl.DockPane = null;
						Model.Scenes.Remove(this);
					};
				}
			}
			return cmenu;
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
