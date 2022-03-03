using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Rylogic.Gui.WPF.TextEditor;

namespace Rylogic.Gui.WPF
{
	public sealed class Caret
	{
		private readonly Layer_Caret m_layer;
		internal Caret(TextArea text_area)
		{
			TextArea = text_area;
			m_position = new TextViewPosition(1, 1, 0);
			m_layer = TextView.Layers.Add(new Layer_Caret(TextView, this));

#if false
			TextView.VisualLinesChanged += TextView_VisualLinesChanged;
			TextView.ScrollOffsetChanged += TextView_ScrollOffsetChanged;
#endif
		}

		/// <summary></summary>
		private TextArea TextArea { get; }

		/// <summary></summary>
		private TextView TextView => TextArea.TextView;

		/// <summary></summary>
		private TextDocument? Document => TextArea.Document;

		/// <summary>Get the document line that contains the caret</summary>
		public Line? DocumentLine => Document is TextDocument doc ? doc.LineByIndex(NonValidatedPosition.Line) : null;

		/// <summary>The visual line that contains the caret</summary>
		internal VisualLine? VisualLine => DocumentLine is Line line ? TextView.VisualLine(line) : null;

		/// <summary>Gets/Sets the color of the caret. (Set allows null)</summary>
		public Brush Colour
		{
			get => m_colour ?? (Brush)TextView.GetValue(TextBlock.ForegroundProperty);
			set => m_colour = value;
		}
		private Brush? m_colour;

		/// <summary>
		/// Gets/Sets the position of the caret.
		/// Retrieving this property will validate the visual column (which can be expensive).
		/// Use the <see cref="Location"/> property instead if you don't need the visual column.
		/// </summary>
		public TextViewPosition Position
		{
			get
			{
				m_position.VisualColumn ??= CalculateVisualColumn();
				return m_position;
			}
			set
			{
				if (m_position == value) return;
				m_position = value;


#if false
				storedCaretOffset = -1;
				ValidatePosition();
				InvalidateVisualColumn();
				RaisePositionChanged();
				if (Visible)
					Show();
#endif
			}
		}
		private TextViewPosition m_position;

		/// <summary>Gets the caret position without validating it.</summary>
		internal TextViewPosition NonValidatedPosition => m_position;

		/// <summary></summary>
		public bool Visible { get; set; }

		/// <summary>Gets whether the caret is in virtual space.</summary>
		public bool IsInVirtualSpace
		{
			get
			{
				//m_position.VisualColumn ??= CalculateVisualColumn();
				//if (TextArea.Document is TextDocument document)
				//{
				//	var doc_line = document.LineByIndex(m_position.Line);
				//	doc_line.VisualLine ??= new VisualLine(TextView, doc_line);
				//}
				//return m_position.VisualColumn > VisualLine?.VisualLength;
				return false; // todo
			}
		}

		/// <summary>Gets/Sets the desired x-position of the caret, in device-independent pixels. This property is NaN if the caret has no desired position.</summary>
		private double DesiredXPos { get; set; } = double.NaN;

		/// <summary>Minimum distance of the caret to the view border.</summary>
		internal const double MinimumDistanceToViewBorder = 30;


		/// <summary>Find the visual column by scanning the document line</summary>
		private int? CalculateVisualColumn()
		{
			if (TextArea.Document is TextDocument document)
			{
				var doc_line = document.LineByIndex(m_position.Line);
				var vis_line = TextView.VisualLine(doc_line);
				//RevalidateVisualColumn(TextView.GetOrConstructVisualLine(doc_line));
			}
			return null;
		}






#if false
		int storedCaretOffset;

		void TextView_VisualLinesChanged(object sender, EventArgs e)
		{
			if (visible)
			{
				Show();
			}

			// required because the visual columns might have changed if the
			// element generators did something differently than on the last run
			// (e.g. a FoldingSection was collapsed)
			InvalidateVisualColumn();
		}

		void TextView_ScrollOffsetChanged(object sender, EventArgs e)
		{
			if (m_layer != null)
			{
				m_layer.InvalidateVisual();
			}
		}

