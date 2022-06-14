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
			: this()
		{
			Left = node.Element(nameof(Left)).As<double>();
			Top = node.Element(nameof(Top)).As<double>();
			Right = node.Element(nameof(Right)).As<double>();
			Bottom = node.Element(nameof(Bottom)).As<double>();
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
		internal Branch? Owner { get; set; }

		/// <summary>The size of the left pane. If >= 1, then the value is pixels, otherwise it's a fraction of the client width</summary>
		public double Left
		{
			get => m_left;
			set => SetProp(ref m_left, value);
		}
		private double m_left;

		/// <summary>The size of the top pane. If >= 1, then the value is pixels, otherwise it's a fraction of the client height</summary>
		public double Top
		{
			get => m_top;
			set => SetProp(ref m_top, value);
		}
		private double m_top;

		/// <summary>The size of the right pane. If >= 1, then the value is pixels, otherwise it's a fraction of the client width</summary>
		public double Right
		{
			get => m_right;
			set => SetProp(ref m_right, value);
		}
		private double m_right;

		/// <summary>The size of the bottom pane. If >= 1, then the value is pixels, otherwise it's a fraction of the client height</summary>
		public double Bottom
		{
			get => m_bottom;
			set => SetProp(ref m_bottom, value);
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
				return ds switch
				{
					EDockSite.Left => Left,
					EDockSite.Right => Right,
					EDockSite.Top => Top,
					EDockSite.Bottom => Bottom,
					_ => 1f,
				};
			}
			set
			{
				switch (ds)
				{
					case EDockSite.Left: Left = value; break;
					case EDockSite.Right: Right = value; break;
					case EDockSite.Top: Top = value; break;
					case EDockSite.Bottom: Bottom = value; break;
					default: throw new Exception($"Cannot set the size for site {ds}");
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
			return location switch
			{
				EDockSite.Centre => 0,
				EDockSite.Left => area.Left,
				EDockSite.Top => area.Top,
				EDockSite.Right => area.Right,
				EDockSite.Bottom => area.Bottom,
				_ => throw new Exception($"No size value for dock zone {location}"),
			};
		}

		/// <summary>Get the size for a dock site in pixels, assuming an available area 'rect'</summary>
		internal void SetSize(EDockSite location, Rect rect, double value, EDockResizeMode resize_mode)
		{
			if (rect.Width == 0 || rect.Height == 0)
				throw new Exception($"Cannot set dock sizes based on zero rectangle");

			// Assign a fractional value for the dock site size
			switch (location)
			{
				case EDockSite.Centre: break;
				case EDockSite.Left: Left = NewSize(value, Left, rect.Width); break;
				case EDockSite.Top: Top = NewSize(value, Top, rect.Height); break;
				case EDockSite.Right: Right = NewSize(value, Right, rect.Width); break;
				case EDockSite.Bottom: Bottom = NewSize(value, Bottom, rect.Height); break;
				default: throw new Exception($"No size value for dock zone {location}");
			}
			double NewSize(double value, double old_size, double max)
			{
				return resize_mode switch
				{
					EDockResizeMode.DontChange   => value / (old_size >= 1 ? 1.0 : max),
					EDockResizeMode.Proportional => value / max,
					EDockResizeMode.Absolute     => value,
					_ => throw new Exception($"Unknown resize mode: {resize_mode}"),
				};
			}
		}

		public static DockSizeData Halves => new DockSizeData(0.5, 0.5, 0.5, 0.5);
		public static DockSizeData Quarters => new DockSizeData(0.25, 0.25, 0.25, 0.25);
	}
}
