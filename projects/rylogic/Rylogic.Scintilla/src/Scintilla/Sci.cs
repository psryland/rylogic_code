using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Rylogic.Common;
using Rylogic.Utility;
using Rylogic.Interop.Win32;
using Rylogic.Gfx;

namespace Rylogic.Scintilla
{
	/// <summary>Rylogic specific features</summary>
	public partial class Sci
	{
		/// <summary>Style id constants</summary>
		public struct EStyleId
		{
			// Notes:
			//  - There are 256 style ids available for use: [0,256).
			//  - Scintilla predefines some styles, i.e. the following

			private int m_id;

			public const int First = 0;
			public const int Default = STYLE_DEFAULT;
			public const int LineNumber = STYLE_LINENUMBER;
			public const int BraceLight = STYLE_BRACELIGHT;
			public const int BraceBad = STYLE_BRACEBAD;
			public const int ControlChar = STYLE_CONTROLCHAR;
			public const int IndentGuide = STYLE_INDENTGUIDE;
			public const int CallTip = STYLE_CALLTIP;
			public const int LastPredefined = STYLE_LASTPREDEFINED;
			public const int Max = STYLE_MAX;

			public static implicit operator EStyleId(int id)
			{
				if (id < 0 || id > Max) throw new Exception($"Invalid style id: {id}");
				return new EStyleId { m_id = id };
			}
			public static explicit operator int(EStyleId id)
			{
				return id.m_id;
			}
		}

		/// <summary></summary>
		public class StyleDesc
		{
			public StyleDesc(int id)
			{
				Id = id;
			}
			public Sci.EStyleId Id { get; }
			public Colour32? Fore { get; set; }
			public Colour32? Back { get; set; }
			public string? Font { get; set; }
			public int? Size { get; set; }
			public bool? Bold { get; set; }
			public bool? Italic { get; set; }
			public bool? Underline { get; set; }
			public bool? EOLFilled { get; set; }
			public int? CharSet { get; set; }
			public ECaseVisible? Case { get; set; }
			public bool? Visible { get; set; }
			public bool? Changeable { get; set; }
			public bool? HotSpot { get; set; }
		}

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

			public sealed class BufPtr :IDisposable
			{
				private GCHandle_.Scope m_scope = null!;
				public BufPtr(Cell[] cells, int ofs, int length)
				{
					m_scope = GCHandle_.Alloc(cells, GCHandleType.Pinned);
					Pointer = m_scope.Handle.AddrOfPinnedObject() + ofs * R<Cell>.SizeOf;
					SizeInBytes = length * R<Cell>.SizeOf;
				}
				public void Dispose()
				{
					Util.Dispose(ref m_scope!);
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
			if (!m_sci_name.TryGetValue(sci_id, out var name))
			{
				var fi = typeof(Sci).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
					.Where(x => x.IsLiteral)
					.Where(x => x.Name.StartsWith("SCI_") || x.Name.StartsWith("SCN_"))
					.FirstOrDefault(x =>
						{
							var val = x.GetValue(null);
							if (val is uint) return (uint)val == (uint)sci_id;
							if (val is int) return (int)val == sci_id;
							return false;
						});

				name = fi != null ? fi.Name : string.Empty;
				m_sci_name.Add(sci_id, name);
			}
			return name;
		}
		private static Dictionary<int, string> m_sci_name = new();

		#region DLL Loading

		private const string Dll = "scintilla";

		/// <summary>True if the scintilla dll has been loaded</summary>
		public static bool ModuleLoaded => m_module != IntPtr.Zero;
		private static IntPtr m_module = IntPtr.Zero;

		/// <summary>The error created if loading the dll fails</summary>
		public static Exception? LoadError = null;

		/// <summary>Load the scintilla dll</summary>
		public static bool LoadDll(string dir = @".\lib\$(platform)\$(config)", bool throw_if_missing = true)
		{
			return ModuleLoaded || (m_module = Win32.LoadDll(Dll + ".dll", out LoadError, dir, throw_if_missing)) != IntPtr.Zero;
		}

		#endregion
	}
}
