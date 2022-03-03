using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RyLogViewer
{
	public interface IReport
	{
		/// <summary>The main window</summary>
		MainWindow Owner { set; }

		/// <summary>Report an non-fatal error message</summary>
		void ErrorPopup(string msg, Exception ex);
		void ErrorPopup(string msg);
	}
}
