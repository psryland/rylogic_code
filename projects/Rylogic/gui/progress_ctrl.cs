using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using pr.maths;
using pr.util;
using pr.win32;

namespace pr.gui
{
	/// <summary>Progress bar that allows text overlay</summary>
	public class TextProgressBar :ProgressBar
	{
		public TextProgressBar()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
			ForeColor = Color.Black;
			TextL = new TextZone(this, string.Empty, Font);
			TextC = new TextZone(this, string.Empty, Font);
			TextR = new TextZone(this, string.Empty, Font);
		}
		protected override void OnHandleCreated(EventArgs e)
		{
			Win32.SetWindowTheme(Handle, string.Empty, string.Empty);
			base.OnHandleCreated(e);
		}

		/// <summary>Regions of text in the progress bar</summary>
		[TypeConverter(typeof(TextZone.TyConv))]
		public class TextZone
		{
			private TextProgressBar m_pb;

			public TextZone() {}
			internal TextZone(TextProgressBar pb, string text, Font font)
			{
				m_pb = pb;
				m_text = text;
				m_font = font;
			}

			/// <summary>Text to display</summary>
			public string Text
			{
				get { return m_text; }
				set { m_text = value; m_pb?.Invalidate(); }
			}
			private string m_text;

			/// <summary>Font for Text</summary>
			public Font Font
			{
				get { return m_font; }
				set { m_font = value; m_pb?.Invalidate(); }
			}
			private Font m_font;

			[Editor("TextZone", typeof(TyConv))]
			private class TyConv :GenericTypeConverter<TextZone> {}
		}

		/// <summary>Text aligned to the near side (Left in LTR mode)</summary>
		public TextZone TextL { get; private set; }

		/// <summary>Text aligned to the centre</summary>
		public TextZone TextC { get; private set; }

		/// <summary>Text aligned to the far side (Right in LTR mode)</summary>
		public TextZone TextR { get; private set; }

		/// <summary>Standard control text maps to 'TextC'</summary>
		[Browsable(true)]
		public override string Text
		{
			get { return base.Text; }
			set
			{
				if (Text == value) return;
				base.Text = TextC.Text = value;
			}
		}

		/// <summary>Standard control font maps to 'TextC'</summary>
		[Browsable(true)]
		public override Font Font
		{
			get { return base.Font; }
			set
			{
				if (ReferenceEquals(TextL.Font, base.Font)) TextL.Font = value;
				if (ReferenceEquals(TextC.Font, base.Font)) TextC.Font = value;
				if (ReferenceEquals(TextR.Font, base.Font)) TextR.Font = value;
				base.Font = value;
			}
		}

		/// <summary>Paint the progress bar</summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			var gfx = e.Graphics;
			var rect = ClientRectangle;
			var visual_styles_enabled = ProgressBarRenderer.IsSupported;

			// Draw the background
			if (visual_styles_enabled)
			{
				ProgressBarRenderer.DrawHorizontalBar(gfx, rect);
			}
			else
			{
				gfx.FillRectangle(SystemBrushes.Control, rect);
				gfx.DrawRectangle(SystemPens.ControlDark, rect);
			}

			// Fill with chunks
			if (Value != Minimum)
			{
				const int pad = 3;
				var frac = Maths.Frac(Minimum, Value, Maximum);
				var clip = new Rectangle(rect.X + pad, rect.Y + pad, (int)(frac * (rect.Width - 2*pad)), rect.Height - 2*pad);
				if (visual_styles_enabled)
					ProgressBarRenderer.DrawHorizontalChunks(gfx, clip);
				else
					gfx.FillRectangle(SystemBrushes.Highlight, clip);
			}
			
			// Draw the text
			using (var b = new SolidBrush(ForeColor))
			{
				if (TextL.Text.HasValue())
				{
					var sz = gfx.MeasureString(TextL.Text, TextL.Font);
					gfx.DrawString(TextL.Text, TextL.Font, b, new PointF(rect.Left + (RightToLeftLayout ? rect.Width - sz.Width : 0), rect.Top + (rect.Height - sz.Height)/2f));
				}
				if (TextC.Text.HasValue())
				{
					var sz = gfx.MeasureString(TextC.Text, TextC.Font);
					gfx.DrawString(TextC.Text, TextC.Font, b, new PointF(rect.Left + (rect.Width - sz.Width)/2, rect.Top + (rect.Height - sz.Height)/2f));
				}
				if (TextR.Text.HasValue())
				{
					var sz = gfx.MeasureString(TextR.Text, TextR.Font);
					gfx.DrawString(TextR.Text, TextR.Font, b, new PointF(rect.Left + (RightToLeftLayout ? 0 : rect.Width - sz.Width), rect.Top + (rect.Height - sz.Height)/2f));
				}
			}
		}
	}
}