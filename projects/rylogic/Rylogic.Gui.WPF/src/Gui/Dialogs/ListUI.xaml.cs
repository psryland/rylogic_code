﻿using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class ListUI : Window, INotifyPropertyChanged
	{
		// Example Usage:
		//  var dlg = new ListUI(owner)
		//  {
		//      Title = "Available Options",
		//      Prompt = "Choose one of these",
		//      SelectionMode = SelectionMode.Extended,
		//      DisplayMember = nameof(Thing.Name),
		//      AllowCancel = true,
		//  };
		//  dlg.Items.AddRange(Model.AllTheThings());
		//  if (dlg.ShowDialog() == true)
		//  {
		//      Use(dlg.SelectedItems.Cast<Thing>());
		//  }

		public ListUI(Window? owner = null)
		{
			InitializeComponent();
			Owner = owner;
			Icon = Owner?.Icon;
			AllowCancel = false;
			Items = new ObservableCollection<object>();
			DataContext = this;

			Accept = Command.Create(this, () =>
			{
				if (!IsValid) return;
				DialogResult = true;
				Close();
			});
			m_list.SelectionChanged += (s, a) =>
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsValid)));
			};
			m_list.PreviewMouseDoubleClick += (s, a) =>
			{
				Accept.Execute();
			};
		}

		/// <summary>Accept the current selection</summary>
		public Command Accept { get; }

		/// <summary>The selection mode for the list</summary>
		public SelectionMode SelectionMode
		{
			get => m_list.SelectionMode;
			set => m_list.SelectionMode = value;
		}

		/// <summary>Get/Set the property to display</summary>
		public string DisplayMember
		{
			get => m_list.DisplayMemberPath;
			set => m_list.DisplayMemberPath = value;
		}

		/// <summary>The prompt text</summary>
		public string Prompt
		{
			get => (string)GetValue(PromptProperty);
			set => SetValue(PromptProperty, value);
		}
		public static readonly DependencyProperty PromptProperty = Gui_.DPRegister<ListUI>(nameof(Prompt), string.Empty, Gui_.EDPFlags.None);

		/// <summary>True if the cancel button is displayed</summary>
		public bool AllowCancel
		{
			get => (bool)GetValue(AllowCancelProperty);
			set => SetValue(AllowCancelProperty, value);
		}
		public static readonly DependencyProperty AllowCancelProperty = Gui_.DPRegister<ListUI>(nameof(AllowCancel), Boxed.False, Gui_.EDPFlags.None);

		/// <summary>Items displayed in the list</summary>
		public ObservableCollection<object> Items { get; }

		/// <summary>The item selected in the list</summary>
		public object SelectedItem => m_list.SelectedItem;

		/// <summary>The items selected in the list</summary>
		public IList SelectedItems => m_list.SelectedItems;

		/// <summary>True if the user input is valid</summary>
		public bool IsValid
		{
			get
			{
				switch (SelectionMode)
				{
				default: throw new Exception("Unknown selection mode");
				case SelectionMode.Single:
					return m_list.SelectedItem != null;
				case SelectionMode.Multiple:
				case SelectionMode.Extended:
					return m_list.SelectedItems.Count > 0;
				}
			}
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
	}
}
