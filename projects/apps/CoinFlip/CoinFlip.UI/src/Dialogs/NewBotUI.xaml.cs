﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using CoinFlip.Bots;
using Rylogic.Gui.WPF;
using Rylogic.Plugin;

namespace CoinFlip.UI.Dialogs
{
	public partial class NewBotUI :Window, INotifyPropertyChanged
	{
		public NewBotUI(Window owner)
		{
			InitializeComponent();
			Owner = owner;
			Icon = owner.Icon;
			BotName = "New Bot";
			AvailableBots = new ListCollectionView(IBot.AvailableBots().ToList());
			AvailableBots.CurrentChanged += delegate { NotifyPropertyChanged(nameof(Validate)); };
			Accept = Command.Create(this, AcceptInternal);
			DataContext = this;
		}

		/// <summary>Bot name</summary>
		public string BotName
		{
			get => (string)GetValue(BotNameProperty);
			set => SetValue(BotNameProperty, value);
		}
		private void BotName_Changed() => NotifyPropertyChanged(nameof(Validate));
		public static readonly DependencyProperty BotNameProperty = Gui_.DPRegister<NewBotUI>(nameof(BotName), string.Empty, Gui_.EDPFlags.TwoWay);

		/// <summary>The bots to choose from</summary>
		public ICollectionView AvailableBots { get; }

		/// <summary>Get/Set the bot selected from the list</summary>
		public PluginFile SelectedBot
		{
			get => (PluginFile)AvailableBots.CurrentItem;
			set => AvailableBots.MoveCurrentTo(value);
		}

		/// <summary>Perform validation on the new bot settings</summary>
		public Exception? Validate
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

		/// <summary>Close with success</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			if (Validate != null) return;
			DialogResult = true;
			Close();
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
