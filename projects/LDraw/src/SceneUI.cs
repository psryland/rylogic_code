using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Graphix;
using Rylogic.Gui;
using Rylogic.Maths;
using Rylogic.Utility;
using Rylogic.Windows32;

namespace LDraw
{
	[DebuggerDisplay("{SceneName}")]
	public class SceneUI :ChartControl ,IDockable
	{
		public const string DefaultName = "Scene";

		private SceneUI() { InitializeComponent(); }
		public SceneUI(string name, Model model)
			:base(string.Empty, model.SceneSettings(name).Options)
		{
			InitializeComponent();
			Settings    = model.SceneSettings(name);
			Model       = model;
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
			Window.FocusPointVisible     = Model.Settings.FocusPointVisible;
			Window.OriginPointVisible    = Model.Settings.OriginPointVisible;
			Window.FocusPointSize        = Model.Settings.FocusPointSize;
			Window.OriginPointSize       = Model.Settings.OriginPointSize;
			Window.SelectionBoxVisible   = Settings.ShowSelectionBox;
			Scene.Window.LightProperties = new View3d.Light(Settings.Light);

			CreateHandle();
		}
		public SceneUI(string name, Model model, XElement user_data)
			:this(name, model)
		{
			// Load the scene camera
			if (user_data != null)
				Camera.Load(user_data.Element(nameof(Camera)));
		}
		protected override void Dispose(bool disposing)
		{
			ShowMeasurementUI = false;
			CameraUI = null;
			LightingUI = null;
			ObjectManagerUI = null;
			DragDropCtx = null;
			DockControl = null;
			Settings = null;
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
			// Update the selection box if necessary
			if (SelectionBoxVisible)
				Window.SelectionBoxFitToSelected();

			base.OnChartRendering(args);
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
				Settings = Model.SceneSettings(m_name, Settings);
				Options = Settings.Options;

				// Invalidate anything looking at this scene
				Model.Scenes.ResetItem(this, optional:true);
			}
		}
		private string m_name;

		/// <summary>App settings</summary>
		public SceneSettings Settings { [DebuggerStepThrough] get; private set; }

		/// <summary>The app logic</summary>
		public Model Model
		{
			[DebuggerStepThrough] get { return m_model; }
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.Settings.SettingChanged -= HandleSettingChanged;
					Scene.View3d.OnSourcesChanged -= HandleSourcesChanged;
					Scene.Window.OnSceneChanged -= HandleSceneChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					Scene.Window.OnSceneChanged += HandleSceneChanged;
					Scene.View3d.OnSourcesChanged += HandleSourcesChanged;
					m_model.Settings.SettingChanged += HandleSettingChanged;
				}

