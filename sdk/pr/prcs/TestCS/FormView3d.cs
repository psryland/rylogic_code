using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.gui;

namespace TestCS
{
	public partial class FormView3d : Form
	{
		private readonly View3D m_view3d;
		public FormView3d()
		{
			InitializeComponent();
			m_view3d = new View3D();
			m_view3d.Anchor = (AnchorStyles)(AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right);
			m_view3d.BorderStyle = BorderStyle.FixedSingle;
			m_view3d.ClickTimeMS = 180;
			m_view3d.Dock = DockStyle.Fill;
			m_view3d.Location = new Point(3, 3);
			m_view3d.Name = "m_view3d";
			m_view3d.FillMode = View3D.EFillMode.Solid;
			m_view3d.Size = new Size(324, 251);
			m_view3d.TabIndex = 2;
			Controls.Add(m_view3d);
		}
	}
}
