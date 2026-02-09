using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class GridTransfers :Grid, IDockable, IDisposable, INotifyPropertyChanged
	{
		public GridTransfers(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "Transfers");
			Model = model;
			Transfers = new ListCollectionView(Array.Empty<Transfer>());

			m_grid.SelectionChanged += (s, a) =>
			{
				if (m_selecting != 0) return;
				using (Scope.Create(() => ++m_selecting, () => --m_selecting))
				{
					Model.SelectedTransfers.Clear();
					foreach (var item in a.AddedItems.Cast<Transfer>())
						Model.SelectedTransfers.Add(item);
				}
			};

			DataContext = this;
		}
		public void Dispose()
		{
			Model = null!;
			DockControl = null!;
		}
		private int m_selecting;

		/// <summary></summary>
		public Model Model
		{
			get => m_model;
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					Exchanges = CollectionViewSource.GetDefaultView(null);
					m_model.SelectedTransfers.CollectionChanged -= HandleSelectedTransfersChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.SelectedTransfers.CollectionChanged += HandleSelectedTransfersChanged;
					Exchanges = CollectionViewSource.GetDefaultView(m_model.Exchanges);
				}

				// Handlers
				void HandleSelectedTransfersChanged(object? sender, NotifyCollectionChangedEventArgs e)
				{
					if (e.Action == NotifyCollectionChangedAction.Add)
					{
						if (m_selecting != 0) return;
						using (Scope.Create(() => ++m_selecting, () => --m_selecting))
						{
							switch (e.Action)
							{
								case NotifyCollectionChangedAction.Reset:
								{
									m_grid.SelectedItems.Clear();
									break;
								}
								case NotifyCollectionChangedAction.Add:
								{
									foreach (var item in e.NewItems())
										m_grid.SelectedItems.Add(item);
									break;
								}
								case NotifyCollectionChangedAction.Remove:
								{
									foreach (var item in e.OldItems())
										m_grid.SelectedItems.Remove(item);
									break;
								}
							}
						}
					}
				}
			}
		}
		private Model m_model = null!;

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			get;
			private set
			{
				if (field == value) return;
				Util.Dispose(ref field!);
				field = value;
			}
		} = null!;

		/// <summary>The global exchanges view. Provides the source of the "Current" exchange </summary>
		private ICollectionView Exchanges
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					field.CurrentChanged -= HandleCurrentChanged;
				}
				field = value;
				if (field != null)
				{
					field.CurrentChanged += HandleCurrentChanged;
				}
				HandleCurrentChanged(null, EventArgs.Empty);

				// Handler
				void HandleCurrentChanged(object? sender, EventArgs e)
				{
					Transfers = new ListCollectionView(Array.Empty<Transfer>());

					var transfers = Exchanges?.CurrentAs<Exchange>()?.Transfers;
					if (transfers != null)
					{
						var view = new ListCollectionView(transfers);
						view.SortDescriptions.Add(new SortDescription(nameof(Transfer.Created), ListSortDirection.Descending));
						Transfers = view;

						PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Transfers)));
					}
				}
			}
		} = null!;

		/// <summary>The view of the trade history</summary>
		public ICollectionView Transfers { get; private set; }

		/// <summary>The currently selected exchange</summary>
		public Transfer Current
		{
			get => Transfers.CurrentAs<Transfer>();
			set => Transfers.MoveCurrentTo(value);
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
