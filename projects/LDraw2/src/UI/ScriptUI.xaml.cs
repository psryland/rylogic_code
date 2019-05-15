using Rylogic.Gui.WPF;
using Rylogic.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LDraw.UI
{
	public partial class ScriptUI : UserControl, IDockable, IDisposable
	{
		public ScriptUI(string name)
		{
			InitializeComponent();
			DockControl = new DockControl(this, name)
			{
				TabText = name,
				ShowTitle = false,
				TabCMenu = (ContextMenu)FindResource("TabCMenu"),
			};
		}
		public virtual void Dispose()
		{
			DockControl = null;
		}

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			[DebuggerStepThrough]
			get { return m_dock_control; }
			private set
			{
				if (m_dock_control == value) return;
				if (m_dock_control != null)
				{
					m_dock_control.ActiveChanged -= HandleSceneActive;
					m_dock_control.SavingLayout -= HandleSavingLayout;
					Util.Dispose(ref m_dock_control);
				}
				m_dock_control = value;
				if (m_dock_control != null)
				{
					m_dock_control.SavingLayout += HandleSavingLayout;
					m_dock_control.ActiveChanged += HandleSceneActive;
				}

				// Handlers
				void HandleSceneActive(object sender, ActiveContentChangedEventArgs args)
				{
					//	Options.BkColour = args.ContentNew == this ? Color.LightSteelBlue : Color.LightGray;
					//	Invalidate();
				}
				void HandleSavingLayout(object sender, DockContainerSavingLayoutEventArgs args)
				{
					//	args.Node.Add2(nameof(Camera), Camera, false);
				}
			}
		}
		private DockControl m_dock_control;
	}
}
