using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Graphix;

namespace Rylogic.Gui
{
	/// <summary>Fancy switch like windows 10 uses</summary>
	public class SwitchCheckBox :Button
	{
		public SwitchCheckBox()
		{
			AutoCheck      = true;
			ThumbColor     = Color.WhiteSmoke;
			UncheckedColor = Color_.FromArgb(0xFF808080);
			CheckedColor   = Color_.FromArgb(0xFF1C76FF);
			Padding        = new Padding(3);
		}

		/// <summary>The button colour when unchecked</summary>
		public Color UncheckedColor
		{
			get;
			set;
		}

		/// <summary>The button colour when checked</summary>
		public Color CheckedColor
		{
			get;
			set;
		}

		/// <summary>The colour of the centre thumb stick</summary>
		public Color ThumbColor
		{
			get;
			set;
		}

		/// <summary>True while the button is pressed</summary>
		public bool Pressed
		{
			get;
			private set;
		}

		/// <summary>True while the mouse is over the button</summary>
		public bool Hovered
		{
			get;
			private set;
		}

		/// <summary>Check state of the button</summary>
		public bool Checked
		{
			get { return m_checked; }
			set
			{
				if (m_checked == value) return;
				m_checked = value;
				OnCheckedChanged();
			}
		}
		private bool m_checked;

		/// <summary>True if 'Checked' is automatically toggled on click</summary>
		public bool AutoCheck
		{
			get;
			set;
		}

		/// <summary>Raised when the checked state changes</summary>
		public event EventHandler CheckedChanged;
		protected void OnCheckedChanged()
		{
			CheckedChanged.Raise(this);
		}

		public override Size GetPreferredSize(Size sz)
		{
			var size = new Size(64, 32);
			if (sz.Width < size.Width) size = new Size(sz.Width, sz.Width / 2);
			if (sz.Height < size.Height) size = new Size(sz.Height * 2, sz.Height);
			return size;
		}
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			// Set the region for the control
			var r = ClientSize.Width < ClientSize.Height * 2 ? ClientSize.Width / 8f : ClientSize.Height / 4f;
			Region = new Region(Gdi.RoundedRectanglePath(new RectangleF(0,0,8*r,4*r), 2*r));
		}
		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			// Swallow
		}
		protected override void OnPaint(PaintEventArgs args)
		{
			// Width/Height = 2
			var cx = ClientSize.Width - Padding.Left - Padding.Right;
			var cy = ClientSize.Height - Padding.Top - Padding.Bottom;

			var x = Padding.Left;
			var y = Padding.Top;
			var r = cx < cy * 2 ? cx / 8f : cy / 4f;

			var bk_col = Checked ? CheckedColor : UncheckedColor;
			if (Hovered) bk_col = bk_col.Lighten(0.2f);
			if (Pressed) bk_col = bk_col.Darken(0.4f);

			var fr_col = ThumbColor;
			if (Pressed) bk_col = bk_col.Darken(0.4f);
				
			args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			args.Graphics.Clear(BackColor);

			using (var bsh_back = new SolidBrush(bk_col))
			using (var bsh_fore0 = new SolidBrush(fr_col.Darken(0.05f)))
			using (var bsh_fore1 = new SolidBrush(fr_col))
			{
				// Draw the track
				args.Graphics.FillRectangleRounded(bsh_back, new RectangleF(x + 0*r, y + 0*r, 8*r, 4*r), 2*r);

				// Draw the thumb
				var p = Checked ? 5 : 1;
				args.Graphics.FillEllipse(bsh_fore0, new RectangleF(x + p*r, y + 1*r, 2*r, 2*r).Inflated(r*0.2f));
				args.Graphics.FillEllipse(bsh_fore1, new RectangleF(x + p*r, y + 1*r, 2*r, 2*r));
			}
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			Pressed = true;
			Invalidate();
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			Pressed = false;
			Invalidate();
		}
		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			Hovered = true;
			Invalidate();
		}
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			Hovered = false;
			Invalidate();
		}
		protected override void OnClick(EventArgs e)
		{
			base.OnClick(e);
			if (AutoCheck) Checked = !Checked;
		}
	}
}
