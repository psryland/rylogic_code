//***************************************************
// CSV Data
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using pr.stream;

namespace pr.container
{
	public class CSVData
	{
		public class Row :List<string>
		{
			public Row() {}
			public Row(int capacity) :base(capacity) {}
			public Row(IEnumerable<object> values) :base(values.Select(x => x.ToString())) {}
			public Row(params object[] values) :base(values.Select(x => x.ToString())) {}

			public new Row Add(string s) { base.Add(s); return this; }
			public Row Add<T>(T s) { base.Add(s.ToString()); return this; }
		}

		private readonly List<Row> m_data = new List<Row>();

		/// <summary>
		/// If true, out of bounds write cause the csv data to grow in size.
		/// if false, out of bounds reads/writes cause exceptions</summary>
		public bool AutoSize { get; set; }

		/// <summary>Return the number of rows</summary>
		public int RowCount
		{
			get { return m_data.Count; }
		}

		/// <summary>Read/Write access to the rows</summary>
		public List<Row> Rows
		{
			get { return m_data; }
		}

		/// <summary>Append a row to the CSV data</summary>
		public void Add(Row row)
		{
			m_data.Add(row);
		}

		/// <summary>Append a row to the CSV data</summary>
		public void Add(IEnumerable<object> values)
		{
			Add(new Row(values));
		}

		/// <summary>Append a row to the CSV data</summary>
		public void Add(params object[] values)
		{
			Add(new Row(values));
		}

		/// <summary>Reserve memory</summary>
		public void Reserve(int rows, int columns)
		{
			m_data.Capacity = rows;
			foreach (var row in m_data)
				row.Capacity = columns;
		}

		/// <summary>Access a row</summary>
		public Row this[int row]
		{
			get
			{
				if (!AutoSize && row >= m_data.Count) throw new IndexOutOfRangeException();
				while (m_data.Count <= row) m_data.Add(new Row());
				return m_data[row];
			}
			set
			{
				if (!AutoSize && row >= m_data.Count) throw new IndexOutOfRangeException();
				while (m_data.Count <= row) m_data.Add(new Row());
				m_data[row] = value;
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

		/// <summary>Read csv rows from a stream</summary>
		public static IEnumerable<Row> Parse(Stream src)
		{
			using (var file = new StreamReader(src))//new UncloseableStream(src)))
			{
				// Fields can optionally be in quotes.
				// Quotes are escaped using double quotes.
				var str = new StringBuilder();
				var row = new Row();
				var esc = false;
				var quoted = false;
				while (!file.EndOfStream)
				{
					var ch = (char)file.Read();

					// If the first character is a '"', this is a quoted item
					if (str.Length == 0 && ch == '"' && !quoted)
					{
						quoted = true;
						continue;
					}

					// If this is a quoted item, check for escaped '"' characters
					if (quoted && ch == '"')
					{
						if (esc) str.Append(ch);
						esc = !esc;
						continue;
					}

					// If this is an item delimiter
					if (ch == ',')
					{
						// If not a quoted item, or we've just passed an unescaped '"' character, end the item
						if (!quoted || esc)
						{
							row.Add(str.ToString());
							str.Length = 0;
							quoted = false;
							esc = false;
							continue;
						}
					}

					// If this is an end of row delimiter
					if (ch == '\n')
					{
						// If not a quoted item, or we've just passed an unescaped '"' character, end the row
						if (!quoted || esc)
						{
							// If there is nothing on the row, leave the row empty
							if (row.Count != 0 || str.Length != 0)
								row.Add(str.ToString());
							str.Length = 0;
							yield return row;
							row = new Row();
							quoted = false;
							esc = false;
							continue;
						}
					}

					str.Append(ch);
				}
				if (str.Length != 0) row.Add(str.ToString());
				if (row.Count != 0) yield return row;
			}
		}

		/// <summary>Stream rows from a csv file</summary>
		public static IEnumerable<Row> Parse(string filepath)
		{
			return Parse(new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read));
		}

		/// <summary>Load and parse a csv file</summary>
		public static CSVData Load(string filepath)
		{
			var csv = new CSVData();
			foreach (var row in Parse(filepath))
				csv.Add(row);
			return csv;
		}

		/// <summary>
		/// Save a csv file.
		/// If 'quoted' is false, elements are written without quotes around them. If the elements contain
		/// quote characters, commas, or newline characters then the produced CSV will be invalid.</summary>
		public void Save(string filepath, bool quoted = true)
		{
			using (var file = new StreamWriter(filepath))
			{
				foreach (var row in m_data)
				{
					var first = true;
					foreach (var e in row)
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
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using container;

	[TestFixture] public static partial class UnitTests
	{
		internal static class TestCSVData
		{
			[Test] public static void CSVRoundTrip()
			{
				var csv = new CSVData();
				csv.Add("One", "Two", "Three", "\"Four\"","\",\r\n\"");
				csv.Add("1,1", "2\r2", "3\n3", "4\r\n");
				csv.Add(new CSVData.Row());
				csv.Add("1,1", "2\r2", "3\n3", "4\r\n");

				var tmp = Path.GetTempFileName();
				csv.Save(tmp);

				try
				{
					var load = CSVData.Load(tmp);

					Assert.AreEqual(csv.RowCount, load.RowCount);
					for (var i = 0; i != csv.RowCount; ++i)
					{
						var r0 = csv[i];
						var r1 = load[i];
						Assert.AreEqual(r0.Count, r1.Count);

						for (var j = 0; j != r0.Count; ++j)
						{
							var e0 = r0[j];
							var e1 = r1[j];
							Assert.AreEqual(e0,e1);
						}
					}
				}
				finally
				{
					File.Delete(tmp);
				}
			}
		}
	}
}
#endif