using System;
using System.Diagnostics;
using System.Windows;
using System.Xml.Linq;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF.DockContainerDetail
{
	/// <summary>Records the proportions used for each dock site at a branch level</summary>
	public class DockSizeData
	{
		/// <summary>The minimum size of a child control</summary>
		public const int MinChildSize = 20;

		public DockSizeData(double left = 0.25, double top = 0.25, double right = 0.25, double bottom = 0.25)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}
		public DockSizeData(DockSizeData rhs)
			: this(rhs.Left, rhs.Top, rhs.Right, rhs.Bottom)
		{ }
		public DockSizeData(XElement node)
		{
			Left = node.Element(nameof(Left)).As(Left);
			Top = node.Element(nameof(Top)).As(Top);
			Right = node.Element(nameof(Right)).As(Right);
			Bottom = node.Element(nameof(Bottom)).As(Bottom);
		}

		/// <summary>Save the state as XML</summary>
		public XElement ToXml(XElement node)
		{
			node.Add2(nameof(Left), Left, false);
			node.Add2(nameof(Top), Top, false);
			node.Add2(nameof(Right), Right, false);
			node.Add2(nameof(Bottom), Bottom, false);
			return node;
		}

		/// <summary>The branch associated with this sizes</summary>
		internal Branch Owner { get; set; }

		/// <summary>
		/// The size of the left, top, right, bottom panes. If >= 1, then the value is interpreted
		/// as pixels, if less than 1 then interpreted as a fraction of the client area width/height</summary>
		public double Left
		{
			get { return m_left; }
			set { SetProp(ref m_left, value); }
		}
		private double m_left;
		public double Top
		{
			get { return m_top; }
			set { SetProp(ref m_top, value); }
		}
		private double m_top;
		public double Right
		{
			get { return m_right; }
			set { SetProp(ref m_right, value); }
		}
		private double m_right;
		public double Bottom
		{
			get { return m_bottom; }
			set { SetProp(ref m_bottom, value); }
		}
		private double m_bottom;

		/// <summary>Update a property and raise an event if different</summary>
		private void SetProp(ref double prop, double value)
		{
			if (Equals(prop, value)) return;
			Debug.Assert(value >= 0);
			prop = value;
		}

		/// <summary>Get/Set the dock site size by EDockSite</summary>
		public double this[EDockSite ds]
		{
			get
			{
				switch (ds)
				{
				case EDockSite.Left: return Left;
				case EDockSite.Right: return Right;
				case EDockSite.Top: return Top;
				case EDockSite.Bottom: return Bottom;
				}
				return 1f;
			}
			set
			{
				switch (ds)
				{
				case EDockSite.Left: Left = value; break;
				case EDockSite.Right: Right = value; break;
				case EDockSite.Top: Top = value; break;
				case EDockSite.Bottom: Bottom = value; break;
				}
			}
		}

		/// <summary>Given a rectangular area, returns the sizes for each visible dock site (given in docked_mask)</summary>
		internal Thickness GetSizesForRect(Rect rect, EDockMask docked_mask, Size? centre_min_size = null)
		{
			// Ensure the centre dock pane is never fully obscured
			var csize = centre_min_size ?? Size.Empty;
			if (csize.Width == 0) csize.Width = MinChildSize;
			if (csize.Height == 0) csize.Height = MinChildSize;

			// See which dock sites actually have something docked
			var existsL = docked_mask.HasFlag(EDockMask.Left);
			var existsR = docked_mask.HasFlag(EDockMask.Right);
			var existsT = docked_mask.HasFlag(EDockMask.Top);
			var existsB = docked_mask.HasFlag(EDockMask.Bottom);

			// Get the size of each area
			var l = existsL ? (Left >= 1f ? Left : rect.Width * Left) : 0.0;
			var r = existsR ? (Right >= 1f ? Right : rect.Width * Right) : 0.0;
			var t = existsT ? (Top >= 1f ? Top : rect.Height * Top) : 0.0;
			var b = existsB ? (Bottom >= 1f ? Bottom : rect.Height * Bottom) : 0.0;

			// If opposite zones overlap, reduce the sizes
			var over_w = rect.Width - (l + csize.Width + r);
			var over_h = rect.Height - (t + csize.Height + b);
			if (over_w < 0)
			{
				if (existsL && existsR) { l += over_w * 0.5; r += (over_w + 1) * 0.5; }
				else if (existsL) { l = rect.Width - csize.Width; }
				else if (existsR) { r = rect.Width - csize.Width; }
			}
			if (over_h < 0)
			{
				if (existsT && existsB) { t += over_h * 0.5; b += (over_h + 1) * 0.5; }
				else if (existsT) { t = rect.Height - csize.Height; }
				else if (existsB) { b = rect.Height - csize.Height; }
			}

			// Return the sizes in pixels
			return new Thickness(Math.Max(0, l), Math.Max(0, t), Math.Max(0, r), Math.Max(0, b));
		}

		/// <summary>Get the size for a dock site in pixels, assuming an available area 'rect'</summary>
		internal double GetSize(EDockSite location, Rect rect, EDockMask docked_mask)
		{
			var area = GetSizesForRect(rect, docked_mask);

			// Return the size of the requested site
			switch (location)
			{
			default: throw new Exception($"No size value for dock zone {location}");
			case EDockSite.Centre: return 0;
			case EDockSite.Left: return area.Left;
			case EDockSite.Top: return area.Top;
			case EDockSite.Right: return area.Right;
			case EDockSite.Bottom: return area.Bottom;
			}
		}

		/// <summary>Get the size for a dock site in pixels, assuming an available area 'rect'</summary>
		internal void SetSize(EDockSite location, Rect rect, double value)
		{
			if (rect.Width == 0 || rect.Height == 0)
				throw new Exception($"Cannot set dock sizes based on zero rectangle");

			// Assign a fractional value for the dock site size
			switch (location)
			{
			default: throw new Exception($"No size value for dock zone {location}");
			case EDockSite.Centre: break;
			case EDockSite.Left: Left = value / (Left >= 1 ? 1.0 : rect.Width); break;
			case EDockSite.Top: Top = value / (Top >= 1 ? 1.0 : rect.Height); break;
			case EDockSite.Right: Right = value / (Right >= 1 ? 1.0 : rect.Width); break;
			case EDockSite.Bottom: Bottom = value / (Bottom >= 1 ? 1.0 : rect.Height); break;
			}
		}

		public static DockSizeData Halves => new DockSizeData(0.5, 0.5, 0.5, 0.5);
		public static DockSizeData Quarters => new DockSizeData(0.25, 0.25, 0.25, 0.25);
	}
}
