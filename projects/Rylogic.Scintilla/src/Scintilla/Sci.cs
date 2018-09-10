using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Rylogic.Common;
using Rylogic.Utility;
using Rylogic.Interop.Win32;

namespace Rylogic.Scintilla
{
	/// <summary>Typedef Scintilla.Scintilla to 'Sci' and add Rylogic specific features</summary>
	public class Sci :global::Scintilla.Scintilla
	{
		#region Enumerations
		public enum EEndOfLineMode
		{
			CR   = SC_EOL_CR,
			LF   = SC_EOL_LF,
			CRLF = SC_EOL_CRLF,
		}
		#endregion

		/// <summary>Helper for sending text to scintilla</summary>
		public class CellBuf
		{
			public CellBuf(int capacity = 1024)
			{
				m_cells = new Cell[capacity];
				Length = 0;
			}

			/// <summary>The capacity of the buffer</summary>
			public int Capacity
			{
				get { return m_cells.Length; }
				set
				{
					Array.Resize(ref m_cells, value);
					Length = Math.Min(Length, value);
				}
			}

			/// <summary>The number of valid cells in the buffer</summary>
			public int Length { get; set; }

			/// <summary>The buffer of scintilla cells</summary>
			public Cell[] Cells { get { return m_cells; } }
			protected Cell[] m_cells;

			/// <summary>Add a single character to the buffer, along with a style</summary>
			public void Add(byte ch, byte sty)
			{
				EnsureSpace(Length + 1);
				Append(ch, sty);
				++Length;
			}

			/// <summary>Add an array of bytes all with the same style</summary>
			public void Add(byte[] bytes, byte sty)
			{
				EnsureSpace(Length + bytes.Length);
				Append(bytes, sty);
				Length += bytes.Length;
			}

			/// <summary>Add a string to the buffer, all using the style 'sty'</summary>
			public void Add(string text, byte sty)
			{
				Add(Encoding.UTF8.GetBytes(text), sty);
			}

			/// <summary>Remove the last 'n' cells from the buffer</summary>
			public void Pop(int n)
			{
				Length -= Math.Min(n, Length);
			}

			/// <summary>Pin the buffer so it can be passed to Scintilla</summary>
			public virtual BufPtr Pin()
			{
				return new BufPtr(m_cells, 0, Length);
			}

			/// <summary>Grow the internal array to ensure it can hold at least 'new_size' cells</summary>
			public void EnsureSpace(int new_size)
			{
				if (new_size <= Capacity) return;
				new_size = Math.Max(new_size, Capacity * 3/2);
				Resize(new_size);
			}

			/// <summary>Add a single byte and style to the buffer</summary>
			protected virtual void Append(byte ch, byte sty)
			{
				m_cells[Length] = new Cell(ch, sty);
			}

			/// <summary>Add an array of bytes and style to the buffer</summary>
			protected virtual void Append(byte[] bytes, byte sty)
			{
				var ofs = Length;
				for (int i = 0; i != bytes.Length; ++i, ++ofs)
					m_cells[ofs] = new Cell(bytes[i], sty);
			}

			/// <summary>Resize the cell buffer</summary>
			protected virtual void Resize(int new_size)
			{
				Array.Resize(ref m_cells, new_size);
			}

			public class BufPtr :IDisposable
			{
				private GCHandle_.Scope m_scope;
				public BufPtr(Cell[] cells, int ofs, int length)
				{
					m_scope = GCHandle_.Alloc(cells, GCHandleType.Pinned);
					Pointer = m_scope.Handle.AddrOfPinnedObject() + ofs * R<Cell>.SizeOf;
					SizeInBytes = length * R<Cell>.SizeOf;
				}
				public void Dispose()
				{
					Util.Dispose(ref m_scope);
				}

				/// <summary>The pointer to the pinned memory</summary>
				public IntPtr Pointer { get; private set; }

				/// <summary>The size of the buffered data</summary>
				public int SizeInBytes { get; private set; }
			}
		}

		/// <summary>A cell buffer that fills from back to front</summary>
		public class BackFillCellBuf :CellBuf
		{
			public BackFillCellBuf(int capacity = 1024) :base(capacity)
			{}

			/// <summary>Add a single character to the buffer, along with a style</summary>
			protected override void Append(byte ch, byte sty)
			{
				m_cells[m_cells.Length - Length] = new Cell(ch, sty);
			}

			/// <summary>Add a string to the buffer, all using the style 'sty'</summary>
			protected override void Append(byte[] bytes, byte sty)
			{
				var ofs = m_cells.Length - Length - bytes.Length;
				for (int i = 0; i != bytes.Length; ++i, ++ofs)
					m_cells[ofs] = new Cell(bytes[i], sty);
			}

			/// <summary>Resize the cell buffer, copying the contents to the end of the new buffer</summary>
			protected override void Resize(int new_size)
			{
				var new_cells = new Cell[new_size];
				Array.Copy(m_cells, m_cells.Length - Length, new_cells, new_cells.Length - Length, Length);
				m_cells = new_cells;
			}

			/// <summary>Pin the buffer so it can be passed to Scintilla</summary>
			public override BufPtr Pin()
			{
				return new BufPtr(m_cells, m_cells.Length - Length, Length);
			}
		}

		/// <summary>Convert a scintilla message id to a string</summary>
		public static string IdToString(int sci_id)
		{
			string name;
			if (!m_sci_name.TryGetValue(sci_id, out name))
			{
				var fi = typeof(Sci).GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static)
					.Where(x => x.IsLiteral)
					.Where(x => x.Name.StartsWith("SCI_") || x.Name.StartsWith("SCN_"))
					.FirstOrDefault(x =>
						{
							var val = x.GetValue(null);
							if (val is uint) return (uint)val == (uint)sci_id;
							if (val is int)  return (int)val == sci_id;
							return false;
						});

				name = fi != null ? fi.Name : string.Empty;
				m_sci_name.Add(sci_id, name);
			}
			return name;
		}
		private static Dictionary<int, string> m_sci_name = new Dictionary<int,string>();

		#region Scintilla Dll
		public const string Dll = "scintilla";

		public static bool ScintillaAvailable { get { return m_module != IntPtr.Zero; } }
		private static IntPtr m_module = IntPtr.Zero;

		/// <summary>Load the scintilla dll</summary>
		public static void LoadDll(string dir = @".\lib\$(platform)")
		{
			if (m_module != IntPtr.Zero) return; // Already loaded
			m_module = Win32.LoadDll(Dll+".dll", dir);
		}
		#endregion
	}
}
