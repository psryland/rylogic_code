using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace LDraw.UI
{
	public partial class SourcesListUI : UserControl, IDockable, IDisposable, INotifyPropertyChanged
	{
		public SourcesListUI(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, $"SourcesList")
			{
				ShowTitle = false,
				TabText = "Sources",
				DestroyOnClose = false,
			};
			Model = model;
			Sources = new ListCollectionView(new List<SourceItemUI>());

			AddSource = Command.Create(this, AddSourceInternal);
			OpenInEditor = Command.Create(this, OpenInEditorInternal, OpenInEditorAvailable);
			DataContext = this;
		}
		public void Dispose()
		{
			Sources = null!;
			Model = null!;
			DockControl = null!;
			GC.SuppressFinalize(this);
		}

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			get => m_dock_control;
			private set
			{
				if (m_dock_control == value) return;
				Util.Dispose(ref m_dock_control!);
				m_dock_control = value;
			}
		}
		private DockControl m_dock_control = null!;

		/// <summary>App logic</summary>
		private Model Model
		{
			get => m_model;
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.SourcesChanged -= HandleSourcesChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.SourcesChanged += HandleSourcesChanged;
				}

				void HandleSourcesChanged(object? sender, EventArgs args)
				{
					var source_list = (List<SourceItemUI>)Sources.SourceCollection;
					source_list.SyncStable(Model.Sources, (l,r) => l.ContextId == r.Source.ContextId, (s,i) => new SourceItemUI(s));
					NotifyPropertyChanged(nameof(Sources));
					Sources.Refresh();
				}
			}
		}
		private Model m_model = null!;

		/// <summary>The loaded sources</summary>
		public ICollectionView Sources
		{
			get => m_sources;
			set
			{
				if (m_sources == value) return;
				if (m_sources != null)
				{
					m_sources.CurrentChanged -= HandleCurrentSelectionChanged;
				}
				m_sources = value;
				if (m_sources != null)
				{
					m_sources.CurrentChanged += HandleCurrentSelectionChanged;
				}
				
				void HandleCurrentSelectionChanged(object? sender, EventArgs e)
				{
					OpenInEditor.NotifyCanExecuteChanged();
				}
			}
		}
		private ICollectionView m_sources = null!;

		/// <summary></summary>
		public Command AddSource { get; }
		private void AddSourceInternal()
		{
			if (Window.GetWindow(this) is MainWindow main_window)
				main_window.AddFileSource(null);
		}

		/// <summary>Open the selected source in an editor (if possible)</summary>
		public Command OpenInEditor { get; }
		private bool OpenInEditorAvailable()
		{
			return
				Sources.CurrentItem is SourceItemUI item &&
				item.Source.CanEdit;
		}
		private void OpenInEditorInternal()
		{
			try
			{
				if (Window.GetWindow(this) is MainWindow main_window &&
					Sources.CurrentItem is SourceItemUI item &&
					item.Source.CanEdit)
				{
					main_window.OpenInEditor(item.Source);
				}
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Info, ex, "Open-in-editor for this file source failed.", string.Empty, 0);
				MsgBox.Show(Window.GetWindow(this), $"Open-in-editor for this file source failed.\n{ex.Message}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Information);
			}
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}