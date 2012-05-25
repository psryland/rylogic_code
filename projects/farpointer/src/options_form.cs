using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FarPointer
{
	public partial class OptionsForm :Form
	{
		public OptionsForm()
		{
			InitializeComponent();
			m_edit_listen_port.KeyPress += delegate(object sender, KeyPressEventArgs e)
			{
				int num;
				e.Handled = !int.TryParse(e.KeyChar.ToString(), out num);
			};
		}
	}
}
