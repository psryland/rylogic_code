using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestCS
{
	public partial class FormTestApp : Form
	{
		private FormView3d m_form_view3d;
		public FormTestApp()
		{
			InitializeComponent();

			m_menu_file_exit.Click += (s,a) =>
				{
					Close();
				};

			m_menu_tests_view3d.Click += (s,a) =>
				{
					m_form_view3d = new FormView3d();
					m_form_view3d.Closed += (ss,aa) => m_form_view3d = null;
					m_form_view3d.Show(this);
				};

			m_menu_test_colour_wheel.Click += (s,a) =>
				{
					new FormColourWheel().Show(this);
				};
		}
	}
}
