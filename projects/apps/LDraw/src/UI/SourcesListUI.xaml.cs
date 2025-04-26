using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		public ICollectionView Sources { get; }

		/// <summary></summary>
		public Command AddSource { get; }
		private void AddSourceInternal()
		{
			try
			{
			}
			catch (Exception)
			{
			}
			//LoadFile();
		}

		/// <summary>Open the selected source in an editor (if possible)</summary>
		public Command OpenInEditor { get; }
		private bool OpenInEditorAvailable()
		{
			return
				Sources.CurrentItem is SourceItemUI item &&
				item.CanEdit();
		}
		private void OpenInEditorInternal()
		{
			try
			{
			}
			catch (Exception)
			{
			}
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}