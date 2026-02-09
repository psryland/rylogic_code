using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Rylogic.Attrib;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WinForms
{
	public class View3dMeasurementUI :UserControl
	{
		// Usage:
		//  - User clicks on 'Start' or 'End' check button.
		//  - MouseMove over the scene moves a hotspot around.
		//  - When a mouse click is detected, the Hit point for start/end is updated, and the check button returns to unchecked.
		//  - If the check button is unchecked, the Hit point is not set
		//  - User can still pan/rotate the scene with mouse drag operations

		/// <summary>Filtering of objects to hit test</summary>
		private Guid[] m_context_ids;
		private int m_include_count;
		private int m_exclude_count;

		#region UI Elements
		private TableLayoutPanel m_table0;
		private Panel m_panel0;
		private Panel m_panel1;
		private DataGridView m_grid_measurement;
		private ValueBox m_tb_snap_distance;
		private CheckBox m_chk_verts;
		private CheckBox m_chk_edges;
		private CheckBox m_chk_faces;
		private CheckBox m_chk_point0;
		private CheckBox m_chk_point1;
		private Label m_lbl_snap_distance;
		private ComboBox m_cb_space;
		private Panel m_panel_spot_colour;
		private Label m_lbl_spot_colour;
		private Label m_lbl_measurements;
		#endregion

		public View3dMeasurementUI()
		{
			InitializeComponent();
			m_context_ids = new Guid[1] { Guid.NewGuid() };
			m_include_count = 0;
			m_exclude_count = 1;
			m_hit0 = new Hit();
			m_hit1 = new Hit();

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			GfxHotSpot0 = null;
			GfxHotSpot1 = null;
			GfxMeasure = null;
			Window = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			Window?.Invalidate();
		}

		/// <summary>The 3D scene to do the measuring in</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public View3d.Window Window
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					field.OnRendering -= HandleRendering;
					field.OnSceneChanged -= HandleSceneChanged;
					Control = null;
				}
				field = value;
				if (field != null)
				{
					Control = FromHandle(field.Hwnd);
					field.OnSceneChanged += HandleSceneChanged;
					field.OnRendering += HandleRendering;
				}

				// Handlers
				void HandleSceneChanged(object sender, View3d.SceneChangedEventArgs e)
				{
					if (e.ChangeType == View3d.ESceneChanged.ObjectsRemoved)
					{
						if (Hit0.Obj != null && !Window.HasObject(Hit0.Obj, true))
							Hit0.Obj = null;
						if (Hit1.Obj != null && !Window.HasObject(Hit1.Obj, true))
							Hit1.Obj = null;
					}
				}
				void HandleRendering(object sender, EventArgs e)
				{
					if (!Visible) return;

					// Add the graphics for the measurement
					if (GfxMeasure != null)
					{
						if (MeasurementValid)
							Window.AddObject(GfxMeasure);
						else
							Window.RemoveObject(GfxMeasure);
					}

					// Add the graphics for the hotspots
					if (GfxHotSpot0 != null)
					{
						if (Hit0.IsValid)
						{
							GfxHotSpot0.Colour = SnapTypeToColour(Hit0.SnapType);
							GfxHotSpot0.O2P = m4x4.Translation(Hit0.PointWS);
							Window.AddObject(GfxHotSpot0);
						}
						else
						{
							Window.RemoveObject(GfxHotSpot0);
						}
					}
					if (GfxHotSpot1 != null)
					{
						if (Hit1.IsValid)
						{
							GfxHotSpot1.Colour = SnapTypeToColour(Hit1.SnapType);
							GfxHotSpot1.O2P = m4x4.Translation(Hit1.PointWS);
							Window.AddObject(GfxHotSpot1);
						}
						else
						{
							Window.RemoveObject(GfxHotSpot1);
						}
					}
				}
			}
		}

		/// <summary>The WinForms control associated with 'Window'</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Control Control
		{
			// This defaults to the control associated with 'Window' however callers
			// can set a different control if necessary (i.e. the ChartControl).
			// This is needed for mouse interaction.
			get { return m_control; }
			set
			{
				if (m_control == value) return;
				if (m_control != null)
				{
					m_control.MouseUp   -= HandleMouseUp;
					m_control.MouseMove -= HandleMouseMove;
					m_control.MouseDown -= HandleMouseDown;
				}
				m_control = value;
				if (m_control != null)
				{
					m_control.MouseDown += HandleMouseDown;
					m_control.MouseMove += HandleMouseMove;
					m_control.MouseUp   += HandleMouseUp;
				}

				// Handlers
				void HandleMouseDown(object sender, MouseEventArgs e)
				{
					if (!Visible) return;
					if (ActiveHit == null) return;
					if (e.Button != MouseButtons.Left) return;
					m_mouse_down_at = v2.From(e.Location);
					m_is_drag = false;
				}
				void HandleMouseUp(object sender, MouseEventArgs e)
				{
					if (!Visible) return;
					if (ActiveHit == null) return;
					if (e.Button != MouseButtons.Left) return;
					if (m_is_drag) return;

					// Lock the hit position
					if (ActiveHit == Hit0)
						ActiveHit = Hit1;
					else
						ActiveHit = null;
				}
				void HandleMouseMove(object sender, MouseEventArgs e)
				{
					if (!Visible) return;
					if (ActiveHit == null) return;
					if (e.Button == MouseButtons.Left)
						m_is_drag |= (v2.From(e.Location) - m_mouse_down_at).Length > 5;

					DoHitTest(e.Location);
				}
			}
		}
		private Control m_control;
		private v2 m_mouse_down_at;
		private bool m_is_drag;

		/// <summary>The view of the scene</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public View3d.Camera Camera
		{
			get { return Window.Camera; }
		}

		/// <summary>The context Id to use for graphics</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Guid CtxId
		{
			get { return m_context_ids.Back(); }
			set { m_context_ids[m_context_ids.Length-1] = value; }
		}

		/// <summary>The include/exclude list of context ids</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Guid[] ContextIds { get { return m_context_ids; } }

		/// <summary>The start point to measure from</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Hit Hit0
		{
			get { return m_hit0; }
			set
			{
				if (m_hit0 == value) return;
				m_hit0 = value;
				InvalidateGfxMeasure();
			}
		}
		public Hit m_hit0;

		/// <summary>The start point to measure from</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Hit Hit1
		{
			get { return m_hit1; }
			set
			{
				if (m_hit1 == value) return;
				m_hit1 = value;
				InvalidateGfxMeasure();
			}
		}
		public Hit m_hit1;

		/// <summary>The reference frame to display the measurements in</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public EReferenceFrame ReferenceFrame
		{
			get { return (EReferenceFrame)m_cb_space.SelectedItem; }
			set
			{
				m_cb_space.SelectedItem = value;
				InvalidateMeasurementsGrid();
			}
		}

		/// <summary>The snap distance</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public float SnapDistance
		{
			get { return (float)m_tb_snap_distance.Value; }
			set { m_tb_snap_distance.Value = value; }
		}

		/// <summary>The snap-to flags</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public View3d.EHitTestFlags Flags
		{
			get
			{
				View3d.EHitTestFlags flags = 0;
				if (m_chk_verts.Checked) flags |= View3d.EHitTestFlags.Verts;
				if (m_chk_edges.Checked) flags |= View3d.EHitTestFlags.Edges;
				if (m_chk_faces.Checked) flags |= View3d.EHitTestFlags.Faces;
				return flags;
			}
			set
			{
				if (Flags == value) return;
				m_chk_verts.Checked = Bit.AllSet(value, View3d.EHitTestFlags.Verts);
				m_chk_edges.Checked = Bit.AllSet(value, View3d.EHitTestFlags.Edges);
				m_chk_faces.Checked = Bit.AllSet(value, View3d.EHitTestFlags.Faces);
			}
		}

		/// <summary>Set the context ids to include/exclude when hit testing</summary>
		public void SetContextIds(Guid[] context_ids, int include_count, int exclude_count)
		{
			// Preserve the graphics context id
			using (Scope.Create(() => CtxId, id => CtxId = id))
			{
				m_context_ids = new Guid[context_ids.Length + 1];
				Array.Copy(context_ids, m_context_ids, context_ids.Length);
				m_include_count = include_count;
				m_exclude_count = exclude_count + 1;
			}
		}

		/// <summary>The colour of the hot spots</summary>
		public Colour32 SpotColour
		{
			get { return m_panel_spot_colour.BackColor; }
			set { m_panel_spot_colour.BackColor = value; }
		}

		/// <summary>Toggle the Hit point that is set</summary>
		public Hit ActiveHit
		{
			get
			{
				return
					m_chk_point0.Checked ? Hit0 :
					m_chk_point1.Checked ? Hit1 :
					null;
			}
			set
			{
				m_chk_point0.Checked = value == Hit0;
				m_chk_point1.Checked = value == Hit1;
			}
		}

		/// <summary>True when we can measure between 'Hit0' and 'Hit1'</summary>
		public bool MeasurementValid
		{
			get { return Hit0.IsValid && Hit1.IsValid; }
		}

		/// <summary>A transform from reference frame space to world space</summary>
		public m4x4 RefSpaceToWorld
		{
			get
			{
				switch (ReferenceFrame)
				{
				default: throw new Exception($"Unknown reference frame: {ReferenceFrame}");
				case EReferenceFrame.WorldSpace:   return m4x4.Identity;
				case EReferenceFrame.Object1Space: return Hit0.IsValid ? Hit0.Obj.O2P : m4x4.Identity;
				case EReferenceFrame.Object2Space: return Hit1.IsValid ? Hit1.Obj.O2P : m4x4.Identity;
				}
			}
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			#region Hot Spots

			// Distance
			m_tb_snap_distance.ValueType = typeof(float);
			m_tb_snap_distance.ValidateText = t => float.TryParse(t, out var f) && f >= 0;
			m_tb_snap_distance.Value = 0.1f;
			m_tb_snap_distance.ValueCommitted += InvalidateGfxMeasure;

			// Snap types
			m_chk_verts.Checked = true;
			m_chk_edges.Checked = true;
			m_chk_faces.Checked = true;
			m_chk_verts.CheckedChanged += InvalidateGfxMeasure;
			m_chk_edges.CheckedChanged += InvalidateGfxMeasure;
			m_chk_faces.CheckedChanged += InvalidateGfxMeasure;

			// Spot colour
			m_panel_spot_colour.Click += (s,a) =>
			{
				using (var dlg = new ColorDialog { Color = SpotColour, AllowFullOpen = true, AnyColor = true, SolidColorOnly = true })
				{
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					SpotColour = dlg.Color;
				}
			};

			#endregion

			#region Start/End point
			m_chk_point0.Checked = false;
			m_chk_point0.CheckedChanged += (s,a) =>
			{
				if (m_chk_point0.Checked)
				{
					ActiveHit = Hit0;
					(Control.TopLevelControl as Form)?.Activate();
				}
			};
			m_chk_point1.Checked = false;
			m_chk_point1.CheckedChanged += (s,a) =>
			{
				if (m_chk_point1.Checked)
				{
					ActiveHit = Hit1;
					(Control.TopLevelControl as Form)?.Activate();
				}
			};
			#endregion

			#region Measurement Grid

			// Measurement coordinate system
			m_cb_space.DataSource = Enum<EReferenceFrame>.ValuesArray;
			m_cb_space.Format += (s,a) =>
			{
				a.Value = ((EReferenceFrame)a.ListItem).Desc();
			};
			m_cb_space.DropDownClosed += (s,a) =>
			{
				InvalidateGfxMeasure();
				Window.Invalidate();
				InvalidateMeasurementsGrid();
			};

			// Measurement data grid
			m_grid_measurement.VirtualMode = true;
			m_grid_measurement.AutoGenerateColumns = false;
			m_grid_measurement.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Quantity",
			});
			m_grid_measurement.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Value",
			});
			m_grid_measurement.CellValueNeeded += (s,a) =>
			{
				if (!m_grid_measurement.Within(a.ColumnIndex, a.RowIndex, out DataGridViewCell cell))
					return;

				// Display the quantities
				var quantity = (EQuantity)a.RowIndex;
				switch (a.ColumnIndex)
				{
				default:
					throw new Exception("Unknown column");
				case 0:
					{
						a.Value = quantity.Desc();
						break;
					}
				case 1:
					{
						// Convert the points into the selected space
						var w2rf = Math_.InvertFast(RefSpaceToWorld);
						var pt0 = w2rf * Hit0.PointWS;
						var pt1 = w2rf * Hit1.PointWS;
						switch (quantity)
						{
						default:
							{
								throw new Exception($"Unknown quantity: {quantity}");
							}
						case EQuantity.Distance:
							{
								a.Value = MeasurementValid ? (pt1 - pt0).Length.ToString() : "---";
								break;
							}
						case EQuantity.DistanceX:
							{
								a.Value = MeasurementValid ? Math.Abs(pt1.x - pt0.x).ToString() : "---";
								break;
							}
						case EQuantity.DistanceY:
							{
								a.Value = MeasurementValid ? Math.Abs(pt1.y - pt0.y).ToString() : "---";
								break;
							}
						case EQuantity.DistanceZ:
							{
								a.Value = MeasurementValid ? Math.Abs(pt1.z - pt0.z).ToString() : "---";
								break;
							}
						case EQuantity.AngleXY:
							{
								a.Value = MeasurementValid ? Math_.RadiansToDegrees(Math.Atan2(Math.Abs(pt1.y - pt0.y), Math.Abs(pt1.x - pt0.x))).ToString() : "---";
								break;
							}
						case EQuantity.AngleXZ:
							{
								a.Value = MeasurementValid ? Math_.RadiansToDegrees(Math.Atan2(Math.Abs(pt1.z - pt0.z), Math.Abs(pt1.x - pt0.x))).ToString() : "---";
								break;
							}
						case EQuantity.AngleYZ:
							{
								a.Value = MeasurementValid ? Math_.RadiansToDegrees(Math.Atan2(Math.Abs(pt1.z - pt0.z), Math.Abs(pt1.y - pt0.y))).ToString() : "---";
								break;
							}
						case EQuantity.Instance0:
							{
								a.Value = Hit0.IsValid ? Hit0.Obj.Name : "---";
								break;
							}
						case EQuantity.Instance1:
							{
								a.Value = Hit1.IsValid ? Hit1.Obj.Name : "---";
								break;
							}
						}
						break;
					}
				}
			};
			m_grid_measurement.KeyDown += DataGridView_.Copy;
			m_grid_measurement.ContextMenuStrip = DataGridView_.CMenu(m_grid_measurement, DataGridView_.EEditOptions.ReadOnly);
			m_grid_measurement.RowCount = Enum<EQuantity>.Count;

			#endregion
		}

		/// <summary></summary>
		private void DoHitTest(PointF mouse_location)
		{
			// Perform a hit test to update the position of the active hit
			//var ray = new View3d.HitTestRay();
			//Camera.SSPointToWSRay(mouse_location, out ray.m_ws_origin, out ray.m_ws_direction);
			var ray = Camera.RaySS(mouse_location);
			var result = Window.HitTest(ray, SnapDistance, Flags, m_context_ids, m_include_count, m_exclude_count);

			// Update the current hit point
			ActiveHit.PointWS  = result.m_ws_intercept;
			ActiveHit.SnapType = result.m_snap_type;
			ActiveHit.Obj      = result.HitObject;

			// Invalidate
			InvalidateGfxMeasure();
			InvalidateMeasurementsGrid();
			Window.Invalidate();
		}

		/// <summary>Graphics for the hotspot that follows the mouse around</summary>
		private View3d.Object GfxHotSpot0
		{
			get
			{
				if (m_gfx_hotspot0 == null)
				{
					var ldr =
						"*Point hotspot0 FF00FFFF { 0 0 0 *Size{20} *Style{Circle}, *NoZTest }";

					GfxHotSpot0 = new View3d.Object(ldr, false, CtxId){ Flags = View3d.ELdrFlags.HitTestExclude };
				}
				return m_gfx_hotspot0;
			}
			set
			{
				if (m_gfx_hotspot0 == value) return;
				Util.Dispose(ref m_gfx_hotspot0);
				m_gfx_hotspot0 = value;
			}
		}
		private View3d.Object m_gfx_hotspot0;

		/// <summary>Graphics for the hotspot that follows the mouse around</summary>
		private View3d.Object GfxHotSpot1
		{
			get
			{
				if (m_gfx_hotspot1 == null)
				{
					var ldr =
						"*Point hotspot1 FF00FFFF { 0 0 0 *Size{20} *Style{Circle}, *NoZTest }";

					GfxHotSpot1 = new View3d.Object(ldr, false, CtxId){ Flags = View3d.ELdrFlags.HitTestExclude };
				}
				return m_gfx_hotspot1;
			}
			set
			{
				if (m_gfx_hotspot1 == value) return;
				Util.Dispose(ref m_gfx_hotspot1);
				m_gfx_hotspot1 = value;
			}
		}
		private View3d.Object m_gfx_hotspot1;

		/// <summary>Measurement graphics</summary>
		private View3d.Object GfxMeasure
		{
			get
			{
				if ((m_gfx_measure == null || !m_gfx_measure_valid) && MeasurementValid)
				{
					var r2w    = RefSpaceToWorld;
					var w2r    = Math_.InvertFast(r2w);
					var pt0    = w2r * Hit0.PointWS;
					var pt1    = w2r * Hit1.PointWS;
					var dist_x = Math.Abs(pt1.x - pt0.x);
					var dist_y = Math.Abs(pt1.y - pt0.y);
					var dist_z = Math.Abs(pt1.z - pt0.z);
					var dist   = (pt1 - pt0).Length;

					var sb = new StringBuilder();
					sb.Append(
						$"*Group Measurement \n"+
						$"{{ \n"+
						$"	*Font {{*Colour{{FFFFFFFF}}}}\n");
					if (dist_x != 0) sb.Append(
						$"	*Line dist_x FFFF0000\n"+
						$"	{{\n"+
						$"		{pt0.x} {pt0.y} {pt0.z} {pt1.x} {pt0.y} {pt0.z}\n"+
						$"		*Text lbl_x {{ \"{dist_x}\" *Billboard *BackColour{{FF800000}} *NoZTest *o2w{{*pos{{{(pt0.x+pt1.x)/2} {pt0.y} {pt0.z}}}}} }}\n"+
						$"	}}\n");
					if (dist_y != 0) sb.Append(
						$"	*Line dist_y FF00FF00\n"+
						$"	{{\n"+
						$"		{pt1.x} {pt0.y} {pt0.z} {pt1.x} {pt1.y} {pt0.z}\n"+
						$"		*Text lbl_y {{ \"{dist_y}\" *Billboard *BackColour{{FF006000}} *NoZTest *o2w{{*pos{{{pt1.x} {(pt0.y+pt1.y)/2} {pt0.z}}}}} }}\n"+
						$"	}}\n");
					if (dist_z != 0) sb.Append(
						$"	*Line dist_z FF0000FF\n"+
						$"	{{\n"+
						$"		{pt1.x} {pt1.y} {pt0.z} {pt1.xyz}\n"+
						$"		*Text lbl_z {{ \"{dist_z}\" *Billboard *BackColour{{FF000080}} *NoZTest *o2w{{*pos{{{pt1.x} {pt1.y} {(pt0.z+pt1.z)/2}}}}} }}\n"+
						$"	}}\n");
					if (dist != 0) sb.Append(
						$"	*Line dist FF000000\n"+
						$"	{{\n"+
						$"		{pt0.xyz} {pt1.xyz}\n"+
						$"		*Text lbl_d {{ \"{dist}\" *Billboard *BackColour{{FF000000}} *NoZTest *o2w{{*pos{{{((pt0+pt1)/2).xyz}}}}} }}\n"+
						$"	}}\n");
					sb.Append(
						$"	*o2w{{*m4x4{{{r2w}}}}}\n"+
						$"}}");

					GfxMeasure = new View3d.Object(sb.ToString(), false, CtxId){ Flags = View3d.ELdrFlags.HitTestExclude };
					m_gfx_measure_valid = true;
				}
				return m_gfx_measure;
			}
			set
			{
				if (m_gfx_measure == value) return;
				Util.Dispose(ref m_gfx_measure);
				m_gfx_measure = value;
			}
		}
		private View3d.Object m_gfx_measure;
		private bool m_gfx_measure_valid;

		/// <summary>Update the measurement graphics</summary>
		private void InvalidateGfxMeasure(object sender = null, EventArgs args = null)
		{
			m_gfx_measure_valid = false;
		}

		/// <summary>Invalidate the values column of the measurements grid</summary>
		private void InvalidateMeasurementsGrid(object sender = null, EventArgs args = null)
		{
			m_grid_measurement.InvalidateColumn(1);
		}

		/// <summary>Return a colour for the given snap type</summary>
		private Colour32 SnapTypeToColour(View3d.ESnapType snap_type)
		{
			switch (snap_type)
			{
			default:
				throw new Exception($"Unknown snap type: {snap_type}");
			case View3d.ESnapType.Vert:
			case View3d.ESnapType.EdgeCentre:
			case View3d.ESnapType.FaceCentre:
			case View3d.ESnapType.Edge:
				return SpotColour;
			case View3d.ESnapType.Face:
				return SpotColour.LerpA(0xFF000000, 0.4f);
			}
		}

		/// <summary>The end point of a measurement</summary>
		public class Hit
		{
			public Hit()
			{
				PointWS  = v4.Origin;
				Obj      = null;
				SnapType = View3d.ESnapType.NoSnap;
			}

			/// <summary>The point in world space of the start of the measurement</summary>
			public v4 PointWS;

			/// <summary>The object that the measurement point is on</summary>
			public View3d.Object Obj;

			/// <summary>The type of point snap applied</summary>
			public View3d.ESnapType SnapType;

			/// <summary>True if this hit point is on a known object</summary>
			public bool IsValid => Obj != null;
		}

		public enum EQuantity
		{
			[Desc("Distance")] Distance,
			[Desc("X Distance")] DistanceX,
			[Desc("Y Distance")] DistanceY,
			[Desc("Z Distance")] DistanceZ,
			[Desc("ATan(Y/X)")] AngleXY,
			[Desc("ATan(Z/X)")] AngleXZ,
			[Desc("ATan(Z/Y)")] AngleYZ,
			[Desc("Object 1")] Instance0,
			[Desc("Object 2")] Instance1,
		}
		public enum EReferenceFrame
		{
			[Desc("World Space")]   WorldSpace,
			[Desc("Object1 Space")] Object1Space,
			[Desc("Object2 Space")] Object2Space,
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_tb_snap_distance = new Rylogic.Gui.WinForms.ValueBox();
			this.m_lbl_snap_distance = new System.Windows.Forms.Label();
			this.m_chk_verts = new System.Windows.Forms.CheckBox();
			this.m_chk_edges = new System.Windows.Forms.CheckBox();
			this.m_chk_faces = new System.Windows.Forms.CheckBox();
			this.m_chk_point0 = new System.Windows.Forms.CheckBox();
			this.m_chk_point1 = new System.Windows.Forms.CheckBox();
			this.m_table0 = new System.Windows.Forms.TableLayoutPanel();
			this.m_panel0 = new System.Windows.Forms.Panel();
			this.m_panel_spot_colour = new System.Windows.Forms.Panel();
			this.m_lbl_spot_colour = new System.Windows.Forms.Label();
			this.m_panel1 = new System.Windows.Forms.Panel();
			this.m_cb_space = new Rylogic.Gui.WinForms.ComboBox();
			this.m_grid_measurement = new Rylogic.Gui.WinForms.DataGridView();
			this.m_lbl_measurements = new System.Windows.Forms.Label();
			this.m_table0.SuspendLayout();
			this.m_panel0.SuspendLayout();
			this.m_panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_measurement)).BeginInit();
			this.SuspendLayout();
			// 
			// m_tb_snap_distance
			// 
			this.m_tb_snap_distance.BackColor = System.Drawing.Color.White;
			this.m_tb_snap_distance.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_snap_distance.BackColorValid = System.Drawing.Color.White;
			this.m_tb_snap_distance.CommitValueOnFocusLost = true;
			this.m_tb_snap_distance.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_snap_distance.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_snap_distance.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_snap_distance.Location = new System.Drawing.Point(13, 19);
			this.m_tb_snap_distance.Name = "m_tb_snap_distance";
			this.m_tb_snap_distance.Size = new System.Drawing.Size(107, 20);
			this.m_tb_snap_distance.TabIndex = 0;
			this.m_tb_snap_distance.UseValidityColours = true;
			this.m_tb_snap_distance.Value = null;
			// 
			// m_lbl_snap_distance
			// 
			this.m_lbl_snap_distance.AutoSize = true;
			this.m_lbl_snap_distance.Location = new System.Drawing.Point(2, 3);
			this.m_lbl_snap_distance.Name = "m_lbl_snap_distance";
			this.m_lbl_snap_distance.Size = new System.Drawing.Size(80, 13);
			this.m_lbl_snap_distance.TabIndex = 1;
			this.m_lbl_snap_distance.Text = "Snap Distance:";
			// 
			// m_chk_verts
			// 
			this.m_chk_verts.AutoSize = true;
			this.m_chk_verts.Location = new System.Drawing.Point(139, 3);
			this.m_chk_verts.Name = "m_chk_verts";
			this.m_chk_verts.Size = new System.Drawing.Size(64, 17);
			this.m_chk_verts.TabIndex = 1;
			this.m_chk_verts.Text = "Vertices";
			this.m_chk_verts.UseVisualStyleBackColor = true;
			// 
			// m_chk_edges
			// 
			this.m_chk_edges.AutoSize = true;
			this.m_chk_edges.Location = new System.Drawing.Point(139, 22);
			this.m_chk_edges.Name = "m_chk_edges";
			this.m_chk_edges.Size = new System.Drawing.Size(56, 17);
			this.m_chk_edges.TabIndex = 2;
			this.m_chk_edges.Text = "Edges";
			this.m_chk_edges.UseVisualStyleBackColor = true;
			// 
			// m_chk_faces
			// 
			this.m_chk_faces.AutoSize = true;
			this.m_chk_faces.Location = new System.Drawing.Point(139, 41);
			this.m_chk_faces.Name = "m_chk_faces";
			this.m_chk_faces.Size = new System.Drawing.Size(55, 17);
			this.m_chk_faces.TabIndex = 3;
			this.m_chk_faces.Text = "Faces";
			this.m_chk_faces.UseVisualStyleBackColor = true;
			// 
			// m_chk_point0
			// 
			this.m_chk_point0.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_chk_point0.Appearance = System.Windows.Forms.Appearance.Button;
			this.m_chk_point0.Location = new System.Drawing.Point(3, 66);
			this.m_chk_point0.Name = "m_chk_point0";
			this.m_chk_point0.Size = new System.Drawing.Size(115, 40);
			this.m_chk_point0.TabIndex = 0;
			this.m_chk_point0.Text = "Start Point";
			this.m_chk_point0.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.m_chk_point0.UseVisualStyleBackColor = true;
			// 
			// m_chk_point1
			// 
			this.m_chk_point1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_chk_point1.Appearance = System.Windows.Forms.Appearance.Button;
			this.m_chk_point1.Location = new System.Drawing.Point(124, 66);
			this.m_chk_point1.Name = "m_chk_point1";
			this.m_chk_point1.Size = new System.Drawing.Size(116, 40);
			this.m_chk_point1.TabIndex = 1;
			this.m_chk_point1.Text = "End Point";
			this.m_chk_point1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.m_chk_point1.UseVisualStyleBackColor = true;
			// 
			// m_table0
			// 
			this.m_table0.ColumnCount = 2;
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table0.Controls.Add(this.m_chk_point1, 1, 1);
			this.m_table0.Controls.Add(this.m_chk_point0, 0, 1);
			this.m_table0.Controls.Add(this.m_panel0, 0, 0);
			this.m_table0.Controls.Add(this.m_panel1, 0, 2);
			this.m_table0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table0.Location = new System.Drawing.Point(0, 0);
			this.m_table0.Margin = new System.Windows.Forms.Padding(0);
			this.m_table0.Name = "m_table0";
			this.m_table0.RowCount = 3;
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.m_table0.Size = new System.Drawing.Size(243, 330);
			this.m_table0.TabIndex = 8;
			// 
			// m_panel0
			// 
			this.m_panel0.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_table0.SetColumnSpan(this.m_panel0, 2);
			this.m_panel0.Controls.Add(this.m_panel_spot_colour);
			this.m_panel0.Controls.Add(this.m_lbl_spot_colour);
			this.m_panel0.Controls.Add(this.m_lbl_snap_distance);
			this.m_panel0.Controls.Add(this.m_chk_faces);
			this.m_panel0.Controls.Add(this.m_tb_snap_distance);
			this.m_panel0.Controls.Add(this.m_chk_edges);
			this.m_panel0.Controls.Add(this.m_chk_verts);
			this.m_panel0.Location = new System.Drawing.Point(0, 0);
			this.m_panel0.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel0.Name = "m_panel0";
			this.m_panel0.Size = new System.Drawing.Size(243, 63);
			this.m_panel0.TabIndex = 8;
			// 
			// m_panel_spot_colour
			// 
			this.m_panel_spot_colour.BackColor = System.Drawing.Color.Aqua;
			this.m_panel_spot_colour.Location = new System.Drawing.Point(74, 43);
			this.m_panel_spot_colour.Name = "m_panel_spot_colour";
			this.m_panel_spot_colour.Size = new System.Drawing.Size(46, 17);
			this.m_panel_spot_colour.TabIndex = 5;
			// 
			// m_lbl_spot_colour
			// 
			this.m_lbl_spot_colour.AutoSize = true;
			this.m_lbl_spot_colour.Location = new System.Drawing.Point(3, 44);
			this.m_lbl_spot_colour.Name = "m_lbl_spot_colour";
			this.m_lbl_spot_colour.Size = new System.Drawing.Size(65, 13);
			this.m_lbl_spot_colour.TabIndex = 4;
			this.m_lbl_spot_colour.Text = "Spot Colour:";
			// 
			// m_panel1
			// 
			this.m_panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_table0.SetColumnSpan(this.m_panel1, 2);
			this.m_panel1.Controls.Add(this.m_cb_space);
			this.m_panel1.Controls.Add(this.m_grid_measurement);
			this.m_panel1.Controls.Add(this.m_lbl_measurements);
			this.m_panel1.Location = new System.Drawing.Point(0, 109);
			this.m_panel1.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel1.Name = "m_panel1";
			this.m_panel1.Size = new System.Drawing.Size(243, 221);
			this.m_panel1.TabIndex = 9;
			// 
			// m_cb_space
			// 
			this.m_cb_space.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_space.BackColorValid = System.Drawing.Color.White;
			this.m_cb_space.CommitValueOnFocusLost = true;
			this.m_cb_space.DisplayProperty = null;
			this.m_cb_space.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_space.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_space.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_space.FormattingEnabled = true;
			this.m_cb_space.Location = new System.Drawing.Point(85, 3);
			this.m_cb_space.Name = "m_cb_space";
			this.m_cb_space.PreserveSelectionThruFocusChange = false;
			this.m_cb_space.Size = new System.Drawing.Size(117, 21);
			this.m_cb_space.TabIndex = 0;
			this.m_cb_space.UseValidityColours = true;
			this.m_cb_space.Value = null;
			// 
			// m_grid_measurement
			// 
			this.m_grid_measurement.AllowUserToAddRows = false;
			this.m_grid_measurement.AllowUserToDeleteRows = false;
			this.m_grid_measurement.AllowUserToResizeRows = false;
			this.m_grid_measurement.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid_measurement.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_measurement.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_grid_measurement.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid_measurement.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_measurement.ColumnHeadersVisible = false;
			this.m_grid_measurement.Location = new System.Drawing.Point(0, 27);
			this.m_grid_measurement.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_measurement.Name = "m_grid_measurement";
			this.m_grid_measurement.ReadOnly = true;
			this.m_grid_measurement.RowHeadersVisible = false;
			this.m_grid_measurement.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
			this.m_grid_measurement.Size = new System.Drawing.Size(243, 194);
			this.m_grid_measurement.TabIndex = 1;
			// 
			// m_lbl_measurements
			// 
			this.m_lbl_measurements.AutoSize = true;
			this.m_lbl_measurements.Location = new System.Drawing.Point(0, 6);
			this.m_lbl_measurements.Name = "m_lbl_measurements";
			this.m_lbl_measurements.Size = new System.Drawing.Size(79, 13);
			this.m_lbl_measurements.TabIndex = 2;
			this.m_lbl_measurements.Text = "Measurements:";
			// 
			// MeasurementUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_table0);
			this.MinimumSize = new System.Drawing.Size(205, 180);
			this.Name = "MeasurementUI";
			this.Size = new System.Drawing.Size(243, 330);
			this.m_table0.ResumeLayout(false);
			this.m_panel0.ResumeLayout(false);
			this.m_panel0.PerformLayout();
			this.m_panel1.ResumeLayout(false);
			this.m_panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_measurement)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
	}
}

