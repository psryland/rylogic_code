using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.TextFormatting;
using System.Windows.Shapes;
using System.Windows.Threading;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Extn.Windows;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.TextEditor
{

	public class TextView :FrameworkElement, IScrollInfo
	{
		// Notes:
		//  - 'TextView' handles all rendering.
		//  - Mapping Ypos (in DIP) to a document line requires generating visual lines
		//    for the whole document. 

		static TextView()
		{
			ClipToBoundsProperty.OverrideMetadata(typeof(TextView), new FrameworkPropertyMetadata(Boxed.True));
			FocusableProperty.OverrideMetadata(typeof(TextView), new FrameworkPropertyMetadata(Boxed.False));
		}
		public TextView()
			: this(new OptionsData())
		{ }
		public TextView(OptionsData options)
		{
			Options = options;
			Layers = new LayerCollection(this);
			TextLayer = Layers.Add(new Layer_Text(this));
		}

		/// <summary>UUID for this text view instance</summary>
		private Guid InstanceId { get; } = Guid.NewGuid();

		/// <summary>Text editor options</summary>
		public OptionsData Options { get; }

		/// <summary>Paragraph styles</summary>
		internal ParaStyle ParaStyle => new ParaStyle();

		/// <summary>Gets/Sets the document displayed by the text editor.</summary>
		public TextDocument? Document
		{
			get => (TextDocument)GetValue(DocumentProperty);
			set => SetValue(DocumentProperty, value);
		}
		private void Document_Changed(TextDocument? new_value, TextDocument? old_value)
		{
			if (old_value != null)
			{
				Formatter = null;
			}
			if (new_value != null)
			{
				Formatter = TextFormatter.Create(TextOptions.GetTextFormattingMode(this));

				// DefaultLineHeight depends on the formatter
				InvalidateDefaultTextMetrics();
			}
			ClearVisualLines();
			ScrollOffset = new Vector(0, 0);
			InvalidateMeasure(DispatcherPriority.Normal);
			DocumentChanged?.Invoke(this, EventArgs.Empty);
		}
		public static readonly DependencyProperty DocumentProperty = Gui_.DPRegister<TextView>(nameof(Document));

		/// <summary>Raised when the document property has changed.</summary>
		public event EventHandler? DocumentChanged;

		/// <summary></summary>
		public TextFormatter? Formatter
		{
			get => m_formatter;
			set
			{
				if (m_formatter == value) return;
				Util.Dispose(ref m_formatter);
				m_formatter = value;
			}
		}
		private TextFormatter? m_formatter;

		/// <summary>Returns the first visible document line for the given scroll Y position</summary>
		public Line? FirstVisibleLine(double Y)
		{
			if (Document == null)
				throw new InvalidOperationException("Cannot find the first visible line when there is no Document");

			// Special case when visual height data has not been calculated yet.
			if (Meta(Document.Root).VisualHeight == 0.0)
				return Document.Root.LeftMost;

			// Binary search for the line that spans 'Y'
			for (var line = Document.Root; line != null; )
			{
				var meta = Meta(line);
				if (Y >= meta.YPos && Y < meta.YPos + meta.VisualHeight)
					return line;

				if (Y < meta.YPos)
					line = line.m_lhs;
				else
					line = line.m_rhs;
			}

			//// Binary search for the line that spans 'Y'
			//for (var line = Document.Root; line != null;)
			//{
			//	var height = Meta(line).SubTreeVisualHeight;
			//	var lhs_height = line.m_lhs?.SubTreeVisualHeight ?? 0.0;

			//	if (Y < lhs_height)
			//	{
			//		line = line.m_lhs;
			//	}
			//	else if (Y > height - lhs_height)
			//	{
			//		line = line.m_rhs;
			//		Y -= height - lhs_height;
			//	}
			//	else
			//	{
			//		return line;
			//	}
			//}
			return null;
		}

		/// <summary>Gets the visual position from a text view position.</summary>
		public Point VisualPosition(TextLocation location, VisualYPosition mode)
		{
			VerifyAccess();
			if (Document == null)
				throw new Exception("No document has been set");

			var doc_line = Document.LineByIndex(location.LineIndex);
			var vis_line = VisualLine(doc_line);
			return vis_line.VisualPosition(location.Column, mode);
		}

		/// <summary>Get the visual line associated with a document line</summary>
		internal VisualLine VisualLine(Line doc_line)
		{
			// Find the range of document lines spanned by a single visual line (e.g. folded lines) (todo).
			var line0 = doc_line;
			var line1 = doc_line;

			// Get the meta data for the first line in the range
			var meta = Meta(line0);
			var vis_line = meta.VisualLine;

			// Create the visual line if missing or out of date
			if (vis_line == null || vis_line.FirstLine != line0 || vis_line.LastLine != line1)
			{
				// Create the visual line, and assign it to each document line it represents
				vis_line = new VisualLine(this, line0, line1);
				foreach (var line in vis_line.DocumentLines)
					Meta(line).VisualLine = vis_line;
			}

			return vis_line;
		}

		/// <summary>Return the meta data for 'doc_line' associated with this TextView instance</summary>
		private LineMetaData Meta(Line doc_line)
		{
			if (doc_line.UserData[InstanceId] is LineMetaData meta)
				return meta;

			meta = new LineMetaData();
			doc_line.UserData[InstanceId] = meta;
			return meta;
		}
		private class LineMetaData
		{
			/// <summary>The visual line used to render the associated document line</summary>
			public VisualLine? VisualLine { get; set; }

			/// <summary>The height (in DIP) of the visual line</summary>
			public double VisualHeight => VisualLine?.LineHeight ?? 0.0;

			/// <summary>The Y position of the visual line</summary>
			public double YPos => VisualLine?.YPos ?? 0.0;
		}

		#region Mouse

		/// <inheritdoc/>
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
		}

		#endregion

		#region Text Metrics

		/// <summary></summary>
		private TextMetricsData? m_text_metrics;
		private struct TextMetricsData
		{
			/// <summary>Width of an 'x'. Used as basis for the tab width, and for scrolling.</summary>
			public double WideSpaceWidth;

			/// <summary>Height of a line containing 'x'. Used for scrolling.</summary>
			public double DefaultLineHeight;

			/// <summary>Baseline of a line containing 'x'. Used for TextTop/TextBottom calculation.</summary>
			public double DefaultBaseline;
		}

		/// <summary>Gets the width of a 'wide space' (the space width used for calculating the tab size).</summary>
		/// <remarks>This is the width of an 'x' in the current font.
		/// We do not measure the width of an actual space as that would lead to tiny tabs in some proportional fonts.
		/// For monospaced fonts, this property will return the expected value, as 'x' and ' ' have the same width.</remarks>
		public double WideSpaceWidth => (m_text_metrics ??= CalculateDefaultTextMetrics()).WideSpaceWidth;

		/// <summary>
		/// Gets the default line height. This is the height of an empty line or a line containing regular text.
		/// Lines that include formatted text or custom UI elements may have a different line height.</summary>
		public double DefaultLineHeight => (m_text_metrics ??= CalculateDefaultTextMetrics()).DefaultLineHeight;

		/// <summary>
		/// Gets the default baseline position. This is the difference between <see cref="VisualYPosition.TextTop"/>
		/// and <see cref="VisualYPosition.Baseline"/> for a line containing regular text.
		/// /// Lines that include formatted text or custom UI elements may have a different baseline.</summary>
		public double DefaultBaseline => (m_text_metrics ??= CalculateDefaultTextMetrics()).DefaultBaseline;

		/// <summary>Recalculate the default text metrics</summary>
		private void InvalidateDefaultTextMetrics()
		{
			m_text_metrics = null;

			// Update the visual heights if DefaultLineHeight has changed
			Document?.UpdateTree();
		}

		/// <summary>Return text metrics</summary>
		private TextMetricsData CalculateDefaultTextMetrics()
		{
			var data = new TextMetricsData();

			if (Formatter != null)
			{
				var text_style = Document?.TextStyles[0] ?? TextStyle.Default;
				var text = new SimpleTextSource("x", text_style);
				using var line = Formatter.FormatLine(text, 0, 32000, ParaStyle, null);
				data.WideSpaceWidth = Math.Max(1, line.WidthIncludingTrailingWhitespace);
				data.DefaultBaseline = Math.Max(1, line.Baseline);
				data.DefaultLineHeight = Math.Max(1, line.Height);
			}
			else
			{
				data.WideSpaceWidth = this.FontSize() / 2;
				data.DefaultBaseline = this.FontSize();
				data.DefaultLineHeight = this.FontSize() + 3;
			}

			return data;
		}

		#endregion

		#region IScrollInfo

		/// <summary>The scroll viewer instance handling the scrolling</summary>
		public ScrollViewer? ScrollOwner { get; set; }

		/// <summary>Size of the document, in pixels.</summary>
		private Size ScrollExtent;

		/// <summary>Size of the viewport.</summary>
		private Size ScrollViewport;

		/// <summary>Gets the horizontal scroll offset.</summary>
		public double HorizontalOffset
		{
			get => m_scroll_offset.X;
			set => ScrollOffset = new Vector(value, m_scroll_offset.Y);
		}

		/// <summary>Gets the vertical scroll offset.</summary>
		public double VerticalOffset
		{
			get => m_scroll_offset.Y;
			set => ScrollOffset = new Vector(m_scroll_offset.X, value);
		}

		/// <summary>Gets the scroll offset;</summary>
		public Vector ScrollOffset
		{
			get => m_scroll_offset;
			set
			{
				if (double.IsNaN(value.X) || double.IsNaN(value.Y))
					throw new Exception("Offset must not be NaN");

				// Validate the scroll position
				value.X = Math.Max(value.X, 0);
				value.Y = Math.Max(value.Y, 0);
				if (!m_can_scrollH) value.X = 0;
				if (!m_can_scrollV) value.Y = 0;
				if (value.X + ScrollViewport.Width > ScrollExtent.Width)
					value.X = Math.Max(0, ScrollExtent.Width - ScrollViewport.Width);
				if (value.Y + ScrollViewport.Height > ScrollExtent.Height)
					value.Y = Math.Max(0, ScrollExtent.Height - ScrollViewport.Height);

				// Only notify if the scroll offset has changed
				if (Vector_.FEql(m_scroll_offset, value))
					return;

				m_scroll_offset = value;
				ScrollOwner?.InvalidateScrollInfo();
				ScrollOffsetChanged?.Invoke(this, EventArgs.Empty);
				InvalidateMeasure(DispatcherPriority.Normal);
			}
		}
		private Vector m_scroll_offset;

		/// <summary>Scrolls the text view so that the specified rectangle (in pixels) becomes visible.</summary>
		public void MakeVisible(Rect area)
		{
			var view = new Rect(m_scroll_offset.X, m_scroll_offset.Y, ScrollViewport.Width, ScrollViewport.Height);
			var new_offset = m_scroll_offset;

			// Move the left edge into view
			if (area.Left < view.Left)
			{
				new_offset.X = area.Right > view.Right ? area.Left + area.Width / 2 : area.Left;
			}
			else if (area.Right > view.Right)
			{
				new_offset.X = area.Right - ScrollViewport.Width;
			}

			// Move the top edge into view
			if (area.Top < view.Top)
			{
				new_offset.Y = area.Bottom > view.Bottom ? area.Top + area.Height / 2 : area.Top;
			}
			else if (area.Bottom > view.Bottom)
			{
				new_offset.Y = area.Bottom - ScrollViewport.Height;
			}

			// Scroll to the new offset
			ScrollOffset = new_offset;
		}
		Rect IScrollInfo.MakeVisible(Visual visual, Rect rectangle)
		{
			if (rectangle.IsEmpty || visual == null || visual == this || !IsAncestorOf(visual))
				return Rect.Empty;

			// Convert rectangle into our coordinate space.
			var c2p = visual.TransformToAncestor(this);
			rectangle = c2p.TransformBounds(rectangle);
			MakeVisible(Rect.Offset(rectangle, m_scroll_offset));
			return rectangle;
		}

		/// <summary>Occurs when the scroll offset has changed.</summary>
		public event EventHandler? ScrollOffsetChanged;

		/// <inheritdoc/>
		bool IScrollInfo.CanVerticallyScroll
		{
			get => m_can_scrollV;
			set
			{
				if (m_can_scrollV == value) return;
				m_can_scrollV = value;
				InvalidateMeasure(DispatcherPriority.Normal);
			}
		}
		private bool m_can_scrollV;

		/// <inheritdoc/>
		bool IScrollInfo.CanHorizontallyScroll
		{
			get => m_can_scrollH;
			set
			{
				if (m_can_scrollH == value) return;
				m_can_scrollH = value;
				InvalidateMeasure(DispatcherPriority.Normal);
				//ClearVisualLines();
			}
		}
		private bool m_can_scrollH;

		/// <inheritdoc/>
		void IScrollInfo.SetHorizontalOffset(double offset) => HorizontalOffset = offset;
		void IScrollInfo.SetVerticalOffset(double offset) => VerticalOffset = offset;
		double IScrollInfo.ExtentWidth => ScrollExtent.Width;
		double IScrollInfo.ExtentHeight => ScrollExtent.Height;
		double IScrollInfo.ViewportWidth => ScrollViewport.Width;
		double IScrollInfo.ViewportHeight => ScrollViewport.Height;

		/// <inheritdoc/>
		void IScrollInfo.LineUp() => VerticalOffset = m_scroll_offset.Y - DefaultLineHeight;
		void IScrollInfo.LineDown() => VerticalOffset = m_scroll_offset.Y + DefaultLineHeight;
		void IScrollInfo.PageUp() => VerticalOffset = m_scroll_offset.Y - ScrollViewport.Height;
		void IScrollInfo.PageDown() => VerticalOffset = m_scroll_offset.Y + ScrollViewport.Height;
		void IScrollInfo.LineLeft() => HorizontalOffset = m_scroll_offset.X - WideSpaceWidth;
		void IScrollInfo.LineRight() => HorizontalOffset = m_scroll_offset.X + WideSpaceWidth;
		void IScrollInfo.PageLeft() => HorizontalOffset = m_scroll_offset.X - ScrollViewport.Width;
		void IScrollInfo.PageRight() => HorizontalOffset = m_scroll_offset.X + ScrollViewport.Width;
		void IScrollInfo.MouseWheelUp() => VerticalOffset = m_scroll_offset.Y - (SystemParameters.WheelScrollLines * DefaultLineHeight);
		void IScrollInfo.MouseWheelDown() => VerticalOffset = m_scroll_offset.Y + (SystemParameters.WheelScrollLines * DefaultLineHeight);
		void IScrollInfo.MouseWheelLeft() => HorizontalOffset = m_scroll_offset.X - (SystemParameters.WheelScrollLines * WideSpaceWidth);
		void IScrollInfo.MouseWheelRight() => HorizontalOffset = m_scroll_offset.X + (SystemParameters.WheelScrollLines * WideSpaceWidth);

		#endregion

		#region Layers

		/// <summary>Layers for the view</summary>
		public LayerCollection Layers { get; }
		public sealed class LayerCollection :IEnumerable<Layer>
		{
			private UIElementCollection m_layers;
			internal LayerCollection(TextView text_view)
			{
				m_layers = new UIElementCollection(text_view, text_view);
			}
			public int Count => m_layers.Count;
			public Layer this[int idx] => (Layer)m_layers[idx];
			public T Add<T>(T layer, bool replace = true) where T : Layer
			{
				int i = 0;
				for (; i != m_layers.Count; ++i)
				{
					if (!(m_layers[i] is Layer l)) continue;
					if (l.Type <= layer.Type) continue;
					break;
				}
				for (; replace && i > 0; --i)
				{
					if (!(m_layers[i - 1] is Layer l)) break;
					if (l.Type != layer.Type) break;
					m_layers.RemoveAt(i - 1);
				}
				m_layers.Insert(i, layer);
				return layer;
			}
			public T Remove<T>(T layer) where T : Layer
			{
				m_layers.Remove(layer);
				return layer;
			}
			public IEnumerator<Layer> GetEnumerator() => m_layers.Cast<Layer>().GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

		/// <summary>The layer containing the visual lines of text</summary>
		internal Layer_Text TextLayer { get; }

		/// <inheritdoc/>
		protected override int VisualChildrenCount => Layers.Count;

		/// <inheritdoc/>
		protected override Visual GetVisualChild(int index) => Layers[index];

		#endregion

		#region Measure/Arrange

		// Notes:
		//  - Arrange occurs after Measure. It's used to layout children
		//    once they have calculated their desired size.

		/// <summary>Remove all visual lines</summary>
		private void ClearVisualLines()
		{
			// Callers should call InvalidateMeasure() to recreate visual lines.
			if (Document == null) return;
			//m_vis_line_cache.Flush();
		}

		/// <inheritdoc/>
		protected override Size MeasureOverride(Size constraint)
		{
			// No doc, return default
			if (!(Document is TextDocument doc))
				return base.MeasureOverride(constraint);

			var size = constraint;

			// Measure each layer
			foreach (var layer in Layers)
			{
				layer.Measure(constraint);
				size = new Size(
					Math.Min(constraint.Width, layer.DesiredSize.Width),
					Math.Min(constraint.Height, layer.DesiredSize.Height));
			}

			// Force an arrange
			InvalidateArrange();

			// Record the bounds of the scroll area
			ScrollViewport = constraint;
			ScrollExtent = new Size(size.Width, 100);//hack VisualLine(doc.Root).SubTreeVisualHeight);

			// Return the desired size
			return size;
		}

		/// <inheritdoc/>
		protected override Size ArrangeOverride(Size size)
		{
			// Arrange each layer
			foreach (var layer in Layers)
			{
				layer.Arrange(new Rect(new Point(), size));
			}
#if false
			InvalidateCursor();

			EnsureVisualLines();
			foreach (UIElement layer in layers)
			{
				layer.Arrange(new Rect(new Point(0, 0), finalSize));
			}

			if (document == null || allVisualLines.Count == 0)
				return finalSize;

			//Debug.WriteLine("Arrange finalSize=" + finalSize + ", scrollOffset=" + scrollOffset);

			//			double maxWidth = 0;


#endif
			return size;
		}

		/// <summary>Call invalidate measure with a priority</summary>
		private void InvalidateMeasure(DispatcherPriority priority)
		{
			// High priority, invalidate immediately
			if (priority >= DispatcherPriority.Render)
			{
				m_invalidate_measure_operation?.Abort();
				m_invalidate_measure_operation = null;
				base.InvalidateMeasure();
				return;
			}

			// Invoke in progress, change the priority
			if (m_invalidate_measure_operation != null)
			{
				m_invalidate_measure_operation.Priority = priority;
				return;
			}

			// Invoke an invalid at the given priority
			m_invalidate_measure_operation = Dispatcher.BeginInvoke(priority, new Action(() =>
			{
				m_invalidate_measure_operation = null;
				base.InvalidateMeasure();
			}));
		}
		private DispatcherOperation? m_invalidate_measure_operation;

		/// <summary>
		/// Additonal amount that allows horizontal scrolling past the end of the longest line.
		/// This is necessary to ensure the caret always is visible, even when it is at the end of the longest line.</summary>
		private const double AdditionalHorizontalScrollAmount = 3;

		#endregion

		#region Rendering

		/// <inheritdoc/>
		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);
			RenderBackground(dc, Layer.EType.Background);

			// If there is no document, don't render
			if (!(Document is TextDocument doc))
				return;

			// Render Padding

			// Render the text lines
			//var line_origin = new Point();
			//foreach (var line in doc.Lines)
			//{
			//	foreach (var ft in line.FormattedText(doc.Styles))
			//	{
			//		dc.DrawText(ft, line_origin);
			//		line_origin.X += ft.WidthIncludingTrailingWhitespace;
			//	}
			//	line_origin.X = 0;
			//	line_origin.Y += line.Height;
			//}
		}

		/// <summary></summary>
		internal void RenderBackground(DrawingContext dc, Layer.EType layer)
		{
			//foreach (IBackgroundRenderer bg in backgroundRenderers)
			//{
			//	if (bg.Layer == layer)
			//	{
			//		bg.Draw(this, drawingContext);
			//	}
			//}
		}

		/// <summary>Causes the text editor to regenerate all visual lines</summary>
		public void Redraw()
		{
			Redraw(DispatcherPriority.Normal);
		}

		/// <summary>Causes the text editor to regenerate all visual lines</summary>
		public void Redraw(DispatcherPriority priority)
		{
			VerifyAccess();
			ClearVisualLines();
			InvalidateMeasure(priority);
		}

		/// <summary>Redraws all lines intersecting with 'segment'.</summary>
		public void Redraw(ISegment segment, DispatcherPriority priority = DispatcherPriority.Normal)
		{
			if (segment == Segment_.Invalid) return;
			Redraw(segment.BegOffset, segment.Length, priority);
		}

		/// <summary>Redraws all lines intersecting the given text range</summary>
		public void Redraw(int offset, int length, DispatcherPriority priority = DispatcherPriority.Normal)
		{
			//VerifyAccess();
			//bool changedSomethingBeforeOrInLine = false;
			//for (int i = 0; i < allVisualLines.Count; i++)
			//{
			//	VisualLine visualLine = allVisualLines[i];
			//	int lineStart = visualLine.FirstDocumentLine.Offset;
			//	int lineEnd = visualLine.LastDocumentLine.Offset + visualLine.LastDocumentLine.TotalLength;
			//	if (offset <= lineEnd)
			//	{
			//		changedSomethingBeforeOrInLine = true;
			//		if (offset + length >= lineStart)
			//		{
			//			allVisualLines.RemoveAt(i--);
			//			DisposeVisualLine(visualLine);
			//		}
			//	}
			//}
			//if (changedSomethingBeforeOrInLine)
			//{
			//	// Repaint not only when something in visible area was changed, but also when anything in front of it
			//	// was changed. We might have to redraw the line number margin. Or the highlighting changed.
			//	// However, we'll try to reuse the existing VisualLines.
			//	InvalidateMeasure(priority);
			//}
		}

		/// <summary>Redraws the given visual line.</summary>
		public void Redraw(VisualLine visualLine, DispatcherPriority redrawPriority = DispatcherPriority.Normal)
		{
			VerifyAccess();
			//if (allVisualLines.Remove(visualLine))
			//{
			//	DisposeVisualLine(visualLine);
			//	InvalidateMeasure(redrawPriority);
			//}
		}

		/// <summary>True during 'Measure'</summary>
		private bool InMeasure => m_in_measure != 0;
		private IDisposable MeasureScope()
		{
			return Scope.Create(
				() => ++m_in_measure,
				() => --m_in_measure);
		}
		private int m_in_measure;

		#endregion
	}
}