				// Handlers
				void HandleSceneChanged(object sender, View3d.SceneChangedEventArgs args)
				{
					SceneChanged.Raise(this, args);
					Invalidate();
				}
				void HandleSourcesChanged(object sender, View3d.SourcesChangedEventArgs args)
				{
					if (args.Reason == View3d.ESourcesChangedReason.Reload)
					{
						// Just after reloading sources
						if (!args.Before && Model.Settings.ResetOnLoad)
							AutoRange(View3d.ESceneBounds.All);
					}
				}
				void HandleSettingChanged(object sender, SettingChangedEventArgs args)
				{
					switch (args.Key)
					{
					case nameof(LDraw.Settings.FocusPointVisible):
						Window.FocusPointVisible = Model.Settings.FocusPointVisible;
						break;
					case nameof(LDraw.Settings.FocusPointSize):
						Window.FocusPointSize = Model.Settings.FocusPointSize;
						break;
					case nameof(LDraw.Settings.OriginPointVisible):
						Window.OriginPointVisible = Model.Settings.OriginPointVisible;
						break;
					case nameof(LDraw.Settings.OriginPointSize):
						Window.OriginPointSize = Model.Settings.OriginPointSize;
						break;
					case nameof(LDraw.SceneSettings.ShowBBoxes):
						Window.BBoxesVisible = Settings.ShowBBoxes;
						break;
					case nameof(LDraw.SceneSettings.ShowSelectionBox):
						Window.SelectionBoxVisible = Settings.ShowSelectionBox;
						break;
					case nameof(LDraw.SceneSettings.Light):
						Scene.Window.LightProperties = new View3d.Light(Settings.Light);
						break;
					}
					Invalidate();
				}
			}
		}
		private Model m_model;

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DockControl DockControl
		{
			[DebuggerStepThrough] get { return m_dock_control; }
			private set
			{
				if (m_dock_control == value) return;
				if (m_dock_control != null)
				{
					m_dock_control.ActiveChanged -= HandleSceneActive;
					m_dock_control.SavingLayout -= HandleSavingLayout;
					Util.Dispose(ref m_dock_control);
				}
				m_dock_control = value;
				if (m_dock_control != null)
				{
					m_dock_control.SavingLayout += HandleSavingLayout;
					m_dock_control.ActiveChanged += HandleSceneActive;
				}

				// Handlers
				void HandleSceneActive(object sender, ActiveContentChangedEventArgs args)
				{
					Options.BkColour = args.ContentNew == this ? Color.LightSteelBlue : Color.LightGray;
					Invalidate();
				}
				void HandleSavingLayout(object sender, DockContainerSavingLayoutEventArgs args)
				{
					args.Node.Add2(nameof(Camera), Camera, false);
				}
			}
		}
		private DockControl m_dock_control;

		/// <summary>Raised when objects are added/removed from the scene</summary>
		public event EventHandler<View3d.SceneChangedEventArgs> SceneChanged;

		/// <summary>Get/Set whether the focus point is visible in this scene</summary>
		public bool FocusPointVisible
		{
			get { return Window?.FocusPointVisible ?? false; }
			set
			{
				if (FocusPointVisible == value) return;
				Model.Settings.FocusPointVisible = value;
				if (Window != null) Window.FocusPointVisible = value;
			}
		}

		/// <summary>Get/Set whether the origin point is visible in this scene</summary>
		public bool OriginPointVisible
		{
			get { return Window?.OriginPointVisible ?? false; }
			set
			{
				if (OriginPointVisible == value) return;
				Model.Settings.OriginPointVisible = value;
				if (Window != null) Window.OriginPointVisible = value;
			}
		}

		/// <summary>Get/Set whether the selection box is visible in this scene</summary>
		public bool SelectionBoxVisible
		{
			get { return Window?.SelectionBoxVisible ?? false; }
			set
			{
				if (SelectionBoxVisible == value) return;
				Settings.ShowSelectionBox = value;
				if (Window != null) Window.SelectionBoxVisible = value;
			}
		}

		/// <summary>Get/Set whether bounding boxes are visible in this scene</summary>
		public bool BBoxesVisible
		{
			get { return Window?.BBoxesVisible ?? false; }
			set
			{
				if (BBoxesVisible == value) return;
				Settings.ShowBBoxes = value;
				if (Window != null) Window.BBoxesVisible = value;
			}
		}

		/// <summary>The filepath of the last file loaded into this scene</summary>
		public string LastFilepath { get; private set; }

		/// <summary>Clear the instances from this scene</summary>
		public void Clear(bool delete_objects)
		{
			// Remove all objects from the window
			Window.RemoveObjects(new [] { ChartTools.Id }, 0, 1);

			// Delete unused objects
			if (delete_objects)
				View3d.DeleteUnused(new[] { ChartTools.Id }, 0, 1);

			Invalidate();
		}

		/// <summary>Add a file source to this scene</summary>
		public void OpenFile(string filepath, bool additional, bool auto_range = true)
		{
			// Record the filepath of the opened file
			if (!additional)
				LastFilepath = filepath;

			// If the file has already been loaded in another scene, just add instances to this scene
			var ctx_id = View3d.ContextIdFromFilepath(filepath);
			if (ctx_id != null)
			{
				AddObjects(ctx_id.Value);
			}
			else
			{
				// Otherwise, load the source in a background thread
				ThreadPool.QueueUserWorkItem(x =>
				{
					// Load a source file and save the context id for that file
					var id = View3d.LoadScriptSource(filepath, true, include_paths: Model.IncludePaths.ToArray());
					this.BeginInvoke(() => AddObjects(id));
				});
			}

			// Add objects associated with 'id' to this scene
			void AddObjects(Guid id)
			{
				if (!additional)
				{
					// Remove all objects from the window (except chart tools)
					Window.RemoveObjects(new[] { ChartTools.Id }, 0, 1);

					// Delete unused objects, except the chart tools and those belonging to the file just opened
					View3d.DeleteUnused(new [] { id, ChartTools.Id }, 0, 2);
				}

				// Add the objects from 'id' this scene.
				Window.AddObjects(new[] { id }, 1, 0);

				// Refresh the window
				if (auto_range)
					AutoRange();
				else
					Invalidate();
			}
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
			Settings.Camera.ResetUp      = Math_.Perpendicular(fwd, Settings.Camera.ResetUp);
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
				Settings.Camera.ResetForward = Math_.Perpendicular(axis, Settings.Camera.ResetForward);
				Settings.Camera.ResetUp      = axis;
				Options.ResetUp              = Settings.Camera.ResetUp;
				Options.ResetForward         = Settings.Camera.ResetForward;
			}
		}

		/// <summary>Show the object manager for this scene</summary>
		public void ShowObjectManager(bool show)
		{
			//Window.ShowObjectManager(show);
			ObjectManagerUI.Show(TopLevelControl);
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

		/// <summary></summary>
		public ObjectManagerUI ObjectManagerUI
		{
			get { return m_obj_mgr_ui ?? (m_obj_mgr_ui = new ObjectManagerUI(this)); }
			set { Util.Dispose(ref m_obj_mgr_ui); }
		}
		private ObjectManagerUI m_obj_mgr_ui;

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

				// Handlers
				bool HandleDrop(object sender, DragEventArgs args, DragDrop.EDrop mode)
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
					if (mode == Rylogic.Gui.DragDrop.EDrop.Drop)
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
			}
		}
		private DragDrop m_dd;

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
						using (var dlg = new PromptUI { Title = "Rename", PromptText = "Enter a name for the scene", Value = DockControl.TabText })
						{
							dlg.ShowDialog(this);
							SceneName = (string)dlg.Value;
						}
					};
				}
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Clear"));
					opt.Click += (s,a) =>
					{
						Clear(delete_objects:true);
					};
				}
				cmenu.Items.AddSeparator();
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Close"));
					opt.Click += (s,a) =>
					{
						DockControl.DockPane = null;
						Model.Scenes.Remove(this);
					};
				}
			}
			return cmenu;
		}

		#region Measurement UI

		/// <summary>Enable/Disable the measurement UI</summary>
		public bool ShowMeasurementUI
		{
			get { return m_measure_ui != null; }
			set
			{
				if (ShowMeasurementUI == value) return;
				if (m_measure_ui != null)
				{
					Util.Dispose(ref m_measure_ui);
				}
				m_measure_ui = value ? new MeasurementUI(this) : null;
				if (m_measure_ui != null)
				{
					m_measure_ui.Show(this);
				}
			}
		}
		private MeasurementUI m_measure_ui;

		/// <summary>Tool window wrapper of the measurement control</summary>
		private class MeasurementUI :ToolForm
		{
			public MeasurementUI(SceneUI scene)
				:base(scene.Model.Owner, EPin.TopLeft, Control_.MapPoint(scene, scene.Model.Owner, new Point(10,10)), new Size(264,330), false)
			{
				FormBorderStyle = FormBorderStyle.SizableToolWindow;
				ShowInTaskbar = false;
				HideOnClose = false;
				Text = "Measure";

				Controls.Add2(new View3d.MeasurementUI
				{
					Dock = DockStyle.Fill,
					CtxId = ChartTools.Id,
					Window = scene.Window,
					Control = scene,
				});

				FormClosed += (s,a) =>
				{
					if (a.CloseReason == CloseReason.UserClosing)
						scene.ShowMeasurementUI = false;
				};
			}
		}

		#endregion

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
