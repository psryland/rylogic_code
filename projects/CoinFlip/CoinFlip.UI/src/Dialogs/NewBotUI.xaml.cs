using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using CoinFlip.Bots;
using Rylogic.Gui.WPF;
using Rylogic.Plugin;

namespace CoinFlip.UI
{
	public partial class NewBotUI :Window, INotifyPropertyChanged
	{
		static NewBotUI()
		{
			BotNameProperty = Gui_.DPRegister<NewBotUI>(nameof(BotName));
		}
		public NewBotUI(Window owner = null)
		{
			InitializeComponent();
			Owner = owner;
			Icon = owner?.Icon;
			BotName = "New Bot";
			AvailableBots = new ListCollectionView(IBot.AvailableBots().ToList());
			AvailableBots.CurrentChanged += delegate { NotifyValidate(); };
			Accept = Command.Create(this, AcceptInternal);
			DataContext = this;
		}

		/// <summary>Bot name</summary>
		public string BotName
		{
			get { return (string)GetValue(BotNameProperty); }
			set { SetValue(BotNameProperty, value); }
		}
		private void BotName_Changed() => NotifyValidate();
		public static readonly DependencyProperty BotNameProperty;

		/// <summary>The bots to choose from</summary>
		public ICollectionView AvailableBots { get; }

		/// <summary>Get/Set the bot selected from the list</summary>
		public PluginFile SelectedBot
		{
			get => (PluginFile)AvailableBots.CurrentItem;
			set => AvailableBots.MoveCurrentTo(value);
		}

		/// <summary>Perform validation on the new bot settings</summary>
		public Exception Validate
		{
			get
			{
				if (SelectedBot == null)
					return new Exception("No Bot type selected");
				if (BotName.Length == 0)
					return new Exception("Bot name is invalid");
				return null;
			}
		}
		private void NotifyValidate()
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Validate)));
		}

		/// <summary>Close with success</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			if (Validate != null) return;
			DialogResult = true;
			Close();
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
