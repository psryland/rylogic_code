﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WPF;

namespace Rylogic.TextAligner
{
	public partial class AlignOptionsUI :UserControl, INotifyPropertyChanged
	{
		private readonly AlignOptions m_options;
		internal AlignOptionsUI(AlignOptions options)
		{
			InitializeComponent();
			m_options = options;
			GroupsView = new ListCollectionView(m_options.Groups);
			PatternsView = new ListCollectionView(Array.Empty<AlignGroup>());

			ShowHelp        = Command.Create(this, ShowHelpInternal);
			ResetToDefaults = Command.Create(this, ResetToDefaultsInternal);
			EditPattern     = Command.Create(this, EditPatternInternal);
			AddGroup        = Command.Create(this, AddGroupInternal);
			DelGroup        = Command.Create(this, DelGroupInternal, DelGroupAvailable);
			MoveGroupUp     = Command.Create(this, MoveGroupUpInternal, MoveGroupUpAvailable);
			MoveGroupDown   = Command.Create(this, MoveGroupDownInternal, MoveGroupDownAvailable);
			AddPattern      = Command.Create(this, AddPatternInternal);
			DelPattern      = Command.Create(this, DelPatternInternal, DelPatternAvailable);
			MovePatternUp   = Command.Create(this, MovePatternUpInternal, MovePatternUpAvailable);
			MovePatternDown = Command.Create(this, MovePatternDownInternal, MovePatternDownAvailable);

			m_options.PropertyChanged -= HandlePropertyChanged;
			m_options.PropertyChanged += HandlePropertyChanged;

			DialogKeyPending += HandleKey;
			m_root.DataContext = this;

			void HandleKey(object sender, RoutedEventArgs args)
			{
				args.Handled = true;
			}
			void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				NotifyPropertyChanged(e.PropertyName);
			}
		}

		/// <summary></summary>
		private event RoutedEventHandler? DialogKeyPending
		{
			add { AddHandler(UIElementDialogPage.DialogKeyPendingEvent, value); }
			remove { RemoveHandler(UIElementDialogPage.DialogKeyPendingEvent, value); }
		}

		/// <summary>Binding view of the alignment groups</summary>
		public ICollectionView GroupsView
		{
			get => m_groups_view;
			private set
			{
				if (m_groups_view == value) return;
				if (m_groups_view != null)
				{
					m_groups_view.CurrentChanged -= HandleCurrentChanged;
				}
				m_groups_view = value;
				if (m_groups_view != null)
				{
					m_groups_view.CurrentChanged += HandleCurrentChanged;
				}
				NotifyPropertyChanged(nameof(GroupsView));
				HandleCurrentChanged(null, null);

				// Handlers
				void HandleCurrentChanged(object? sender, EventArgs? e)
				{
					MoveGroupUp?.NotifyCanExecuteChanged();
					MoveGroupDown?.NotifyCanExecuteChanged();

					var gidx = GroupsView.CurrentPosition;
					if (gidx >= 0 && gidx < m_options.Groups.Count)
						PatternsView = new ListCollectionView(m_options.Groups[gidx].Patterns);
					else
						PatternsView = new ListCollectionView(Array.Empty<AlignPattern>());
				}
			}
		}
		private ICollectionView m_groups_view = null!;

		/// <summary>Binding view of the alignment patterns within the current group</summary>
		public ICollectionView PatternsView
		{
			get => m_patterns_view;
			private set
			{
				if (m_patterns_view == value) return;
				if (m_patterns_view != null)
				{
					m_patterns_view.CurrentChanged -= HandleCurrentChanged;
				}
				m_patterns_view = value;
				if (m_patterns_view != null)
				{
					m_patterns_view.CurrentChanged += HandleCurrentChanged;
				}
				NotifyPropertyChanged(nameof(PatternsView));
				HandleCurrentChanged(null, null);

				// Handlers
				void HandleCurrentChanged(object? sender, EventArgs? e)
				{
					MovePatternUp?.NotifyCanExecuteChanged();
					MovePatternDown?.NotifyCanExecuteChanged();
				}
			}
		}
		private ICollectionView m_patterns_view = null!;

		/// <summary>Alignment style</summary>
		public EAlignCharacters AlignStyle
		{
			get => m_options.AlignStyle;
			set => m_options.AlignStyle = value;
		}

		/// <summary>A pattern for ignoring lines during alignment</summary>
		public AlignPattern LineIgnorePattern
		{
			get => m_options.LineIgnorePattern;
			set => m_options.LineIgnorePattern = value;
		}
		
		/// <summary>Show help information</summary>
		public Command ShowHelp { get; }
		private void ShowHelpInternal()
		{
			new HelpUI(this).ShowDialog();
		}

		/// <summary>Reset alignment patterns to their defaults</summary>
		public Command ResetToDefaults { get; }
		private void ResetToDefaultsInternal()
		{
			m_options.ResetSettings();
			GroupsView.Refresh();
		}

		/// <summary>Edit a selected pattern</summary>
		public Command EditPattern { get; }
		private void EditPatternInternal(object? pattern)
		{
			if (pattern is not AlignPattern p)
				return;

			// Show the pattern editor for 'p'
			var dlg = new PatternEditorUI(this, p);
			dlg.ShowDialog();

			// Notify all properties changed
			p.NotifyPropertyChanged(null);
		}

