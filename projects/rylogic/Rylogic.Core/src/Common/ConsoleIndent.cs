using System;
using System.IO;
using System.Text;

namespace Rylogic.Common
{
	/// <summary>Helper for supporting indenting on the console</summary>
	public class ConsoleIndent :TextWriter
	{
		// Notes:
		// using (var ci = new ConsoleIndent{Indent = 4})
		//    Console.WriteLine("Intended text");

		private TextWriter m_OldConsole;
		private bool m_DoIndent;

		public ConsoleIndent()
		{
			m_OldConsole = Console.Out;
			m_DoIndent = true;
			IndentString = "  ";
			Indent = 0;
			Console.SetOut(this);
		}
		protected override void Dispose(bool disposing)
		{
			Release();
			base.Dispose(disposing);
		}

		/// <summary>The string inserted as an indent</summary>
		public string IndentString { get; set; }

		/// <summary>The current indent level</summary>
		public int Indent { get; set; }

		/// <summary>The length of the indent string inserted on each new line</summary>
		public int IndentStringLength => IndentString.Length * Indent;

		/// <summary></summary>
		public override Encoding Encoding => m_OldConsole.Encoding;

		/// <summary></summary>
		public void Release()
		{
			if (m_OldConsole != null) Console.SetOut(m_OldConsole);
			m_OldConsole = Console.Out;
		}

		/// <summary></summary>
		public override void Write(char ch)
		{
			for (int i = 0; m_DoIndent && i != Indent; ++i)
				m_OldConsole.Write(IndentString);

			m_OldConsole.Write(ch);
			m_DoIndent = ch == '\n';
		}
	}
}