		/// <summary>Gets/Sets the location of the caret.
		/// The getter of this property is faster than <see cref="Position"/> because it doesn't have
		/// to validate the visual column.
		/// </summary>
		public TextLocation Location
		{
			get
			{
				return m_position.Location;
			}
			set
			{
				this.Position = new TextViewPosition(value);
			}
		}

		/// <summary>Gets/Sets the caret line.</summary>
		public int Line
		{
			get { return m_position.Line; }
			set
			{
				Position = new TextViewPosition(value, m_position.Column);
			}
		}

		/// <summary>
		/// Gets/Sets the caret column.
		/// </summary>
		public int Column
		{
			get { return m_position.Column; }
			set
			{
				this.Position = new TextViewPosition(m_position.Line, value);
			}
		}

		/// <summary>
		/// Gets/Sets the caret visual column.
		/// </summary>
		public int VisualColumn
		{
			get
			{
				ValidateVisualColumn();
				return m_position.VisualColumn;
			}
			set
			{
				this.Position = new TextViewPosition(m_position.Line, m_position.Column, value);
			}
		}



		internal void OnDocumentChanging()
		{
			storedCaretOffset = this.Offset;
			InvalidateVisualColumn();
		}

		internal void OnDocumentChanged(DocumentChangeEventArgs e)
		{
			InvalidateVisualColumn();
			if (storedCaretOffset >= 0)
			{
				int newCaretOffset = e.GetNewOffset(storedCaretOffset, AnchorMovementType.Default);
				TextDocument document = TextArea.Document;
				if (document != null)
				{
					// keep visual column
					this.Position = new TextViewPosition(document.GetLocation(newCaretOffset), m_position.VisualColumn);
				}
			}
			storedCaretOffset = -1;
		}

		/// <summary>
		/// Gets/Sets the caret offset.
		/// Setting the caret offset has the side effect of setting the <see cref="DesiredXPos"/> to NaN.
		/// </summary>
		public int Offset
		{
			get
			{
				TextDocument document = TextArea.Document;
				if (document == null)
				{
					return 0;
				}
				else
				{
					return document.GetOffset(m_position.Location);
				}
			}
			set
			{
				TextDocument document = TextArea.Document;
				if (document != null)
				{
					this.Position = new TextViewPosition(document.GetLocation(value));
					this.DesiredXPos = double.NaN;
				}
			}
		}

		void ValidatePosition()
		{
			if (m_position.Line < 1)
				m_position.Line = 1;
			if (m_position.Column < 1)
				m_position.Column = 1;
			if (m_position.VisualColumn < -1)
				m_position.VisualColumn = -1;
			TextDocument document = TextArea.Document;
			if (document != null)
			{
				if (m_position.Line > document.LineCount)
				{
					m_position.Line = document.LineCount;
					m_position.Column = document.GetLineByNumber(m_position.Line).Length + 1;
					m_position.VisualColumn = -1;
				}
				else
				{
					DocumentLine line = document.GetLineByNumber(m_position.Line);
					if (m_position.Column > line.Length + 1)
					{
						m_position.Column = line.Length + 1;
						m_position.VisualColumn = -1;
					}
				}
			}
		}

		/// <summary>
		/// Event raised when the caret position has changed.
		/// If the caret position is changed inside a document update (between BeginUpdate/EndUpdate calls),
		/// the PositionChanged event is raised only once at the end of the document update.
		/// </summary>
		public event EventHandler PositionChanged;

		bool raisePositionChangedOnUpdateFinished;

		void RaisePositionChanged()
		{
			if (TextArea.Document != null && TextArea.Document.IsInUpdate)
			{
				raisePositionChangedOnUpdateFinished = true;
			}
			else
			{
				if (PositionChanged != null)
				{
					PositionChanged(this, EventArgs.Empty);
				}
			}
		}

		internal void OnDocumentUpdateFinished()
		{
			if (raisePositionChangedOnUpdateFinished)
			{
				if (PositionChanged != null)
				{
					PositionChanged(this, EventArgs.Empty);
				}
			}
		}

		bool visualColumnValid;

		void InvalidateVisualColumn()
		{
			visualColumnValid = false;
		}

