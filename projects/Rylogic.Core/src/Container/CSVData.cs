//***************************************************
// CSV Data
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Rylogic.Extn;

namespace Rylogic.Container
{
	public class CSVData
	{
		public class Row :List<string>
		{
			public Row()
			{ }
			public Row(int capacity) :base(capacity)
			{ }
			public Row(IEnumerable<object> values)
				:base(values.Select(x => x?.ToString() ?? string.Empty))
			{ }
			public Row(params object[] values)
				:base(values.Select(x => x?.ToString() ?? string.Empty))
			{ }

			/// <summary>Add a item to the row</summary>
			public new Row Add(string s)
			{
				base.Add(s);
				return this;
			}
			public Row Add(object s)
			{
				return Add(s.ToString());
			}

			/// <summary>Add multiple items to the row</summary>
			public Row Add<T>(IEnumerable<T> range)
			{
				foreach (var x in range) Add(x);
				return this;
			}
			public Row Add(params object[] values)
			{
				foreach (var x in values) Add(x.ToString());
				return this;
			}

			/// <summary>Write this row as a line of CSV data</summary>
			public virtual void Save(StreamWriter file, bool quoted = true)
			{
				var first = true;
				foreach (var e in this)
				{
					var elem = e ?? string.Empty;

					// Comma separate
					if (!first) file.Write(',');
					first = false;

					if (!quoted)
					{
						file.Write(elem);
					}
					else
					{
						file.Write('"');
						foreach (var ch in elem)
						{
							file.Write(ch);
							if (ch == '"') file.Write('"');
						}
						file.Write('"');
					}
				}

				// Empty rows still add a newline to preserve row counts
				file.Write('\n');
			}
		}
		public class CommentRow :Row
		{
			public CommentRow(string comment)
				:base(comment)
			{}
			public CommentRow(IEnumerable<string> comments)
				:base(comments.Select(x => (object)x))
			{}
			public CommentRow(params string[] comments)
				:base(comments.Select(x => (object)x))
			{}

			/// <summary>Write this row as a line of CSV data</summary>
			public override void Save(StreamWriter file, bool quoted)
			{
				file.Write("# ");
				var first = true;
				foreach (var e in this)
				{
					if (!first) file.Write(',');
					first = false;
					file.Write(e ?? string.Empty);
				}
				file.Write('\n');
			}
		}

		public CSVData()
		{
			Rows = new List<Row>();
		}

		/// <summary>
		/// If true, out of bounds write cause the CSV data to grow in size.
		/// if false, out of bounds reads/writes cause exceptions</summary>
		public bool AutoSize { get; set; }

		/// <summary>Return the number of rows</summary>
		public int RowCount
		{
			get { return Rows.Count; }
		}

		/// <summary>Read/Write access to the rows</summary>
		public List<Row> Rows { get; }

		/// <summary>Append a row to the CSV data</summary>
		public Row Add(Row row)
		{
			return Rows.Add2(row);
		}

		/// <summary>Append a row to the CSV data</summary>
		public Row Add(IEnumerable<object> values)
		{
			return Add(new Row(values));
		}

		/// <summary>Append a row to the CSV data</summary>
		public Row Add(params object[] values)
		{
			return Add(new Row(values));
		}

		/// <summary>Insert a row in the csv data</summary>
		public Row Insert(int index, Row row)
		{
			return Rows.Insert2(index, row);
		}
		
		/// <summary>Insert a row in the csv data</summary>
		public Row Insert(int index, IEnumerable<object> values)
		{
			return Rows.Insert2(index, new Row(values));
		}

		/// <summary>Append a row to the CSV data</summary>
		public Row Insert(int index, params object[] values)
		{
			return Rows.Insert2(index, new Row(values));
		}

		/// <summary>Add a comment row to the CSV data</summary>
		public CommentRow AddComment(params string[] comment)
		{
			return Rows.Add2(new CommentRow(comment));
		}

