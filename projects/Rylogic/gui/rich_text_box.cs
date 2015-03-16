using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using pr.util;
using pr.win32;

namespace pr.gui
{
	/// <summary>Subclass winforms RichTextBox to get RICHEDIT5.0 instead of 2.0!</summary>
	public class RichTextBox :System.Windows.Forms.RichTextBox
	{
		protected override CreateParams CreateParams
		{
			get
			{
				var cparams = base.CreateParams; 
				if (Win32.LoadLibrary("msftedit.dll") != IntPtr.Zero)
				{
					cparams.ClassName = "RICHEDIT50W";
				}
				return cparams;
			 }
		}
	}
}
