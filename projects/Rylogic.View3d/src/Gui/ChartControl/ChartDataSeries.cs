using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	/// <summary>Represents a data source that can be added to a chart control</summary>
	public partial class ChartDataSeries : ChartControl.Element
	{
		public ChartDataSeries(string name, EFormat format, OptionsData? options = null, int? capacity = null)
			: this(Guid.NewGuid(), name, format, options, capacity)
		{ }
		public ChartDataSeries(Guid id, string name, EFormat format, OptionsData? options = null, int? capacity = null)
			: base(id, m4x4.Identity, name)
		{
			m_data = new List<Pt>(capacity ?? 100);
			m_range_x = RangeF.Invalid;
			Cache = new ChartGfxCache(CreatePiece);
			Format = format;
			Options = options ?? new OptionsData();
		}
		public ChartDataSeries(XElement node)
			: base(node)
		{
			m_data = new List<Pt>();
			m_range_x = RangeF.Invalid;
			Cache = new ChartGfxCache(CreatePiece);
			Format = node.Element(nameof(Format)).As<EFormat>();
			Options = node.Element(nameof(Options)).As<OptionsData>();
		}
		protected override void Dispose(bool _)
		{
			Cache = null!;
			base.Dispose(_);
		}
		public override XElement ToXml(XElement node)
		{
			node.Add2(nameof(Format), Format, false);
			node.Add2(nameof(Options), Options, false);
			return base.ToXml(node);
		}

		/// <summary>Options for rendering this series</summary>
		public OptionsData Options { get; set; }

		/// <summary>Gain access to the underlying data</summary>
		public LockData Lock() => new LockData(this);
		private readonly List<Pt> m_data;

		/// <summary>Default identity colour</summary>
		public override Colour32 Colour => Options.Colour;

		/// <summary>Read the number of data points in the series. (Not synchronised by 'Lock()')</summary>
		public int SampleCount => m_data.Count;

		/// <summary>The format of the plot data</summary>
		public EFormat Format { get; set; }

		/// <summary>Return the X-Axis range of the data</summary>
		public RangeF RangeX
		{
			get
			{
				if (m_range_x == RangeF.Invalid)
				{
					using (Lock())
					{
						// 'Transform' only applies to the graphics
						if (m_data.Count == 0) return RangeF.Invalid;
						var beg = m_data.Front();
						var end = m_data.Back();
						m_range_x = new RangeF(beg.xf, end.xf);
					}
				}
				return m_range_x;
			}
		}
		private RangeF m_range_x;

		/// <summary>Cause all graphics models to be recreated</summary>
		public void FlushCachedGraphics()
		{
			Invalidate();
			Cache.Invalidate();
			m_range_x = RangeF.Invalid;
		}

		/// <summary>Cause graphics model that intersect 'x_range' to be recreated</summary>
		public void FlushCachedGraphics(RangeF x_range)
		{
			Invalidate();
			Cache.Invalidate(x_range);
			if (m_range_x != RangeF.Invalid)
				m_range_x.Grow(x_range);
		}

		/// <summary>Update the graphics for this series and add it to the scene</summary>
		protected override void UpdateSceneCore()
		{
			if (Chart == null)
				return;

			// Remove all series data graphics
			Chart.Scene.Window.RemoveObjects(new[] { Id }, 1, 0);

			// Add each graphics piece over the range
			if (Visible)
			{
				// If there is no data, then there's no graphics
				var range_x = RangeX;
				if (range_x == RangeF.Invalid)
					return;

				// Get the range required for display
				var range = new RangeF(
					Math.Max(Chart.XAxis.Min, range_x.Beg),
					Math.Min(Chart.XAxis.Max, range_x.End));

				// Add each graphics piece over the range
				foreach (var piece in Cache.Get(range).OfType<ChartGfxPiece>())
				{
					if (piece.Gfx == null) return;
					Chart.Scene.Window.AddObject(piece.Gfx);
				}
			}
		}

		/// <summary>Generate a piece of the graphics for 'x'</summary>
		private ChartGfxPiece CreatePiece(double x, RangeF missing)
		{
			Debug.Assert(missing.Contains(x));
			using (Lock())
			{
				// Find the nearest point in the data to 'x'
				var idx = m_data.BinarySearch(pt => pt.xf.CompareTo(x), find_insert_position: true);

				// Convert 'missing' to an index range within the data
				var idx_missing = new RangeI(
					m_data.BinarySearch(pt => pt.xf.CompareTo(missing.Beg), find_insert_position: true),
					m_data.BinarySearch(pt => pt.xf.CompareTo(missing.End), find_insert_position: true));

				// Limit the size of 'idx_missing' to the block size
				const int PieceBlockSize = 4096;
				var idx_range = new RangeI(
					Math.Max(idx_missing.Beg, idx - PieceBlockSize),
					Math.Min(idx_missing.End, idx + PieceBlockSize));

				// Create graphics over the data range 'idx_range'
				//todo: this isn't right... need to handle this function returning 'failed to create piece'
				switch (Options.PlotType)
				{
				default: throw new Exception($"Unsupported plot type: {Options.PlotType}");
				case EPlotType.Point:
					{
						return idx_range.Size > 0
							? CreatePointPlot(idx_range)
							: new ChartGfxPiece(null, missing);
					}
				case EPlotType.Line:
					{
						return idx_range.Size > 1
							? CreateLinePlot(idx_range)
							: new ChartGfxPiece(null, missing);
					}
				case EPlotType.StepLine:
					{
						return idx_range.Size > 1
							? CreateStepLinePlot(idx_range)
							: new ChartGfxPiece(null, missing);
					}
				case EPlotType.Bar:
					{
						return idx_range.Size > 1
							? CreateBarPlot(idx_range)
							: new ChartGfxPiece(null, missing);
					}
				}
			}
		}

		/// <summary>Create a point cloud plot</summary>
		private ChartGfxPiece CreatePointPlot(RangeI idx_range)
		{
			var n = idx_range.Sizei;

			// Resize the geometry buffers
			m_vbuf.Resize(n);
			m_ibuf.Resize(n);
			m_nbuf.Resize(1);

			// Create the vertex/index data
			var col = Options.Colour;
			var x_range = RangeF.Invalid;
			for (int i = 0, iend = n; i != iend; ++i)
			{
				var pt = m_data[i + idx_range.Begi];
				m_vbuf[i] = new View3d.Vertex(new v4((float)pt.xf, (float)pt.yf, 0f, 1f), col);
				m_ibuf[i] = (ushort)i;
				x_range.Grow(pt.xf);
			}

			// Create a nugget for the points using the sprite shader
			{
				var mat = View3d.Material.New();
				mat.m_diff_tex = View3d.Texture.FromStock((View3d.EStockTexture)Options.PointStyle)?.Handle ?? IntPtr.Zero;
				mat.Use(View3d.ERenderStep.ForwardRender, View3d.EShaderGS.PointSpritesGS, $"*PointSize {{{Options.PointSize} {Options.PointSize}}} *Depth {{{false}}}");
				m_nbuf[0] = new View3d.Nugget(View3d.ETopo.PointList, View3d.EGeom.Vert | View3d.EGeom.Colr | View3d.EGeom.Tex0, mat:mat);
			}

			// Create the graphics
			var gfx = new View3d.Object($"{Name}-[{idx_range.Beg},{idx_range.End})", 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray(), Id);
			return new ChartGfxPiece(gfx, x_range);
		}

		/// <summary>Create a line plot</summary>
		private ChartGfxPiece CreateLinePlot(RangeI idx_range)
		{
			var n = idx_range.Sizei;
			if (n == 0)
				throw new ArgumentException($"{nameof(ChartControl)}.{nameof(CreateLinePlot)} Index range must not be empty");

			// Resize the geometry buffers
			m_vbuf.Resize(n);
			m_ibuf.Resize(n);
			m_nbuf.Resize(1 + (Options.PointsOnLinePlot ? 1 : 0));

			// Create the vertex/index data
			int vert = 0, indx = 0;
			var col = Options.Colour;
			var x_range = RangeF.Invalid;
			for (int i = 0, iend = Math.Min(n, m_data.Count - idx_range.Begi); i != iend; ++i)
			{
				var j = i + idx_range.Begi;
				var pt = m_data[j];

				var v = vert;
				m_vbuf[vert++] = new View3d.Vertex(new v4((float)pt.xf, (float)pt.yf, 0f, 1f), col);
				m_ibuf[indx++] = (ushort)(v);

				x_range.Grow(pt.xf);
			}

			// Create a nugget for the list strip using the thick line shader
			{
				var mat = View3d.Material.New();
				mat.Use(View3d.ERenderStep.ForwardRender, View3d.EShaderGS.ThickLineListGS, $"*LineWidth {{{Options.LineWidth}}}");
				m_nbuf[0] = new View3d.Nugget(View3d.ETopo.LineStrip, View3d.EGeom.Vert | View3d.EGeom.Colr, mat:mat);
			}

			// Create a nugget for the points (if visible)
			if (Options.PointsOnLinePlot)
			{
				var mat = View3d.Material.New();
				mat.m_diff_tex = View3d.Texture.FromStock((View3d.EStockTexture)Options.PointStyle)?.Handle ?? IntPtr.Zero;
				mat.Use(View3d.ERenderStep.ForwardRender, View3d.EShaderGS.PointSpritesGS, $"*PointSize {{{Options.PointSize} {Options.PointSize}}} *Depth {{{false}}}");
				m_nbuf[1] = new View3d.Nugget(View3d.ETopo.PointList, View3d.EGeom.Vert | View3d.EGeom.Colr | View3d.EGeom.Tex0, range_overlaps:true, mat:mat);
			}

			// Create the graphics
			var gfx = new View3d.Object($"{Name}-[{idx_range.Beg},{idx_range.End})", 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray(), Id);
			return new ChartGfxPiece(gfx, x_range);
		}

		/// <summary>Create a step line plot</summary>
		private ChartGfxPiece CreateStepLinePlot(RangeI idx_range)
		{
			var n = idx_range.Sizei;

			// Resize the geometry buffers
			m_vbuf.Resize(2 * n);
			m_ibuf.Resize(2 * n + (Options.PointsOnLinePlot ? n : 0));
			m_nbuf.Resize(1 + (Options.PointsOnLinePlot ? 1 : 0));

			// Create the vertex/index data
			int vert = 0, indx = 0;
			var col = Options.Colour;
			var x_range = RangeF.Invalid;
			for (int i = 0, iend = Math.Min(n + 1, m_data.Count - idx_range.Begi); i != iend; ++i)
			{
				// Get the point and the next point
				var j = i + idx_range.Begi;
				var pt = m_data[j];
				var pt_r = j + 1 != m_data.Count ? m_data[j + 1] : pt;

				var v = vert;
				m_vbuf[vert++] = new View3d.Vertex(new v4((float)pt.xf, (float)pt.yf, 0f, 1f), col);
				m_vbuf[vert++] = new View3d.Vertex(new v4((float)pt_r.xf, (float)pt.yf, 0f, 1f), col);
				m_ibuf[indx++] = (ushort)(v + 0);
				m_ibuf[indx++] = (ushort)(v + 1);

				x_range.Grow(pt.xf);
			}

			// Create a nugget for the list strip using the thick line shader
			{
				var mat = View3d.Material.New();
				mat.Use(View3d.ERenderStep.ForwardRender, View3d.EShaderGS.ThickLineListGS, $"*LineWidth {{{Options.LineWidth}}}");
				m_nbuf[0] = new View3d.Nugget(View3d.ETopo.LineStrip, View3d.EGeom.Vert | View3d.EGeom.Colr, 0, (uint)vert, 0, (uint)indx, View3d.ENuggetFlag.None, false, mat);
			}

			// Create a nugget for the points (if visible)
			if (Options.PointsOnLinePlot)
			{
				// Add indices for the points
				var i0 = indx;
				for (int i = 0, iend = n; i != iend; ++i)
					m_ibuf[indx++] = (ushort)(i * 2);

				var mat = View3d.Material.New();
				mat.m_diff_tex = View3d.Texture.FromStock((View3d.EStockTexture)Options.PointStyle)?.Handle ?? IntPtr.Zero;
				mat.Use(View3d.ERenderStep.ForwardRender, View3d.EShaderGS.PointSpritesGS, $"*PointSize {{{Options.PointSize} {Options.PointSize}}} *Depth {{{false}}}");
				m_nbuf[1] = new View3d.Nugget(View3d.ETopo.PointList, View3d.EGeom.Vert | View3d.EGeom.Colr | View3d.EGeom.Tex0, 0, (uint)vert, (uint)i0, (uint)indx, View3d.ENuggetFlag.None, false, mat);
			}

			// Create the graphics
			var gfx = new View3d.Object($"{Name}-[{idx_range.Beg},{idx_range.End})", 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray(), Id);
			return new ChartGfxPiece(gfx, x_range);
		}

		/// <summary>Create a bar graph</summary>
		private ChartGfxPiece CreateBarPlot(RangeI idx_range)
		{
			var n = idx_range.Sizei;

			// Resize the geometry buffers
			m_vbuf.Resize(4 * n);
			m_ibuf.Resize(6 * n);
			m_nbuf.Resize(1);

			// Create the vertex/index data
			int vidx = 0, iidx = 0, nidx = 0;
			var col = Options.Colour;
			var lwidth = (0.0 - Options.BarHorizontalAlignment) * Options.BarWidth;
			var rwidth = (1.0 - Options.BarHorizontalAlignment) * Options.BarWidth;
			var x_range = RangeF.Invalid;
			for (int i = 0; i != n; ++i)
			{
				// Get the points on either side of 'i'
				var j = i + idx_range.Begi;
				var pt_l = j != 0 ? m_data[j - 1] : null;
				var pt = m_data[j];
				var pt_r = j + 1 != m_data.Count ? m_data[j + 1] : null;

				// Get the distance to the left and right of 'pt.x'
				var l = pt_l != null ? lwidth * (pt_l.xf - pt.xf) : 0;
				var r = pt_r != null ? rwidth * (pt_r.xf - pt.xf) : 0;
				if (j == 0 && j + 1 != m_data.Count && pt_r != null) l = -lwidth * (pt_r.xf - pt.xf);
				if (j + 1 == m_data.Count && j != 0 && pt_l != null) r = -rwidth * (pt_l.xf - pt.xf);

				var v = vidx;
				m_vbuf[vidx++] = new View3d.Vertex(new v4((float)(pt.xf + r), (float)(pt.yf >= 0f ? pt.yf : 0f), 0f, 1f), col);
				m_vbuf[vidx++] = new View3d.Vertex(new v4((float)(pt.xf - l), (float)(pt.yf >= 0f ? pt.yf : 0f), 0f, 1f), col);
				m_vbuf[vidx++] = new View3d.Vertex(new v4((float)(pt.xf - l), (float)(pt.yf >= 0f ? 0f : pt.yf), 0f, 1f), col);
				m_vbuf[vidx++] = new View3d.Vertex(new v4((float)(pt.xf + r), (float)(pt.yf >= 0f ? 0f : pt.yf), 0f, 1f), col);

				m_ibuf[iidx++] = (ushort)(v + 0);
				m_ibuf[iidx++] = (ushort)(v + 1);
				m_ibuf[iidx++] = (ushort)(v + 2);
				m_ibuf[iidx++] = (ushort)(v + 2);
				m_ibuf[iidx++] = (ushort)(v + 3);
				m_ibuf[iidx++] = (ushort)(v + 0);

				x_range.Grow(pt.xf);
			}

			// Create a nugget for the tri list
			uint v0 = 0, v1 = (uint)vidx;
			uint i0 = 0, i1 = (uint)iidx;
			var flags = col.A != 0xff ? View3d.ENuggetFlag.GeometryHasAlpha : View3d.ENuggetFlag.None;
			m_nbuf[nidx++] = new View3d.Nugget(View3d.ETopo.TriList, View3d.EGeom.Vert | View3d.EGeom.Colr, v0, v1, i0, i1, flags);

			// Add the bar 'tops'
			if (Options.LinesOnBarPlot)
			{
				// Use line strips
				m_vbuf.Resize(m_vbuf.Count + n * 2);
				m_ibuf.Resize(m_ibuf.Count + n * 2);
				m_nbuf.Resize(m_nbuf.Count + 1);

				var line_colour = col.Darken(0.25f).Alpha(1f);
				for (int i = 0; i != n; ++i)
				{
					var vert0 = m_vbuf[i * 4 + 0];
					var vert1 = m_vbuf[i * 4 + 1];
					var v = vidx;

					m_vbuf[vidx++] = new View3d.Vertex(vert0.m_pos, line_colour);
					m_vbuf[vidx++] = new View3d.Vertex(vert1.m_pos, line_colour);

					m_ibuf[iidx++] = (ushort)(v + 0);
					m_ibuf[iidx++] = (ushort)(v + 1);
				}

				// Create a nugget for the bar tops
				v0 = v1; v1 += (uint)(n * 2);
				i0 = i1; i1 += (uint)(n * 2);
				m_nbuf[nidx++] = new View3d.Nugget(View3d.ETopo.LineList, View3d.EGeom.Vert | View3d.EGeom.Colr, v0, v1, i0, i1);
			}

			// Create the graphics
			var gfx = new View3d.Object($"{Name}-[{idx_range.Beg},{idx_range.End})", 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray(), Id);
			return new ChartGfxPiece(gfx, x_range);
		}

		/// <summary>A cache of graphics pieces for this data series</summary>
		private ChartGfxCache Cache
		{
			get => m_impl_cache;
			set
			{
				if (m_impl_cache == value) return;
				Util.Dispose(ref m_impl_cache!);
				m_impl_cache = value;
			}
		}
		private ChartGfxCache m_impl_cache = null!;

		/// <summary>ToString</summary>
		public override string ToString() => $"{Name} count={m_data.Count}";

		/// <summary>Supported plot types</summary>
		public enum EPlotType
		{
			Point,
			Line,
			StepLine,
			Bar,
		}

		/// <summary>Formats for the plot data</summary>
		[Flags]
		public enum EFormat
		{
			XIntg = 1 << 0,
			XReal = 1 << 1,
			YIntg = 1 << 2,
			YReal = 1 << 3,

			XIntgYIntg = XIntg | YIntg,
			XIntgYReal = XIntg | YReal,
			XRealYIntg = XReal | YIntg,
			XRealYReal = XReal | YReal,
		}

		/// <summary>A single point in the data series. A class so that it can be sub-classed</summary>
		[DebuggerDisplay("{Description,nq}")]
		[StructLayout(LayoutKind.Explicit, Pack = 1)]
		public class Pt
		{
			// Notes:
			// - The format of this data is given by 'Format' in the 'ChartDataSeries' owner.
			// - Not IComparible because we don't know whether to compare 'xf' or 'xi'.
			//   Use the static 'CompareX?' functions below

			public Pt(double x_, double y_)
			{
				xf = x_;
				yf = y_;
			}
			public Pt(double x_, long y_)
			{
				xf = x_;
				yi = y_;
			}
			public Pt(long x_, double y_)
			{
				xi = x_;
				yf = y_;
			}
			public Pt(long x_, long y_)
			{
				xi = x_;
				yi = y_;
			}
			public Pt(Pt rhs)
			{
				xf = rhs.xf;
				yf = rhs.yf;
			}

			[FieldOffset(0)] public double xf;
			[FieldOffset(8)] public double yf;
			[FieldOffset(0)] public long xi;
			[FieldOffset(8)] public long yi;

			public static implicit operator Pt(v2 pt) { return new Pt(pt.x, pt.y); }
			public static implicit operator v2(Pt pt) { return new v2((float)pt.xf, (float)pt.yf); }
			public static implicit operator Pt(PointF pt) { return new Pt(pt.X, pt.Y); }
			public static implicit operator PointF(Pt pt) { return new PointF((float)pt.xf, (float)pt.yf); }

			/// <summary>Sorting predicate on X</summary>
			public static IComparer<Pt> CompareXf
			{
				get { return Cmp<Pt>.From((l, r) => l.xf.CompareTo(r.xf)); }
			}
			public static IComparer<Pt> CompareXi
			{
				get { return Cmp<Pt>.From((l, r) => l.xi.CompareTo(r.xi)); }
			}

			/// <summary>Guess at whether double or long values are used</summary>
			private string Description
			{
				get { return $"{(Math.Abs(xf) < 1e-200 ? xi.ToString() : xf.ToString())} {(Math.Abs(yf) < 1e-200 ? yi.ToString() : yf.ToString())}"; }
			}
		}

		/// <summary>RAII object for synchronising access to the underlying data</summary>
		public sealed class LockData : ICollection<Pt>, IDisposable
		{
			private RangeF m_changed_data_rangex;
			public LockData(ChartDataSeries owner)
			{
				Owner = owner;
				m_changed_data_rangex = RangeF.Invalid;
				Monitor.Enter(Data);
			}
			public void Dispose()
			{
				if (m_changed_data_rangex == RangeF.Max)
					Owner.FlushCachedGraphics();
				else if (m_changed_data_rangex != RangeF.Invalid)
					Owner.FlushCachedGraphics(m_changed_data_rangex);

				Monitor.Exit(Data);
			}

			/// <summary>The data series that this lock is on</summary>
			public ChartDataSeries Owner { get; }

			/// <summary>The number of elements in the data series</summary>
			public int Count
			{
				get => Data.Count;
				set
				{
					m_changed_data_rangex = RangeF.Max;
					Data.Resize(value);
				}
			}

			/// <summary>Reset the collection</summary>
			public void Clear()
			{
				m_changed_data_rangex = RangeF.Max;
				Data.Clear();
			}

			/// <summary>Add a datum point</summary>
			public Pt Add(Pt point)
			{
				m_changed_data_rangex.Grow(Format.HasFlag(EFormat.XIntg) ? point.xi : point.xf);
				Data.Add(point);
				return point;
			}

			/// <summary>Remove a datum point</summary>
			public bool Remove(Pt item)
			{
				m_changed_data_rangex = RangeF.Max;
				return Data.Remove(item);
			}

			/// <summary>Insert a datum point</summary>
			public Pt Insert(int index, Pt point)
			{
				m_changed_data_rangex.Grow(Format.HasFlag(EFormat.XIntg) ? point.xi : point.xf);
				Data.Insert(index, point);
				return point;
			}

			/// <summary>The data series</summary>
			public Pt this[int idx]
			{
				get { return Data[idx]; }
				set
				{
					m_changed_data_rangex.Grow(Format.HasFlag(EFormat.XIntg) ? value.xi : value.xf);
					Data[idx] = value;
				}
			}

			/// <summary>Search for a point</summary>
			public bool Contains(Pt point) => Data.Contains(point);

			/// <summary>Sort the data series on X values (F = doubles, I = longs)</summary>
			public void SortF()
			{
				m_changed_data_rangex = RangeF.Max;
				Data.Sort(Pt.CompareXf);
			}
			public void SortI()
			{
				m_changed_data_rangex = RangeF.Max;
				Data.Sort(Pt.CompareXi);
			}

			///<summary>
			/// Return the range of indices that need to be considered when plotting from 'xmin' to 'xmax'
			/// Note: in general, this range should include one point to the left of 'xmin' and one to the right
			/// of 'xmax' so that line graphs plot a line up to the border of the plot area</summary>
			public void IndexRange(double xmin, double xmax, out int imin, out int imax)
			{
				var lwr = new Pt(xmin, 0.0);
				var upr = new Pt(xmax, 0.0);

				imin = Data.BinarySearch(0, Count, lwr, Pt.CompareXf);
				if (imin < 0) imin = ~imin;
				if (imin != 0) --imin;

				imax = Data.BinarySearch(imin, Count - imin, upr, Pt.CompareXf);
				if (imax < 0) imax = ~imax;
				if (imax != Count) ++imax;
			}

			/// <summary>Enumerable access to the data given an index range</summary>
			public IEnumerable<Pt> Values(int i0 = 0, int i1 = int.MaxValue)
			{
				Debug.Assert(i0 >= 0 && i0 <= Count);
				Debug.Assert(i1 >= i0);
				for (int i = i0, iend = Math.Min(i1, Count); i != iend; ++i)
				{
					var pt = this[i];
					yield return pt;
				}
			}

			/// <summary>
			/// Returns the range of series data to consider when plotting from 'xmin' to 'xmax'
			/// Note: in general, this range should include one point to the left of 'xmin' and one to
			/// the right of 'xmax' so that line graphs plot a line up to the border of the plot area</summary>
			public IEnumerable<Pt> Values(double xmin, double xmax)
			{
				IndexRange(xmin, xmax, out var i0, out var i1);
				return Values(i0, i1);
			}

			/// <summary>
			/// Returns the range of indices to consider when plotting from 'xmin' to 'xmax'
			/// Note: in general, this range should include one point to the left of 'xmin' and one to
			/// the right of 'xmax' so that line graphs plot a line up to the border of the plot area</summary>
			public IEnumerable<int> Indices(double xmin, double xmax)
			{
				IndexRange(xmin, xmax, out var i0, out var i1);
				return Enumerable.Range(i0, i1 - i0);
			}

			/// <summary>The data format</summary>
			private EFormat Format => Owner.Format;

			/// <summary>Private so that all 'Add' are seen</summary>
			private List<Pt> Data => Owner.m_data;

			#region ICollection<Pt>
			void ICollection<Pt>.Add(Pt point) => Add(point);
			void ICollection<Pt>.CopyTo(Pt[] array, int arrayIndex) => Owner.m_data.CopyTo(array, arrayIndex);
			public IEnumerator<Pt> GetEnumerator() => Owner.m_data.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => Owner.m_data.GetEnumerator();
			bool ICollection<Pt>.IsReadOnly => false;
			#endregion
		}

		/// <summary>Plot colour generator</summary>
		public static Colour32 GenerateColour(int i)
		{
			return m_colours[i % m_colours.Length];
		}
		private static readonly Colour32[] m_colours =
		{
			Colour32.Black     ,
			Colour32.Blue      , Colour32.Red         , Colour32.Green      ,
			Colour32.DarkBlue  , Colour32.DarkRed     , Colour32.DarkGreen  ,
			Colour32.Purple    , Colour32.Turquoise   , Colour32.Magenta    ,
			Colour32.Orange    , Colour32.Yellow      ,
			Colour32.LightBlue , Colour32.LightSalmon , Colour32.LightGreen ,
		};

		/// <summary>Rendering options for this data series</summary>
		public class OptionsData : SettingsSet<OptionsData>
		{
			public OptionsData()
			{
				Colour = Colour32.Black;
				PlotType = EPlotType.Point;
				PointStyle = EPointStyle.Square;
				PointSize = 10f;
				LineWidth = 5f;
				PointsOnLinePlot = true;
				BarWidth = 0.8;
				BarHorizontalAlignment = 0.5;
				LinesOnBarPlot = true;
				//Visible        = true;
				//DrawData       = true;
				//DrawErrorBars  = false;
				//PlotType       = EPlotType.Line;
				//PointColour    = Color.FromArgb(0xff, 0x80, 0, 0);
				//PointSize      = 5f;
				//LineColour     = Color.FromArgb(0xff, 0, 0, 0);
				//LineWidth      = 1f;
				//BarColour      = Color.FromArgb(0xff, 0x80, 0, 0);
				//ErrorBarColour = Color.FromArgb(0x80, 0xff, 0, 0);
				//DrawMovingAvr  = false;
				//MAWindowSize   = 10;
				//MALineColour   = Color.FromArgb(0xff, 0, 0, 0xFF);
				//MALineWidth    = 3f;
			}
			public OptionsData(XElement node)
				: base(node)
			{ }
			public OptionsData(OptionsData rhs)
				: base(rhs)
			{ }

			/// <summary>The base colour for this plot</summary>
			public Colour32 Colour
			{
				get => get<Colour32>(nameof(Colour));
				set => set(nameof(Colour), value);
			}

			/// <summary>The plot type for the series</summary>
			public EPlotType PlotType
			{
				get => get<EPlotType>(nameof(PlotType));
				set => set(nameof(PlotType), value);
			}

			/// <summary>The style of points to draw</summary>
			public EPointStyle PointStyle
			{
				get => get<EPointStyle>(nameof(PointStyle));
				set => set(nameof(PointStyle), value);
			}

			/// <summary>The size of point data graphics</summary>
			public double PointSize
			{
				get => get<double>(nameof(PointSize));
				set => set(nameof(PointSize), value);
			}

			/// <summary>The width of the line in line plots</summary>
			public double LineWidth
			{
				get => get<double>(nameof(LineWidth));
				set => set(nameof(LineWidth), value);
			}

			/// <summary>True if point graphics should be drawn on line plots</summary>
			public bool PointsOnLinePlot
			{
				get => get<bool>(nameof(PointsOnLinePlot));
				set => set(nameof(PointsOnLinePlot), value);
			}

			/// <summary>The normalised bar width in bar plots</summary>
			public double BarWidth
			{
				get => get<double>(nameof(BarWidth));
				set => set(nameof(BarWidth), value);
			}

			/// <summary>The normalised alignment of the bar to the point. 0 = point is at the left edge, 0.5 = in the middle, 1.0 at the right edge</summary>
			public double BarHorizontalAlignment
			{
				get => get<double>(nameof(BarHorizontalAlignment));
				set => set(nameof(BarHorizontalAlignment), value);
			}

			/// <summary>True if line graphics should be drawn on the tops of bar plots</summary>
			public bool LinesOnBarPlot
			{
				get => get<bool>(nameof(LinesOnBarPlot));
				set => set(nameof(LinesOnBarPlot), value);
			}

			//public bool       Visible        { get; set; }
			//public bool       DrawData       { get; set; }
			//public bool       DrawErrorBars  { get; set; }
			//public EPlotType  PlotType       { get; set; }
			//public Color      BarColour      { get; set; }
			//public float      BarWidth       { get; set; }
			//public Color      ErrorBarColour { get; set; }
			//public bool       DrawMovingAvr  { get; set; }
			//public int        MAWindowSize   { get; set; }
			//public Color      MALineColour   { get; set; }
			//public float      MALineWidth    { get; set; }
		}
	}
}