		/// <summary>
		/// Validates the visual column of the caret using the specified visual line.
		/// The visual line must contain the caret offset.
		/// </summary>
		void RevalidateVisualColumn(VisualLine visualLine)
		{
			if (visualLine == null)
				throw new ArgumentNullException("visualLine");

			// mark column as validated
			visualColumnValid = true;

			int caretOffset = TextView.Document.GetOffset(m_position.Location);
			int firstDocumentLineOffset = visualLine.FirstDocumentLine.Offset;
			m_position.VisualColumn = visualLine.ValidateVisualColumn(m_position, TextArea.Selection.EnableVirtualSpace);

			// search possible caret positions
			int newVisualColumnForwards = visualLine.GetNextCaretPosition(m_position.VisualColumn - 1, LogicalDirection.Forward, CaretPositioningMode.Normal, TextArea.Selection.EnableVirtualSpace);
			// If position.VisualColumn was valid, we're done with validation.
			if (newVisualColumnForwards != m_position.VisualColumn)
			{
				// also search backwards so that we can pick the better match
				int newVisualColumnBackwards = visualLine.GetNextCaretPosition(m_position.VisualColumn + 1, LogicalDirection.Backward, CaretPositioningMode.Normal, TextArea.Selection.EnableVirtualSpace);

				if (newVisualColumnForwards < 0 && newVisualColumnBackwards < 0)
					throw ThrowUtil.NoValidCaretPosition();

				// determine offsets for new visual column positions
				int newOffsetForwards;
				if (newVisualColumnForwards >= 0)
					newOffsetForwards = visualLine.GetRelativeOffset(newVisualColumnForwards) + firstDocumentLineOffset;
				else
					newOffsetForwards = -1;
				int newOffsetBackwards;
				if (newVisualColumnBackwards >= 0)
					newOffsetBackwards = visualLine.GetRelativeOffset(newVisualColumnBackwards) + firstDocumentLineOffset;
				else
					newOffsetBackwards = -1;

				int newVisualColumn, newOffset;
				// if there's only one valid position, use it
				if (newVisualColumnForwards < 0)
				{
					newVisualColumn = newVisualColumnBackwards;
					newOffset = newOffsetBackwards;
				}
				else if (newVisualColumnBackwards < 0)
				{
					newVisualColumn = newVisualColumnForwards;
					newOffset = newOffsetForwards;
				}
				else
				{
					// two valid positions: find the better match
					if (Math.Abs(newOffsetBackwards - caretOffset) < Math.Abs(newOffsetForwards - caretOffset))
					{
						// backwards is better
						newVisualColumn = newVisualColumnBackwards;
						newOffset = newOffsetBackwards;
					}
					else
					{
						// forwards is better
						newVisualColumn = newVisualColumnForwards;
						newOffset = newOffsetForwards;
					}
				}
				this.Position = new TextViewPosition(TextView.Document.GetLocation(newOffset), newVisualColumn);
			}
			m_in_virtual_space = (m_position.VisualColumn > visualLine.VisualLength);
		}

		Rect CalcCaretRectangle(VisualLine visualLine)
		{
			if (!visualColumnValid)
			{
				RevalidateVisualColumn(visualLine);
			}

			TextLine textLine = visualLine.GetTextLine(m_position.VisualColumn);
			double xPos = visualLine.GetTextLineVisualXPosition(textLine, m_position.VisualColumn);
			double lineTop = visualLine.GetTextLineVisualYPosition(textLine, VisualYPosition.TextTop);
			double lineBottom = visualLine.GetTextLineVisualYPosition(textLine, VisualYPosition.TextBottom);

			return new Rect(xPos,
							lineTop,
							SystemParameters.CaretWidth,
							lineBottom - lineTop);
		}

		/// <summary>
		/// Returns the caret rectangle. The coordinate system is in device-independent pixels from the top of the document.
		/// </summary>
		public Rect CalculateCaretRectangle()
		{
			if (TextView != null && TextView.Document != null)
			{
				VisualLine visualLine = TextView.GetOrConstructVisualLine(TextView.Document.GetLineByNumber(m_position.Line));
				return CalcCaretRectangle(visualLine);
			}
			else
			{
				return Rect.Empty;
			}
		}