		/// <summary>Reserve memory</summary>
		public void Reserve(int rows, int columns)
		{
			Rows.Capacity = rows;
			foreach (var row in Rows)
				row.Capacity = columns;
		}

		/// <summary>Access a row</summary>
		public Row this[int row]
		{
			get
			{
				if (!AutoSize && row >= Rows.Count) throw new IndexOutOfRangeException();
				while (Rows.Count <= row) Rows.Add(new Row());
				return Rows[row];
			}
			set
			{
				if (!AutoSize && row >= Rows.Count) throw new IndexOutOfRangeException();
				while (Rows.Count <= row) Rows.Add(new Row());
				Rows[row] = value;
			}
		}

		/// <summary>Access an element by row,column</summary>
		public string this[int row, int col]
		{
			get
			{
				var R = this[row];
				if (!AutoSize && col >= R.Count) throw new IndexOutOfRangeException();
				while (R.Count <= col) R.Add("");
				return R[col];
			}
			set
			{
				var R = this[row];
				if (!AutoSize && col >= R.Count) throw new IndexOutOfRangeException();
				while (R.Count <= col) R.Add("");
				R[col] = value;
			}
		}

		/// <summary>
		/// Save a CSV file.
		/// If 'quoted' is false, elements are written without quotes around them. If the elements contain
		/// quote characters, commas, or newline characters then the produced CSV will be invalid.</summary>
		public void Save(string filepath, bool quoted = true)
		{
			using (var file = new StreamWriter(filepath))
				foreach (var row in Rows)
					row.Save(file, quoted);
		}

		/// <summary>
		/// Read CSV rows from a stream or file.
		/// if 'ignore_comment_rows' is true, rows that start with a '#' character are skipped, otherwise they are considered as CSV rows</summary>
		public static IEnumerable<Row> Parse(Stream src, bool ignore_comment_rows, Action<double> progress_cb = null)
		{
			// Parse the CSV stream
			// Fields can optionally be in quotes.
			// Quotes are escaped using double quotes.
			using (var file = new StreamReader(src))
			{
				var row = new Row();
				var str = new StringBuilder(); // Buffer for each element
				int line = 1, elem = 1;        // Natural indices

				// Progress reporting
				var last_progress = 0.0;
				var stream_length = file.BaseStream.Length;
				var ReportProgress = progress_cb == null || stream_length == 0 ? (Action)null : () =>
				{
					var progress = (double)file.BaseStream.Position / stream_length;
					if (progress - last_progress < 0.01) return;
					progress_cb(progress);
					last_progress = progress;
				};

				// Parse the file char-by-char emitting CSV rows
				for (int ch; (ch = next()) >= 0; )
				{
					Debug.Assert(str.Length == 0, "Each loop around should be for a new element");

					// Report progress
					ReportProgress?.Invoke();

					// If comments are supported, and the row starts with the comment symbol, consume the line
					if (ch == '#' && ignore_comment_rows && row.Count == 0)
					{
						for (; (ch = next()) >= 0 && ch != '\n'; ) {} // consume the line
						++line;
						elem = 1;
						continue;
					}

					// If the element starts with a quote, consume up to the next un-escaped quote
					if (ch == '"')
					{
						// Read literal chars directly from the stream, quoted text is allowed to contain \r\n
						for (; (ch = file.Read()) >= 0;)
						{
							if (ch == '"')
							{
								if (file.Peek() == '"') file.Read();
								else break;
							}
							str.Append((char)ch);
						}
						if (ch >= 0) ch = next(); // Consume the closing quote
						else throw new Exception($"Quoted CSV element not closed starting at line {line}, element {elem}");

						// Expect the next character to be delimiter or EOF
						if (ch != ',' && ch != '\n' && ch >= 0)
							throw new Exception($"Unexpected character following quoted CSV element at line {line}, element {elem}");
					}
					// Otherwise, if not a row or element delimiter, assume this is an un-quoted element, consume to the next delimiter
					else if (ch != ',' && ch != '\n')
					{
						// Otherwise, assume this is an un-quoted element, consume to the next delimiter
						str.Append((char)ch);
						for (; (ch = next()) >= 0 && ch != ',' && ch != '\n';)
							str.Append((char)ch);
					}

					// If the next character is an element delimiter, add the element to the row
					if (ch == ',')
					{
						row.Add(str.ToString());
						str.Clear();
						++elem;
						continue;
					}

					// If the next character is a row delimiter or EOF, emit the CSV row
					if (ch == '\n' || ch < 0)
					{
						// If the row is empty, don't add an empty element
						if (row.Count != 0 || str.Length != 0)
						{
							row.Add(str.ToString());
							str.Clear();
							++elem;
						}

						// Emit the row
						yield return row;
						row = new Row(row.Capacity);
						++line;
						elem = 1;
						continue;
					}
				}

				// Get the next character from the stream
				int next()
				{
					var chint = file.Read();

					// Deal with line endings. Converts '\r', '\n', '\r\n' to '\n'
					if (chint != '\r') return chint;
					if (file.Peek() == '\n') file.Read();
					return '\n';
				}
			}
		}
		public static IEnumerable<Row> Parse(string filepath, bool ignore_comment_rows, Action<double> progress_cb = null)
		{
			var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
			return Parse(stream, ignore_comment_rows, progress_cb);
		}