		/// <summary>Add another alignment group</summary>
		public Command AddGroup { get; }
		private void AddGroupInternal()
		{
			var grp = new AlignGroup("<New Group>", 0);
			if (GroupsView.CurrentItem != null)
				m_options.Groups.Insert(GroupsView.CurrentPosition, grp);
			else
				m_options.Groups.Add(grp);

			GroupsView.MoveCurrentTo(grp);
		}

		/// <summary>Remove the selected alignment groups</summary>
		public Command DelGroup { get; }
		private void DelGroupInternal()
		{
			var grp = GroupsView.CurrentAs<AlignGroup>();
			if (grp != null) m_options.Groups.Remove(grp);
		}
		private bool DelGroupAvailable()
		{
			return GroupsView.CurrentItem != null;
		}

		/// <summary>Move the selected group up</summary>
		public Command MoveGroupUp { get; }
		private void MoveGroupUpInternal()
		{
			var gidx = GroupsView.CurrentPosition;
			if (gidx > 0 && gidx < m_options.Groups.Count)
			{
				m_options.Groups.Swap(gidx, gidx - 1);
				GroupsView.MoveCurrentToPosition(gidx - 1);
			}
		}
		private bool MoveGroupUpAvailable()
		{
			var gidx = GroupsView.CurrentPosition;
			return gidx > 0 && gidx < m_options.Groups.Count;
		}

		/// <summary>Move the selected group down</summary>
		public Command MoveGroupDown { get; }
		private void MoveGroupDownInternal()
		{
			var gidx = GroupsView.CurrentPosition;
			if (gidx >= 0 && gidx < m_options.Groups.Count - 1)
			{
				m_options.Groups.Swap(gidx, gidx + 1);
				GroupsView.MoveCurrentToPosition(gidx + 1);
			}
		}
		private bool MoveGroupDownAvailable()
		{
			var gidx = GroupsView.CurrentPosition;
			return gidx >= 0 && gidx < m_options.Groups.Count - 1;
		}

		/// <summary>Add another alignment pattern</summary>
		public Command AddPattern { get; }
		private void AddPatternInternal()
		{
			var pat = new AlignPattern(EPattern.Substring, string.Empty);
			var patterns = (ObservableCollection<AlignPattern>)PatternsView.SourceCollection;
			if (PatternsView.CurrentItem != null)
				patterns.Insert(PatternsView.CurrentPosition, pat);
			else
				patterns.Add(pat);

			PatternsView.MoveCurrentTo(pat);
		}

		/// <summary>Remove the selected alignment pattern</summary>
		public Command DelPattern { get; }
		private void DelPatternInternal()
		{
			var pat = PatternsView.CurrentAs<AlignPattern>();
			var patterns = (ObservableCollection<AlignPattern>)PatternsView.SourceCollection;
			if (pat != null) patterns.Remove(pat);
		}
		private bool DelPatternAvailable()
		{
			return PatternsView.CurrentItem != null;
		}

		/// <summary>Move the selected pattern up</summary>
		public Command MovePatternUp { get; }
		private void MovePatternUpInternal()
		{
			var gidx = GroupsView.CurrentPosition;
			if (gidx < 0 || gidx >= m_options.Groups.Count)
				return;

			var pidx = PatternsView.CurrentPosition;
			var patterns = m_options.Groups[gidx].Patterns;
			if (pidx > 0 && pidx < patterns.Count)
			{
				patterns.Swap(pidx, pidx - 1);
				PatternsView.MoveCurrentToPosition(pidx - 1);
			}
		}
		private bool MovePatternUpAvailable()
		{
			var gidx = GroupsView.CurrentPosition;
			var pidx = PatternsView.CurrentPosition;
			return
				gidx >= 0 && gidx < m_options.Groups.Count &&
				pidx > 0 && pidx < m_options.Groups[gidx].Patterns.Count;
		}

		/// <summary>Move the selected pattern down</summary>
		public Command MovePatternDown { get; }
		private void MovePatternDownInternal()
		{
			var gidx = GroupsView.CurrentPosition;
			if (gidx < 0 || gidx >= m_options.Groups.Count)
				return;

			var pidx = PatternsView.CurrentPosition;
			var patterns = m_options.Groups[gidx].Patterns;
			if (pidx >= 0 && pidx < patterns.Count - 1)
			{
				patterns.Swap(pidx, pidx + 1);
				PatternsView.MoveCurrentToPosition(pidx + 1);
			}
		}
		private bool MovePatternDownAvailable()
		{
			var gidx = GroupsView.CurrentPosition;
			var pidx = PatternsView.CurrentPosition;
			return
				gidx >= 0 && gidx < m_options.Groups.Count &&
				pidx >= 0 && pidx < m_options.Groups[gidx].Patterns.Count - 1;
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string? prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary></summary>
		private void MouseDoubleClick_AlignmentPatterns(object sender, MouseButtonEventArgs e)
		{
			var cell = DataGrid_.FindCell((DependencyObject)e.OriginalSource);
			if (cell != null && cell.Column.Header is string header && header == "Pattern" && cell.DataContext is AlignPattern pattern)
			{
				EditPattern.Execute(pattern);
				e.Handled = true;
			}
		}
	}
}
