using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace LDraw.Dialogs
{
	public partial class AboutUI :Window
	{
		public AboutUI(Window owner)
		{
			InitializeComponent();
			Owner = owner;
			Icon = owner?.Icon;

			Accept = Command.Create(this, AcceptInternal);

			DataContext = this;

			var anim = $"*Animation {{ *Style PlayContinuous *Period {{1}} *AngVelocity {{ 1 1 1 }} }}";
			var id = m_view.View3d.LoadScript(
				$"*Sphere s FF0000FF {{ 0.6 }}\n" +
				$"*Box b0 FFFF0000 {{ 1.003 {anim} }}\n" +
				$"*Box b1 FF00FF00 {{ 1.002 {anim} }}\n" +
				$"*Box b2 FF0000FF {{ 1.001 {anim} }}\n",
				false);

			m_view.Window.AddObjects(id);
			m_view.Window.SetLightSource(v4.Origin, new v4(-1, -1, -2, 0), true);
			m_view.Camera.SetPosition(new v4(2, 2, 2, 1));
			Closed += delegate
			{
				m_view.Dispose();
			};
		}

		/// <summary></summary>
		public string AboutInfo =>
			$"{Util.AppProductName} - Scripted 3D object viewer.\r\n" +
			$"Version: {Util.AppVersion}\r\n" +
			$"{Util.AppCopyright}\r\n";

		/// <summary>Ok button</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			Close();
		}
	}
}
