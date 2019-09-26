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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Rylogic.Gui.WPF;

namespace Bot.Arbitrage
{
	public partial class ConfigureUI :Window
	{
		private readonly Bot m_bot;
		public ConfigureUI(Window owner, Bot bot)
		{
			InitializeComponent();
			Icon = owner?.Icon;
			Owner = owner;
			m_bot = bot;

			// Commands
			Accept = Command.Create(this, AcceptInternal);

			DataContext = this;
		}

		/// <summary>Ok Button</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			Close();
		}
	}
}
