using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Rylogic.Attrib;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gfx
{
	public class Measurement :IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - This class provides the functionality for a measurement tool.
		//  - UI frameworks need to provide binding wrappers.
		//
		// General usage:
		//  - User clicks on 'Start' or 'End' toggle button.
		//  - MouseMove over the scene moves a hotspot around.
		//  - When a mouse click is detected, the Hit point for start/end is updated,
		//    and the toggle button returns to unchecked.
		//  - If the check button is unchecked, the Hit point is not set
		//  - User can still pan/rotate the scene with mouse drag operations

		public Measurement(View3d.Window window)
		{
			ReferenceFrame = EReferenceFrame.WorldSpace;
			Flags = View3d.EHitTestFlags.Verts | View3d.EHitTestFlags.Edges | View3d.EHitTestFlags.Faces;
			SnapDistance = 0.1;
			SpotColour = Colour32.Aqua;
			ContextIds = new Guid[1] { Guid.NewGuid() };
			IncludeCount = 0;
			ExcludeCount = 1;
			Results = new BindingDict<EQuantity, Result>(Enum<EQuantity>.Values.ToDictionary(x => x, x => new Result(x, "---")));
			m_hit0 = new Hit();
			m_hit1 = new Hit();
			
			Window = window;
		}
		public virtual void Dispose()
		{
			GfxHotSpot0 = null;
			GfxHotSpot1 = null;
			GfxMeasure = null;
			Window = null;
		}

		/// <summary>The 3D scene to do the measuring in</summary>
		public View3d.Window Window
		{
			get { return m_window; }
			set
			{
				if (m_window == value) return;
				if (m_window != null)
				{
					m_window.OnRendering -= HandleRendering;
					m_window.OnSceneChanged -= HandleSceneChanged;
				}
				m_window = value;
				if (m_window != null)
				{
					m_window.OnSceneChanged += HandleSceneChanged;
					m_window.OnRendering += HandleRendering;
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
							GfxHotSpot0.Colour = SpotColour;
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
							GfxHotSpot1.Colour = SpotColour;
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
		private View3d.Window m_window;

		/// <summary>The context Id to use for the measurement graphics</summary>
		public Guid CtxId
		{
			get { return ContextIds[ContextIds.Length - 1]; }
			set { ContextIds[ContextIds.Length - 1] = value; }
		}

		/// <summary>The include/exclude list of context ids</summary>
		public Guid[] ContextIds { get; private set; }
		private int IncludeCount;
		private int ExcludeCount;

		/// <summary>Set the context ids to include/exclude when hit testing</summary>
		public void SetContextIds(Guid[] context_ids, int include_count, int exclude_count)
		{
			// Preserve the graphics context id
			using (Scope.Create(() => CtxId, id => CtxId = id))
			{
				ContextIds = new Guid[context_ids.Length + 1];
				Array.Copy(context_ids, ContextIds, context_ids.Length);
				IncludeCount = include_count;
				ExcludeCount = exclude_count + 1;
			}
		}

		/// <summary>The colour of the hot spots</summary>
		public Colour32 SpotColour
		{
			get { return m_spot_colour; }
			set
			{
				if (m_spot_colour == value) return;
				m_spot_colour = value;
				Window?.Invalidate();
				NotifyPropertyChanged(nameof(SpotColour));
			}
		}
		private Colour32 m_spot_colour;
		
		/// <summary>The snap distance</summary>
		public double SnapDistance
		{
			get { return m_snap_distance; }
			set
			{
				if (SnapDistance == value) return;
				m_snap_distance = value;
				Window?.Invalidate();
				NotifyPropertyChanged(nameof(SnapDistance));
			}
		}
		private double m_snap_distance;

		/// <summary>The snap-to flags</summary>
		public View3d.EHitTestFlags Flags
		{
			get { return m_flags; }
			set
			{
				if (Flags == value) return;
				m_flags = value;
				Window?.Invalidate();
				NotifyPropertyChanged(nameof(Flags));
			}
		}
		private View3d.EHitTestFlags m_flags;

		/// <summary>The reference frame to display the measurements in</summary>
		public EReferenceFrame ReferenceFrame
		{
			get { return m_reference_frame; }
			set
			{
				if (ReferenceFrame == value) return;
				m_reference_frame = value;
				InvalidateGfxMeasure();
				UpdateResults();
				Window?.Invalidate();
				NotifyPropertyChanged(nameof(ReferenceFrame));
			}
		}
		private EReferenceFrame m_reference_frame;

		/// <summary>The start point to measure from</summary>
		public Hit Hit0
		{
			get { return m_hit0; }
			set
			{
				if (m_hit0 == value) return;
				m_hit0 = value;
				UpdateResults();
				InvalidateGfxMeasure();
			}
		}
		private Hit m_hit0;

		/// <summary>The start point to measure from</summary>
		public Hit Hit1
		{
			get { return m_hit1; }
			set
			{
				if (m_hit1 == value) return;
				m_hit1 = value;
				UpdateResults();
				InvalidateGfxMeasure();
			}
		}
		private Hit m_hit1;

		/// <summary>Get/Set the Hit point that is being set</summary>
		public Hit ActiveHit
		{
			get { return m_active_hit; }
			set
			{
				if (ActiveHit == value) return;
				Debug.Assert(value == null || value == Hit0 || value == Hit1, "ActiveHit must be either Hit0 or Hit1");
				m_active_hit = value;
				NotifyPropertyChanged(nameof(ActiveHit));
			}
		}
		private Hit m_active_hit;

		/// <summary>True when we can measure between 'Hit0' and 'Hit1'</summary>
		public bool MeasurementValid => Hit0.IsValid && Hit1.IsValid;

		/// <summary>A transform from reference frame space to world space</summary>
		public m4x4 RefSpaceToWorld
		{
			get
			{
				switch (ReferenceFrame)
				{
				default:
					throw new Exception($"Unknown reference frame: {ReferenceFrame}");
				case EReferenceFrame.WorldSpace:
					{
						return m4x4.Identity;
					}
				case EReferenceFrame.Object1Space:
					{
						if (!Hit0.IsValid) return m4x4.Identity;
						return Math_.Orthonormalise(Hit0.Obj.O2P);
					}
				case EReferenceFrame.Object2Space:
					{
						if (!Hit1.IsValid) return m4x4.Identity;
						return Math_.Orthonormalise(Hit1.Obj.O2P);
					}
				}
			}
		}

		/// <summary>Cast a ray into the scene to find the intercept</summary>
		public void UpdateActiveHitPosition(v2 point_cs)
		{
			if (ActiveHit == null)
				return;

			// Perform a hit test to update the position of the active hit
			var ray = new View3d.HitTestRay();
			Window.Camera.SSPointToWSRay(point_cs, out ray.m_ws_origin, out ray.m_ws_direction);
			var result = Window.HitTest(ray, (float)SnapDistance, Flags, ContextIds, IncludeCount, ExcludeCount);

			// Update the current hit point
			ActiveHit.PointWS = result.m_ws_intercept;
			ActiveHit.SnapType = result.m_snap_type;
			ActiveHit.Obj = result.HitObject;

			// Invalidate
			UpdateResults();
			InvalidateGfxMeasure();
			Window?.Invalidate();
		}

		/// <summary>Mouse handler helpers. 'point_cs' is the client space mouse position</summary>
		public void MouseDown(v2 point_cs)
		{
			if (ActiveHit == null) return;
			m_mouse_down_at = point_cs;
			m_is_drag = false;
		}
		public void MouseMove(v2 point_cs, double drag_threshold = 5.0)
		{
			if (ActiveHit == null) return;
			m_is_drag |= m_mouse_down_at != null && (point_cs - m_mouse_down_at.Value).Length > drag_threshold;
			UpdateActiveHitPosition(point_cs);
		}
		public void MouseUp()
		{
			m_mouse_down_at = null;
			if (ActiveHit == null) return;
			if (m_is_drag) return;

			// Lock the hit position
			if (ActiveHit == Hit0)
				ActiveHit = Hit1;
			else
				ActiveHit = null;
		}
		private v2? m_mouse_down_at;
		private bool m_is_drag;

		/// <summary>Graphics for the hotspot that follows the mouse around</summary>
		private View3d.Object GfxHotSpot0
		{
			get
			{
				if (m_gfx_hotspot0 == null)
				{
					var ldr = "*Point hotspot0 FF00FFFF { 0 0 0 *Size{20} *Style{Circle} *NoZTest *NoZWrite }";
					GfxHotSpot0 = new View3d.Object(ldr, false, CtxId) { Flags = View3d.EFlags.HitTestExclude };
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
					var ldr = "*Point hotspot1 FF00FFFF { 0 0 0 *Size{20} *Style{Circle}, *NoZTest }";
					GfxHotSpot1 = new View3d.Object(ldr, false, CtxId) { Flags = View3d.EFlags.HitTestExclude };
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
				if (m_gfx_measure == null && MeasurementValid)
				{
					var r2w = RefSpaceToWorld;
					var w2r = Math_.InvertFast(r2w);
					var pt0 = w2r * Hit0.PointWS;
					var pt1 = w2r * Hit1.PointWS;
					var dist_x = Math.Abs(pt1.x - pt0.x);
					var dist_y = Math.Abs(pt1.y - pt0.y);
					var dist_z = Math.Abs(pt1.z - pt0.z);
					var dist = (pt1 - pt0).Length;

					var sb = new StringBuilder();
					sb.Append(
						$"*Group Measurement \n" +
						$"{{ \n" +
						$"	*Font {{*Colour{{FFFFFFFF}}}}\n");
					if (dist_x != 0) sb.Append(
						$"	*Line dist_x FFFF0000\n" +
						$"	{{\n" +
						$"		{pt0.x} {pt0.y} {pt0.z} {pt1.x} {pt0.y} {pt0.z}\n" +
						$"		*Text lbl_x {{ \"{dist_x}\" *Billboard *BackColour{{FF800000}} *NoZTest *o2w{{*pos{{{(pt0.x + pt1.x) / 2} {pt0.y} {pt0.z}}}}} }}\n" +
						$"	}}\n");
					if (dist_y != 0) sb.Append(
						$"	*Line dist_y FF00FF00\n" +
						$"	{{\n" +
						$"		{pt1.x} {pt0.y} {pt0.z} {pt1.x} {pt1.y} {pt0.z}\n" +
						$"		*Text lbl_y {{ \"{dist_y}\" *Billboard *BackColour{{FF006000}} *NoZTest *o2w{{*pos{{{pt1.x} {(pt0.y + pt1.y) / 2} {pt0.z}}}}} }}\n" +
						$"	}}\n");
					if (dist_z != 0) sb.Append(
						$"	*Line dist_z FF0000FF\n" +
						$"	{{\n" +
						$"		{pt1.x} {pt1.y} {pt0.z} {pt1.xyz}\n" +
						$"		*Text lbl_z {{ \"{dist_z}\" *Billboard *BackColour{{FF000080}} *NoZTest *o2w{{*pos{{{pt1.x} {pt1.y} {(pt0.z + pt1.z) / 2}}}}} }}\n" +
						$"	}}\n");
					if (dist != 0) sb.Append(
						$"	*Line dist FF000000\n" +
						$"	{{\n" +
						$"		{pt0.xyz} {pt1.xyz}\n" +
						$"		*Text lbl_d {{ \"{dist}\" *Billboard *BackColour{{FF000000}} *NoZTest *o2w{{*pos{{{((pt0 + pt1) / 2).xyz}}}}} }}\n" +
						$"	}}\n");
					sb.Append(
						$"	*o2w{{*m4x4{{{r2w}}}}}\n" +
						$"}}");

					GfxMeasure = new View3d.Object(sb.ToString(), false, CtxId) { Flags = View3d.EFlags.HitTestExclude };
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

		/// <summary>Update the measurement graphics</summary>
		private void InvalidateGfxMeasure(object sender = null, EventArgs args = null)
		{
			GfxMeasure = null;
		}

		/// <summary>Property changed</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		protected void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>The measurement results</summary>
		public BindingDict<EQuantity, Result> Results { get; }

		/// <summary>Populate the results collection</summary>
		private void UpdateResults()
		{
			if (MeasurementValid)
			{
				// Convert the points into the selected space
				var w2rf = Math_.InvertFast(RefSpaceToWorld);
				var pt0 = w2rf * Hit0.PointWS;
				var pt1 = w2rf * Hit1.PointWS;

				Results[EQuantity.Distance] = new Result(EQuantity.Distance, (pt1 - pt0).Length.ToString());
				Results[EQuantity.DistanceX] = new Result(EQuantity.DistanceX, Math.Abs(pt1.x - pt0.x).ToString());
				Results[EQuantity.DistanceY] = new Result(EQuantity.DistanceY, Math.Abs(pt1.y - pt0.y).ToString());
				Results[EQuantity.DistanceZ] = new Result(EQuantity.DistanceZ, Math.Abs(pt1.z - pt0.z).ToString());
				Results[EQuantity.AngleXY] = new Result(EQuantity.AngleXY, Math_.RadiansToDegrees(Math.Atan2(Math.Abs(pt1.y - pt0.y), Math.Abs(pt1.x - pt0.x))).ToString());
				Results[EQuantity.AngleXZ] = new Result(EQuantity.AngleXZ, Math_.RadiansToDegrees(Math.Atan2(Math.Abs(pt1.z - pt0.z), Math.Abs(pt1.x - pt0.x))).ToString());
				Results[EQuantity.AngleYZ] = new Result(EQuantity.AngleYZ, Math_.RadiansToDegrees(Math.Atan2(Math.Abs(pt1.z - pt0.z), Math.Abs(pt1.y - pt0.y))).ToString());
			}
			else
			{
				foreach (var q in Enum<EQuantity>.Values)
					Results[q] = new Result(q, "---");
			}

			// Update the hit object if known
			Results[EQuantity.Instance0] = new Result(EQuantity.Instance0, Hit0.IsValid ? Hit0.Obj.Name : "---");
			Results[EQuantity.Instance1] = new Result(EQuantity.Instance1, Hit1.IsValid ? Hit1.Obj.Name : "---");
		}

		/// <summary>The end point of a measurement</summary>
		public class Hit
		{
			public Hit()
			{
				PointWS = v4.Origin;
				Obj = null;
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

		/// <summary>The single result of a measurement</summary>
		public class Result
		{
			public Result(EQuantity quantity, string value)
			{
				Quantity = quantity;
				Value = value;
			}

			/// <summary>The name of the measured quantity</summary>
			public EQuantity Quantity { get; }

			/// <summary>String name for the quantity</summary>
			public string QuantityName => Quantity.Desc();

			/// <summary>The result of the measurement</summary>
			public string Value { get; set; }
		}

		/// <summary>Types of measured quantities</summary>
		public enum EQuantity
		{
			[Desc("Distance")] Distance,
			[Desc("X Distance")] DistanceX,
			[Desc("Y Distance")] DistanceY,
			[Desc("Z Distance")] DistanceZ,
			[Desc("XY Angle")] AngleXY,
			[Desc("XZ Angle")] AngleXZ,
			[Desc("YZ Angle")] AngleYZ,
			[Desc("Object 1")] Instance0,
			[Desc("Object 2")] Instance1,
		}

		/// <summary>Spaces that measurements can be made in</summary>
		public enum EReferenceFrame
		{
			[Desc("World Space")] WorldSpace,
			[Desc("Start Object Space")] Object1Space,
			[Desc("End Object Space")] Object2Space,
		}
	}
}
