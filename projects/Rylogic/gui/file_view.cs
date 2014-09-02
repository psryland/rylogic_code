//***************************************************
// Copyright © Rylogic Ltd 2012
//***************************************************
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace pr.gui
{
	// work in progress....
	// basically a virtual edit control backed by a file

	/// <summary>A rich edit control that displays the lines of a text file</summary>
	public class FileView :RichTextBox ,ISupportInitialize
	{
		private const int AvrBytesPerLine = 256;
		private const int CacheSize = 4096;
		
		class Line
		{
			public long   m_ofs;     // The byte offset from the start of the file to this line
			public string m_text;    // The text of the line
			public Line(long ofs, string text) { m_ofs = ofs; m_text = text; }
		}

		///// <summary>The state of the terminal</summary>
		//private class State
		//{
		//    /// <summary>The output cursor location</summary>
		//    public Point m_cursor_location = new Point(0,0);

		//    /// <summary>The text attributes when last saved</summary>
		//    public Font m_saved_font = new Font(FontFamily.GenericMonospace, 10);

		//    /// <summary>The cursor position when last saved</summary>
		//    public Point m_saved_cursor_location = new Point(0,0);
		//}

		/// <summary>Terminal settings</summary>
		public FileViewSettings m_settings = new FileViewSettings();

		/// <summary>A FS watcher to detect file changes</summary>
		private readonly FileSystemWatcher m_watch;
		
		/// <summary>The file we're reading from (open with shared read/write)</summary>
		private FileStream m_file;
		
		/// <summary>The path of 'm_file'</summary>
		private string m_filepath;
		
		/// <summary>A temporary buffer for reading sections of the file</summary>
		private readonly byte[] m_buf;
		
		/// <summary>The line number of the first line written into the control</summary>
		private long m_line_first;
		
		/// <summary>The byte offset to the first character written into the control</summary>
		private long m_pos_first;
		
		/// <summary>The line number that 'm_pos' is on in the file</summary>
		private long m_line_index;
		
		/// <summary>The byte offset of the 'current' position in the file</summary>
		private long m_pos;

		/// <summary>Occurs when settings are changed</summary>
		public event Action<FileView> SettingsChanged;

		public FileView()
		{
			InitializeComponent();
			m_watch = new FileSystemWatcher{NotifyFilter = NotifyFilters.LastWrite|NotifyFilters.Size};
			m_buf = new byte[CacheSize];
			m_line_first = 0;
			m_pos_first = 0;
			m_line_index = 0;
			m_pos = 0;
			
			DoubleBuffered = true;
			Multiline      = true;
			HideSelection  = false;
			WordWrap       = false;
			ReadOnly       = true;
			Font           = m_settings.Font;
			
			// File watcher
			m_watch.Changed += (s,a) => Display();
			
			// Context menu
			MouseUp += (s,e)=>
			{
				if (e.Button == MouseButtons.Right)
					ShowContextMenu(e.Location);
			};

			Disposed += (s,e)=>
			{
				if (m_file != null)
					m_file.Dispose();
			};
		}

		/// <summary>View a file</summary>
		public void View(string filepath)
		{
			// Switch files
			FileStream file = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, m_settings.LineCount * AvrBytesPerLine);
			if (m_file != null) m_file.Close();
			m_filepath = filepath;
			m_file = file;
			
			// Setup the watcher to watch for file changes
			m_watch.Path = Path.GetDirectoryName(filepath);
			m_watch.Filter = Path.GetFileName(filepath);
		}

		
		/// <summary>Diplay the content of the log file from 'm_position'</summary>
		private void Display()
		{
			try
			{
				// Fill the control from 'm_pos_first'
				SuspendLayout();
				Clear();
				m_file.Seek(m_pos_first, SeekOrigin.Begin);
				using (StreamReader sr = new StreamReader(new UncloseableStream(m_file)))
				{
					for (long lines = 0; lines != m_settings.LineCount && !sr.EndOfStream; ++lines)
					{
						// Read a whole line
						string line = sr.ReadLine();
						if (line != null)
						{
							SelectionStart = int.MaxValue;
							SelectionLength = 0;
							int start_idx = SelectionStart;
							
							// Add the text to the control
							line = line.TrimEnd('\n','\r') + Environment.NewLine;
							SelectedText = line;
							
							// Do alternating backgrounds
							if (m_settings.AlternateLineColours)
							{
								SelectionStart  = start_idx;
								SelectionLength = line.Length;
								SelectionBackColor = ((lines + m_line_first) % 2) == 1
									? m_settings.LineColour1 : m_settings.LineColour2;
							}
							
							//todo RegExp highlighting...
							//todo RegExp filtering...
						}
					}
				}
			}
			finally
			{
				ResumeLayout();
			}
			//// Search backward through the file counting lines
			//// to get to the byte index to display from
			//long lines = 0;
			//long start = m_pos;
			//while (lines != m_settings.LineCount && start != 0)
			//{
			//    // The number of bytes to buffer
			//    long count = Math.Min(start, m_buf.Length);
			//    long pos = start - count;

			//    // Buffer file data
			//    m_file.Seek(pos, SeekOrigin.Begin);
			//    int read = m_file.Read(m_buf, 0, (int)count);
				
			//    // Search backwards counting lines
			//    while (read-- != 0)
			//    {
			//        --start;
			//        if (m_buf[read] != '\n') continue;
			//        if (++lines == m_settings.LineCount) break;
			//    }
			//}
			
			//// Fill the control from 'start' with upto 'm_settings.LineCount' lines
			//try
			//{
			//    long line_num = m_line_index - lines;
			//    SuspendLayout();
			//    Clear();
			//    using (StreamReader sr = new StreamReader(new UncloseableStream(m_file)))
			//    {
			//        // Read back to 'm_pos'
			//        m_file.Seek(start, SeekOrigin.Begin);
			//        while (!sr.EndOfStream && start < m_pos)
			//        {
			//            // Read a whole line
			//            string line = sr.ReadLine();
			//            if (line != null)
			//            {
			//                // Add the text to the control
			//                SelectedText = line;
							
			//                //todo RegExp filtering...

			//                // Do highlighting
			//                if (m_settings.AlternateLineColours)
			//                {
			//                    SelectionBackColor = (line_num % 2) == 1 ? m_settings.LineColour1 : m_settings.LineColour2;
			//                }
							
			//                // move the current position
			//                start += line.Length;
			//            }
			//        }
			//    }
			//}
			//finally
			//{
			//    ResumeLayout();
			//}

			// Insert the lines into the 

			// Seek to the
			//BufferLines();
			
			// Do everything from the end of the file backwards
		}

				/// <summary></summary>
		private void BufferLines()
		{

			//// If 'first' == 'm_range.m_begin' then assume 'm_lines' is still
			//// valid and simply add lines to the end of the collection.
			//bool append = first == m_range.m_begin && m_lines.Count != 0;
			//if (append)
			//{
			//    // Re-read the last line, because it may have been a partial line last time
			//    Line last   = m_lines[m_lines.Count - 1];
			//    first       = last.m_ofs;
			//    line_count -= m_lines.Count - 1;
		//    }
			
		//    // Open the file and read lines
		//    List<Line> lines = new List<Line>();
		//    using (FileStream fs )
		//    {
		//        // Seek to the place to start reading from
		//        fs.Seek(first, SeekOrigin.Begin);
		//        using (StreamReader sr = new StreamReader(fs))
		//        {
		//            // Read 'line_count' lines or to the end of the file, whichever happens first
		//            for (int i = 0; i != line_count && fs.Position != fs.Length; ++i)
		//                m_lines.Add(new Line{m_ofs = fs.Position, m_text = sr.ReadLine()});
		//        }
		//        if (append)
		//        {
		//            m_lines.RemoveAt(m_lines.Count - 1);
		//            m_lines.AddRange(lines);
		//            m_range.m_end = fs.Position;
		//        }
		//        else
		//        {
		//            m_lines = lines;
		//            m_range.m_begin = first;
		//            m_range.m_end = fs.Position;
		//        }
		//    }
		}
	
		/// <summary>Forces the control to invalidate its client area and immediately redraw itself and any child controls.</summary>
		public override void Refresh()
		{
			base.Refresh();
			Display();
		}
		
		/// <summary>A context menu for the vt100 terminal</summary>
		private void ShowContextMenu(Point location)
		{
			ContextMenuStrip menu = new ContextMenuStrip();
			{// Clear
				ToolStripMenuItem item = new ToolStripMenuItem{Text="Clear"};
				item.Click += (s,e)=> {Clear();};
				menu.Items.Add(item);
			}
			menu.Items.Add(new ToolStripSeparator());
			{// Copy
				ToolStripMenuItem item = new ToolStripMenuItem{Text="Copy"};
				item.Click += (s,e)=> {Copy();};
				menu.Items.Add(item);
			}
			{// Paste
				ToolStripMenuItem item = new ToolStripMenuItem{Text="Paste"};
				item.Click += (s,e)=> {Paste();};
				menu.Items.Add(item);
			}
			menu.Items.Add(new ToolStripSeparator());
			{// Terminal Options
				ToolStripMenuItem options = new ToolStripMenuItem{Text="Terminal Options"};
				{// Background colour
					ToolStripMenuItem item = new ToolStripMenuItem{Text="Background Colour"};
					ToolStripButton btn = new ToolStripButton{Text="   ", BackColor=BackColor, AutoToolTip=false};
					btn.Click += (s,e)=> { ColorDialog cd = new ColorDialog(); if (cd.ShowDialog() == DialogResult.OK) {BackColor = cd.Color;} menu.Close(); };
					item.DropDownItems.Add(btn);
					options.DropDownItems.Add(item);
				}
				{// Text colour
					ToolStripMenuItem item = new ToolStripMenuItem{Text="Text Colour"};
					ToolStripButton btn = new ToolStripButton{Text="   ", BackColor=ForeColor, AutoToolTip=false};
					btn.Click += (s,e)=> { ColorDialog cd = new ColorDialog(); if (cd.ShowDialog() == DialogResult.OK) {ForeColor = cd.Color;} menu.Close(); };
					item.DropDownItems.Add(btn);
					options.DropDownItems.Add(item);
				}
				menu.Items.Add(options);
			}

			// Show the context menu
			menu.Show(this, location);
		}

		/// <summary>Get/Set the control background colour </summary>
		public override Color BackColor
		{
			get { return base.BackColor; }
			set { base.BackColor = value; SettingsChanged.Raise(this); }
		}

		/// <summary>Get/Set the control foreground colour </summary>
		public override Color ForeColor
		{
			get { return base.ForeColor; }
			set { base.ForeColor = value; SettingsChanged.Raise(this); }
		}

		/// <summary>Return the number of lines of text in the control</summary>
		public int LineCount
		{
			get { return TextLength == 0 ? 0 : GetLineFromCharIndex(TextLength) + 1; }
		}

		/// <summary>Return the length of a line in the control</summary>
		public long LineLength(int line, bool include_newline)
		{
			return IndexRangeFromLine(line, include_newline).Count;
		}

		/// <summary>Get/Set the line that SelectionStart is on</summary>
		public int CurrentLine
		{
			get { return GetLineFromCharIndex(SelectionStart); }
			set { SelectionStart = GetFirstCharIndexFromLine(value); }
		}

		/// <summary>Return the index range for the given line</summary>
		public Range IndexRangeFromLine(int line, bool include_newline)
		{
			if (line <  0)         return new Range(0,0);
			if (line >= LineCount) return new Range(TextLength, TextLength);
			int idx0 = GetFirstCharIndexFromLine(line);
			int idx1 = GetFirstCharIndexFromLine(line+1);
			if (idx1 > idx0 && !include_newline) --idx1;
			return new Range(idx0, idx1 >= idx0 ? idx1 : TextLength);
		}

		/// <summary>Get/Set the terminal settings</summary>
		[Browsable(false)] public FileViewSettings Settings
		{
			get { return m_settings; }
			set
			{
				m_settings = value;
				base.BackColor = m_settings.BackColour;
				base.ForeColor = m_settings.ForeColour;
			}
		}

		#region C# designer code
		/// <summary>Required designer variable.</summary>
		private IContainer components;

		/// <summary>Clean up any resources being used.</summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null)) components.Dispose();
			base.Dispose(disposing);
		}

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			components = new Container();
		}

		public void BeginInit(){}
		public void EndInit(){}
		#endregion
	}

	/// <summary>Settings for the FileView</summary>
	[TypeConverter(typeof(FileViewSettings))]
	public class FileViewSettings :GenericTypeConverter<FileViewSettings>
	{
		/// <summary>The number of lines to buffer in the control</summary>
		public int LineCount { get; set; }
		
		/// <summary>The font style</summary>
		public string FontName { get; set; }

		/// <summary>Font size</summary>
		public int FontEmSize { get; set; }

		/// <summary>The font to display the file text with</summary>
		public Font Font { get { return new Font(new FontFamily(FontName), FontEmSize); } }
		
		/// <summary>True if we should alternate the background colour of lines</summary>
		public bool AlternateLineColours { get; set; }
		
		/// <summary>The background colour of alternate lines</summary>
		public Color LineColour1 { get; set; }
		
		/// <summary>The background colour of alternate lines</summary>
		public Color LineColour2 { get; set; }
		
		/// <summary>The back colour for the control</summary>
		public Color BackColour {get;set;}
		
		/// <summary>The fore colour for the control</summary>
		public Color ForeColour {get;set;}
		
		/// <summary>Watch the file and redisplay the tail whenever the file changes</summary>
		public bool Tail { get; set; }

		public FileViewSettings()
		{
			LineCount = 1000;
			FontName = FontFamily.GenericMonospace.Name;
			FontEmSize = 10;
			AlternateLineColours = true;
			LineColour1 = Color.White;
			LineColour2 = Color.LightCyan;

			//LocalEcho = true;
			//TabSize = 8;
			//TerminalWidth = 100;
			//NewlineRecv = ENewLineMode.LF;
			//NewlineSend = ENewLineMode.CR;
			BackColour = Color.White;
			ForeColour = Color.Black;
			Tail = true;
			//HexOutput = false;
		}

		/// <summary>Get/Set the settings as an xml string</summary>
		[Browsable(false)] public string XML
		{
			get
			{
				XDocument xml = new XDocument(
					new XElement("fileview_settings",
						new XElement("back_colour"    ,BackColour.ToArgb().ToString() ),
						new XElement("fore_colour"    ,ForeColour.ToArgb().ToString() )
						)
					);
				return xml.ToString();
			}
			set
			{
				if (string.IsNullOrEmpty(value)) return;
				XDocument xml = XDocument.Parse(value);
				XElement node = xml.Element("fileview_settings"); if (node == null) return;
				foreach (XElement n in node.Descendants())
				{
					switch (n.Name.LocalName)
					{
					default: break;
					case "back_colour":    BackColour    = Color.FromArgb(int.Parse(n.Value)); break;
					case "fore_colour":    ForeColour    = Color.FromArgb(int.Parse(n.Value)); break;
					}
				}
			}
		}
	}
}
