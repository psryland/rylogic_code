//***************************************************
// CSV Data
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace pr.util
{
	public class CSVData
	{
		public class Row :List<string>
		{
			public Row() {}
			public Row(IEnumerable<string> collection) :base(collection) {} 
			public Row(int capacity) :base(capacity) {}
		}
		
		private readonly List<Row> m_data = new List<Row>();

		// If true, out of bounds write cause the csv data to grow in size
		// if false, out of bounds reads/writes cause exceptions
		public bool AutoSize
		{
			get;
			set;
		}

		// Return the number of rows
		public int RowCount
		{
			get { return m_data.Count; }
		}

		// Read/Write access to the rows
		public List<Row> Rows
		{
			get { return m_data; }
		}

		// Access to the end of the collection
		public void Add(Row row)
		{
			m_data.Add(row);
		}

		// Reserve memory
		public void Reserve(int rows, int columns)
		{
			m_data.Capacity = rows;
			foreach (var row in m_data)
				row.Capacity = columns;
		}

		// Access a row
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

		// Access an element by row,column
		public string this[int row, int col]
		{
			get
			{
				List<string> R = this[row];
				if (!AutoSize && col >= R.Count) throw new IndexOutOfRangeException();
				while (R.Count <= col) R.Add("");
				return R[col];
			}
			set
			{
				List<string> R = this[row];
				if (!AutoSize && col >= R.Count) throw new IndexOutOfRangeException();
				while (R.Count <= col) R.Add("");
				R[col] = value;
			}
		}

		// Load and parse a csv file
		public static CSVData Load(string filepath)
		{
			CSVData csv = new CSVData();
		
			using (TextReader file = new StreamReader(filepath))
			{
				StringBuilder str = new StringBuilder();
				var row = new Row();
				while (!((StreamReader)file).EndOfStream)
				{
					char ch = (char)file.Read();
					switch (ch)
					{
					default:   str.Append(ch); break;
					case ',':  row.Add(str.ToString()); str.Length = 0; break;
					case '\n': row.Add(str.ToString()); str.Length = 0; csv.m_data.Add(row); row = new Row(); break;
					case '\r': break;
					}
				}
				if (str.Length != 0) row.Add(str.ToString());
				if (row.Count != 0) csv.m_data.Add(row);
			}
			return csv;
		}

		// Save a csv file
		public void Save(string filepath)
		{
			using (TextWriter file = new StreamWriter(filepath))
			{
				foreach (var row in m_data)
				{
					if (row.Count != 0) file.Write(row[0]);
					for (int i = 1; i < row.Count; ++i)
					{
						file.Write(',');
						file.Write(row[i]);
					}
					file.Write('\n');
				}
			}
		}
	}
}