using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;
using Rylogic.Extn.Windows;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.TextEditor
{
	public class TextView :FrameworkElement, IScrollInfo
	{
		// Notes:
		// - This control is the text view and handles all UI functionality

		static TextView()
		{
			ClipToBoundsProperty.OverrideMetadata(typeof(TextView), new FrameworkPropertyMetadata(Boxed.True));
			FocusableProperty.OverrideMetadata(typeof(TextView), new FrameworkPropertyMetadata(Boxed.False));
		}
		public TextView()
		{ }

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
				//cachedElements.Dispose();
				//cachedElements = null;
				//TextDocumentWeakEventManager.Changing.RemoveListener(oldValue, this);
			}
			//this.document = newValue;
			//ClearScrollData();
			//ClearVisualLines();
			if (new_value != null)
			{
				//TextDocumentWeakEventManager.Changing.AddListener(newValue, this);
				//formatter = TextFormatterFactory.Create(this);
				InvalidateDefaultTextMetrics(); // measuring DefaultLineHeight depends on formatter
				//heightTree = new HeightTree(newValue, DefaultLineHeight);
				//cachedElements = new TextViewCachedElements();
			}
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

		/// <summary>Gets the visual position from a text view position.</summary>
		/// <param name="position">The text view position.</param>
		/// <param name="mode">The mode how to retrieve the Y position.</param>
		/// <returns>The position in WPF device-independent pixels relative to the top left corner of the document.</returns>
		public Point GetVisualPosition(Location position, VisualYPosition mode = VisualYPosition.LineTop)
		{
			throw new NotImplementedException();
		//	// Don't allow cross thread calls
		//	VerifyAccess();
		//
		//	if (Document == null)
		//		throw new Exception("No document has been set");
		//
		//	var documentLine = Document.Lines[position.Line];
		//	var visualLine = GetOrConstructVisualLine(documentLine);
		//	int visualColumn = position.VisualColumn;
		//	if (visualColumn < 0)
		//	{
		//		int offset = documentLine.Offset + position.Column - 1;
		//		visualColumn = visualLine.GetVisualColumn(offset - visualLine.FirstDocumentLine.Offset);
		//	}
		//	return visualLine.GetVisualPosition(visualColumn, mode);
		}

		/// <inheritdoc/>
		protected override Size MeasureOverride(Size constraint)
		{
			var sz = base.MeasureOverride(constraint);
			if (Document is TextDocument doc)
				sz.Height = doc.Lines.Sum(x => x.Height);
			return sz;
		}

		/// <inheritdoc/>
		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);

			// If there is no document, don't render
			if (!(Document is TextDocument doc))
				return;

			// Render Padding

			// Render the text lines
			var line_origin = new Point();
			foreach (var line in doc.Lines)
			{
				foreach (var ft in line.FormattedText(doc.Styles))
				{
					dc.DrawText(ft, line_origin);
					line_origin.X += ft.WidthIncludingTrailingWhitespace;
				}
				line_origin.X = 0;
				line_origin.Y += line.Height;
			}
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

		/// <summary></summary>
		private void InvalidateDefaultTextMetrics()
		{
			m_text_metrics = null;
			//if (heightTree != null)
			//{
			//	// calculate immediately so that height tree gets updated
			//	CalculateDefaultTextMetrics();
			//}
		}

		/// <summary></summary>
		private TextMetricsData CalculateDefaultTextMetrics()
		{
			var data = new TextMetricsData();

			if (m_formatter != null)
			{
				var props = new GlobalTextRunProperties(this);
				var text = new SimpleTextSource("x", props);
				var para = new VisualLineTextParagraphProperties(props);
				using var line = m_formatter.FormatLine(text, 0, 32000, para, null);
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
			// Update heightTree.DefaultLineHeight, if a document is loaded.
			///if (heightTree != null)
			///	heightTree.DefaultLineHeight = defaultLineHeight;
			return data;
		}
		private struct TextMetricsData
		{
			/// <summary>Width of an 'x'. Used as basis for the tab width, and for scrolling.</summary>
			public double WideSpaceWidth;

			/// <summary>Height of a line containing 'x'. Used for scrolling.</summary>
			public double DefaultLineHeight;

			/// <summary>Baseline of a line containing 'x'. Used for TextTop/TextBottom calculation.</summary>
			public double DefaultBaseline;
		}

		#endregion

		#region IScrollInfo

		/// <summary>The scroll viewer instance handling the scrolling</summary>
		public ScrollViewer? ScrollOwner { get; set; }

		/// <summary>Size of the document, in pixels.</summary>
		private Size m_scroll_extent;

		/// <summary>Size of the viewport.</summary>
		private Size m_scroll_viewport;

		/// <summary>Offset of the scroll position.</summary>
		private Vector m_scroll_offset;

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

				value.X = Math.Max(value.X, 0);
				value.Y = Math.Max(value.Y, 0);
				if (!m_can_scrollH) value.X = 0;
				if (!m_can_scrollV) value.Y = 0;
				if (Vector_.FEql(m_scroll_offset, value))
					return;

				m_scroll_offset = value;
				ScrollOwner?.InvalidateScrollInfo();
				ScrollOffsetChanged?.Invoke(this, EventArgs.Empty);
				InvalidateMeasure(DispatcherPriority.Normal);
			}
		}

		/// <summary>Scrolls the text view so that the specified rectangle (in pixels) becomes visible.</summary>
		public void MakeVisible(Rect area)
		{
			var view = new Rect(m_scroll_offset.X, m_scroll_offset.Y, m_scroll_viewport.Width, m_scroll_viewport.Height);
			var new_offset = m_scroll_offset;
			
			// Move the left edge into view
			if (area.Left < view.Left)
			{
				new_offset.X = area.Right > view.Right ? area.Left + area.Width / 2 : area.Left;
			}
			else if (area.Right > view.Right)
			{
				new_offset.X = area.Right - m_scroll_viewport.Width;
			}

			// Move the top edge into view
			if (area.Top < view.Top)
			{
				new_offset.Y = area.Bottom > view.Bottom ? area.Top + area.Height / 2 : area.Top;
			}
			else if (area.Bottom > view.Bottom)
			{
				new_offset.Y = area.Bottom - m_scroll_viewport.Height;
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
		double IScrollInfo.ExtentWidth => m_scroll_extent.Width;
		double IScrollInfo.ExtentHeight => m_scroll_extent.Height;
		double IScrollInfo.ViewportWidth => m_scroll_viewport.Width;
		double IScrollInfo.ViewportHeight => m_scroll_viewport.Height;

		/// <inheritdoc/>
		void IScrollInfo.LineUp() => VerticalOffset = m_scroll_offset.Y - DefaultLineHeight;
		void IScrollInfo.LineDown() => VerticalOffset = m_scroll_offset.Y + DefaultLineHeight;
		void IScrollInfo.PageUp() => VerticalOffset = m_scroll_offset.Y - m_scroll_viewport.Height;
		void IScrollInfo.PageDown() => VerticalOffset = m_scroll_offset.Y + m_scroll_viewport.Height;
		void IScrollInfo.LineLeft() => HorizontalOffset = m_scroll_offset.X - WideSpaceWidth;
		void IScrollInfo.LineRight() => HorizontalOffset = m_scroll_offset.X + WideSpaceWidth;
		void IScrollInfo.PageLeft() => HorizontalOffset = m_scroll_offset.X - m_scroll_viewport.Width;
		void IScrollInfo.PageRight() => HorizontalOffset = m_scroll_offset.X + m_scroll_viewport.Width;
		void IScrollInfo.MouseWheelUp() => VerticalOffset = m_scroll_offset.Y - (SystemParameters.WheelScrollLines * DefaultLineHeight);
		void IScrollInfo.MouseWheelDown() => VerticalOffset = m_scroll_offset.Y + (SystemParameters.WheelScrollLines * DefaultLineHeight);
		void IScrollInfo.MouseWheelLeft() => HorizontalOffset = m_scroll_offset.X - (SystemParameters.WheelScrollLines * WideSpaceWidth);
		void IScrollInfo.MouseWheelRight() => HorizontalOffset = m_scroll_offset.X + (SystemParameters.WheelScrollLines * WideSpaceWidth);

		#endregion

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
	}
}