		/// <summary>Scrolls the text view so that the caret is visible.</summary>
		public void BringCaretToView()
		{
			BringCaretToView(MinimumDistanceToViewBorder);
		}

		internal void BringCaretToView(double border)
		{
			Rect caretRectangle = CalculateCaretRectangle();
			if (!caretRectangle.IsEmpty)
			{
				caretRectangle.Inflate(border, border);
				TextView.MakeVisible(caretRectangle);
			}
		}

		/// <summary>Makes the caret visible and updates its on-screen position.</summary>
		public void Show()
		{
			Log("Caret.Show()");
			visible = true;
			if (!showScheduled)
			{
				showScheduled = true;
				TextArea.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(ShowInternal));
			}
		}

		bool showScheduled;
		bool hasWin32Caret;

		void ShowInternal()
		{
			showScheduled = false;

			// if show was scheduled but caret hidden in the meantime
			if (!visible)
				return;

			if (m_layer != null && TextView != null)
			{
				VisualLine visualLine = TextView.GetVisualLine(m_position.Line);
				if (visualLine != null)
				{
					Rect caretRect = CalcCaretRectangle(visualLine);
					// Create Win32 caret so that Windows knows where our managed caret is. This is necessary for
					// features like 'Follow text editing' in the Windows Magnifier.
					if (!hasWin32Caret)
					{
						hasWin32Caret = Win32.CreateCaret(TextView, caretRect.Size);
					}
					if (hasWin32Caret)
					{
						Win32.SetCaretPosition(TextView, caretRect.Location - TextView.ScrollOffset);
					}
					m_layer.Show(caretRect);
					TextArea.ime.UpdateCompositionWindow();
				}
				else
				{
					m_layer.Hide();
				}
			}
		}

		/// <summary>Makes the caret invisible.</summary>
		public void Hide()
		{
			visible = false;
			if (hasWin32Caret)
			{
				Win32.DestroyCaret();
				hasWin32Caret = false;
			}
			if (m_layer != null)
			{
				m_layer.Hide();
			}
		}
#endif

		/// <summary>The system setting for caret blink period (negative if disabled)</summary>
		internal static TimeSpan BlinkPeriod => TimeSpan.FromMilliseconds(GetCaretBlinkTime_());
		[DllImport("user32.dll", EntryPoint = "GetCaretBlinkTime")] private static extern int GetCaretBlinkTime_();

		/// <summary>Creates an invisible Win32 caret for the specified Visual with the specified size (coordinates local to the owner visual).</summary>
		public static bool Create(Visual owner, Size size)
		{
			if (PresentationSource.FromVisual(owner) is HwndSource source)
			{
				var r = owner.PointToScreen(new Point(size.Width, size.Height)) - owner.PointToScreen(new Point(0, 0));
				return CreateCaret_(source.Handle, IntPtr.Zero, (int)Math.Ceiling(r.X), (int)Math.Ceiling(r.Y));
			}
			return false;
		}
		[DllImport("user32.dll", EntryPoint = "CreateCaret")][return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CreateCaret_(IntPtr hWnd, IntPtr hBitmap, int nWidth, int nHeight);

		/// <summary>Sets the position of the caret previously created using <see cref="CreateCaret"/>. Position is relative to the owner visual.</summary>
		public static bool SetCaretPosition(Visual owner, Point position)
		{
			if (PresentationSource.FromVisual(owner) is HwndSource source)
			{
				var point_on_root_visual = owner.TransformToAncestor(source.RootVisual).Transform(position);
				var point_on_hwnd = point_on_root_visual.TransformToDevice(source.RootVisual);
				return SetCaretPos_((int)point_on_hwnd.X, (int)point_on_hwnd.Y);
			}
			return false;
		}
		[DllImport("user32.dll", EntryPoint = "SetCaretPos")][return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetCaretPos_(int x, int y);

		/// <summary>Destroys the caret previously created using <see cref="CreateCaret"/>.</summary>
		public static bool DestroyCaret() => DestroyCaret_();
		[DllImport("user32.dll", EntryPoint = "DestroyCaret")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DestroyCaret_();
	}
}