		/// <summary>
		/// Load and parse a CSV stream or file.
		/// if 'ignore_comment_rows' is true, rows that start with a '#' character are skipped, otherwise they are considered as CSV rows</summary>
		public static CSVData Load(Stream src, bool ignore_comment_rows, Action<double> progress_cb = null)
		{
			var csv = new CSVData();
			csv.Rows.AddRange(Parse(src, ignore_comment_rows, progress_cb));
			return csv;
		}
		public static CSVData Load(string filepath, bool ignore_comment_rows, Action<double> progress_cb = null)
		{
			var csv = new CSVData();
			csv.Rows.AddRange(Parse(filepath, ignore_comment_rows, progress_cb));
			return csv;
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Container;

	[TestFixture] public class TestCSVData
	{
		[Test] public void CSVRoundTrip()
		{
			var csv = new CSVData();
			csv.AddComment("This is a CSV file comment");
			csv.Add("One", "Two", "Three", "\"Four\"","\",\r\n\"");
			csv.Add("1,1", "2\r2", "3\n3", "4\r\n");
			csv.Add(new CSVData.Row());
			csv.Add("1,1", "2\r2", "3\n3", "4\r\n");

			var tmp = Path.GetTempFileName();
			csv.Save(tmp);

			try
			{
				var load = CSVData.Load(tmp, ignore_comment_rows:true);
				csv.Rows.RemoveAt(0); // Delete the comment

				Assert.Equal(csv.RowCount, load.RowCount);
				for (var i = 0; i != csv.RowCount; ++i)
				{
					var r0 = csv[i];
					var r1 = load[i];
					Assert.Equal(r0.Count, r1.Count);

					for (var j = 0; j != r0.Count; ++j)
					{
						var e0 = r0[j];
						var e1 = r1[j];
						Assert.Equal(e0,e1);
					}
				}
			}
			finally
			{
				File.Delete(tmp);
			}
		}
		[Test] public void CSVRaw()
		{
			var csv_string =
				"One,Two,Three\n" +
				"\"Quoted\",,\r\n" +
				"# Comment Line\r"+ 
				"Four,# Not a Comment,\"# Also\r\n# Not,\n# A\r\n# Comment\"";
			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv_string), false))
			{
				var load = CSVData.Load(ms, ignore_comment_rows:true);
				Assert.Equal(load.RowCount, 3);
				Assert.True(load[0].SequenceEqual(new[]{"One","Two","Three"}));
				Assert.True(load[1].SequenceEqual(new[]{"Quoted","",""}));
				Assert.True(load[2].SequenceEqual(new[]{"Four","# Not a Comment","# Also\r\n# Not,\n# A\r\n# Comment"}));
			}
		}
	}
}
#endif