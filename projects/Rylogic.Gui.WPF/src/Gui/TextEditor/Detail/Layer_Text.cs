using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Rylogic.Gui.WPF.TextEditor
{
	internal sealed class Layer_Text :Layer
	{
		public Layer_Text(TextView text_view)
			: base(EType.Text, text_view)
		{}

		/// <summary>The offset (in DIP) of the first line relative to the top of the display area (typically negative)</summary>
		public double PartialLineOffset { get; private set; }

		/// <summary>
		/// Additonal amount that allows horizontal scrolling past the end of the longest line.
		/// This is necessary to ensure the caret always is visible, even when it is at the end of the longest line.</summary>
		private const double AdditionalHorizontalScrollAmount = 3;

		/// <inheritdoc/>
		protected override Size MeasureCore(Size constraint)
		{
			// Remove all visual children
			Visuals.Clear();

			// Measure the visible lines, starting at the first visible line from the scroll position
			double ypos = 0.0, xwidth = 0.0;
			var partial_offset = (double?)null;
			var ofs = TextView.ScrollOffset;

			for (var doc_line = TextView.FirstVisibleLine(ofs.Y); doc_line != null; doc_line = doc_line.NextLine)
			{
				// Get/Create the visual line
				var vis_line = TextView.VisualLine(doc_line);
				vis_line.EnsureTextLines(constraint);
				vis_line.YPos = ofs.Y + ypos;

				// Record the partial offset of the first line
				partial_offset ??= vis_line.YPos - ofs.Y;// todo

				// Record the width of the line
				xwidth = Math.Max(xwidth, vis_line.LineWidth);
				
				// Increment the y position by the line height
				ypos += vis_line.LineHeight;

				// Add the line as a visual child of the view
				Visuals.Add(vis_line.Graphics);
			}
			InvalidateArrange();

			// Record the partial line offset
			PartialLineOffset = partial_offset ?? 0.0;

			// Grow the document area a bit
			const bool AllowScrollBelowDocument = false; //todo: option
			xwidth += AdditionalHorizontalScrollAmount;
			ypos += AllowScrollBelowDocument && !double.IsInfinity(constraint.Height) ? Math.Min(0, constraint.Height - TextView.DefaultLineHeight) : 0.0;

			// Return the desired size
			return new Size(
				Math.Min(constraint.Width, xwidth),
				Math.Min(constraint.Height, ypos));
		}

		/// <inheritdoc/>
		protected override void ArrangeCore(Rect finalRect)
		{
			// Position the visual lines
			var ofs = TextView.ScrollOffset;
			var pos = new Point(-ofs.X, -PartialLineOffset);
			foreach (var visual in Visuals.OfType<VisualLine.Gfx>())
			{
				if (!(visual.Transform is TranslateTransform t) || t.X != pos.X || t.Y != pos.Y)
				{
					visual.Transform = new TranslateTransform(pos.X, pos.Y);
					visual.Transform.Freeze();
				}
				pos.Y += visual.Height;
			}
		}
	}
}

