using System;
using System.IO;
using System.Text;

namespace pr.common
{
	///<remarks>
	/// Usage:
	/// using (var ci = new ConsoleIndent{Indent = 4})
	///    Console.WriteLine("Intended text");
	///</remarks>

	/// <summary>Helper for supporting indenting on the console</summary>
	public class ConsoleIndent :TextWriter ,IDisposable
	{
		private TextWriter m_OldConsole;
		private bool m_DoIndent;

		/// <summary>The string inserted as an indent</summary>
		public string IndentString { get; set; }

		/// <summary>The current indent level</summary>
		public int Indent { get; set; }

		/// <summary>The length of the indent string inserted on each new line</summary>
		public int IndentStringLength { get { return IndentString.Length * Indent; } }

		public ConsoleIndent()
		{
			m_OldConsole = Console.Out;
			m_DoIndent = true;
			IndentString = "  ";
			Indent = 0;
			Console.SetOut(this);
		}
		public void Release()
		{
			if (m_OldConsole != null) Console.SetOut(m_OldConsole);
			m_OldConsole = null;
		}
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Release();
		}
		public override void Write(char ch)
		{
			for (int i = 0; m_DoIndent && i != Indent; ++i) m_OldConsole.Write(IndentString);
			m_OldConsole.Write(ch);
			m_DoIndent = ch == '\n';
		}
		public override Encoding Encoding
		{
			get { return m_OldConsole.Encoding; }
		}
	}
}