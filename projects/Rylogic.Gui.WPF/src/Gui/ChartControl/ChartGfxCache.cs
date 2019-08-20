using System;
using System.Collections.Generic;
using System.Diagnostics;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	/// <summary>A cache of graphics objects that span the X-Axis</summary>
	public class ChartGfxCache : IDisposable
	{
		// Notes:
		// - This cache is intended to be used by ChartDataSeries-like classes.
		//   If provides the functionality of breaking a data series up into pieces
		//   so that the limit of 64K indices is not exceeded.

		public ChartGfxCache(CreatePieceHandler handler)
		{
			Pieces = new List<ChartGfxPiece>();
			CreatePiece = handler;
		}
		public void Dispose()
		{
			Pieces = null;
		}

		/// <summary>Reset the cache</summary>
		public void Invalidate()
		{
			Util.DisposeRange(Pieces);
			Pieces.Clear();
		}
		public void Invalidate(RangeF x_range)
		{
			var beg = Pieces.BinarySearch(p => p.Range.CompareTo(x_range.Beg), find_insert_position: true);
			var end = Pieces.BinarySearch(p => p.Range.CompareTo(x_range.End), find_insert_position: true);
			Util.DisposeRange(Pieces, beg, end - beg);
			Pieces.RemoveRange(beg, end - beg);
		}

		/// <summary>The collection of cached graphics models</summary>
		private List<ChartGfxPiece> Pieces
		{
			get { return m_pieces; }
			set
			{
				if (m_pieces == value) return;
				Util.DisposeRange(m_pieces);
				m_pieces = value;
			}
		}
		private List<ChartGfxPiece> m_pieces;

		/// <summary>Get the series data graphics that spans the given x range</summary>
		public IEnumerable<ChartGfxPiece> Get(RangeF range)
		{
			// Return each graphics piece over the range
			for (var x = range.Beg; x < range.End;)
			{
				var piece = CacheGet(x);
				yield return piece;
				Debug.Assert(piece.Range.End > x);
				x = piece.Range.End;
			}
		}

		/// <summary>Return the graphics piece that spans 'x'</summary>
		public ChartGfxPiece CacheGet(double x)
		{
			// Search the cache for the model that spans 'x'
			var idx = Pieces.BinarySearch(p => p.Range.CompareTo(x));
			if (idx < 0)
			{
				idx = ~idx;

				// Get the X-range that is not cached
				var missing = new RangeF(
					idx != 0 ? Pieces[idx - 1].Range.End : double.MinValue,
					idx != Pieces.Count ? Pieces[idx].Range.Beg : double.MaxValue);

				// There is no cached graphics for 'x', create it now
				var piece = CreatePiece(x, missing);
				Pieces.Insert(idx, piece);
			}
			return Pieces[idx];
		}

		/// <summary>The handler for providing pieces of the graphics</summary>
		public CreatePieceHandler CreatePiece { get; set; }

		/// <summary>
		/// Implementers of this handler should return a graphics object that spans
		/// some region about 'x', clipped by 'missing'. The returned graphics piece
		/// should contain the x-range that the graphics represents.</summary>
		public delegate ChartGfxPiece CreatePieceHandler(double x, RangeF missing);
	}

	/// <summary>Graphics for a part of the series data</summary>
	public class ChartGfxPiece :IDisposable
	{
		public ChartGfxPiece(View3d.Object gfx, RangeF range)
		{
			Gfx = gfx;
			Range = range;
		}
		public void Dispose()
		{
			Gfx = null;
		}

		/// <summary>The model for the piece of the series data graphics</summary>
		public View3d.Object Gfx
		{
			get { return m_gfx; }
			private set
			{
				if (m_gfx == value) return;
				Util.Dispose(ref m_gfx);
				m_gfx = value;
			}
		}
		private View3d.Object m_gfx;

		/// <summary>The X-Axis span covered by this piece</summary>
		public RangeF Range { get; }
	}
}

