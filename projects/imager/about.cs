using System.Reflection;
using System.Windows.Forms;

namespace imager
{
	public partial class About :Form
	{
		public About()
		{
			InitializeComponent();
			
			Assembly asm = Assembly.GetExecutingAssembly();
			
			m_lbl_title.Text =
				GetAttr<AssemblyTitleAttribute>().Title;
			
			m_lbl_copyright.Text =
				GetAttr<AssemblyCopyrightAttribute>().Copyright + " " +
				GetAttr<AssemblyCompanyAttribute>().Company + "\r\n" +
				"All Rights Reserved.";
			
			m_lbl_credits.Text =
				"Imager is based on DirectX 9.0c and DirectShow.NET";

			m_lbl_version_check.Text = 
				"Version: " + asm.GetName().Version;
		}

		private static T GetAttr<T>()
		{
			object[] attr = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false);
			return (T)(attr[0]);
		}
	}
}
