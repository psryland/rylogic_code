using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using Rylogic.Gui.WPF;

namespace LDraw.UI
{
	public sealed partial class AssetListUI :UserControl, IDockable, IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - An asset is a non-script geometry source.
		//  - This UI component manages a collection of assets
		//  - It's basically a 'ScriptUI' for binary files.

		public AssetListUI(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, $"AssetList")
			{
				ShowTitle = false,
				TabText = "Assets",
				//TabCMenu = TabCMenu(),
				DestroyOnClose = false,
			};
			Model = model;
			Assets = CollectionViewSource.GetDefaultView(Model.Assets);
			
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
				if (m_dock_control != null)
				{
				//	m_dock_control.ActiveChanged -= HandleActiveChanged;
				//	m_dock_control.SavingLayout -= HandleSavingLayout;
				//	Util.Dispose(ref m_dock_control!);
				}
				m_dock_control = value;
				if (m_dock_control != null)
				{
				//	m_dock_control.SavingLayout += HandleSavingLayout;
				//	m_dock_control.ActiveChanged += HandleActiveChanged;
				}

				// Handlers
				//void HandleActiveChanged(object sender, ActiveContentChangedEventArgs e)
				//{
				//	//// When activated, restore focus to the editor
				//	//if (DockControl.IsActiveContent)
				//	//	Editor.Focus();
				//	//
				//	//	Options.BkColour = args.ContentNew == this ? Color.LightSteelBlue : Color.LightGray;
				//	//	Invalidate();
				//}
				//void HandleSavingLayout(object sender, DockContainerSavingLayoutEventArgs e)
				//{
				//	if (!Model.IsTempScriptFilepath(Filepath))
				//		e.Node.Add2(nameof(Filepath), Filepath, false);
				//}
			}
		}
		private DockControl m_dock_control = null!;

		/// <summary>App logic</summary>
		public Model Model
		{
			get => m_model;
			private set
			{
				if (m_model == value) return;
				//if (m_model != null)
				//{
				//	m_model.Scenes.CollectionChanged -= HandleScenesCollectionChanged;
				//	m_model.Scripts.Remove(this);
				//}
				m_model = value;
				//if (m_model != null)
				//{
				////	// Don't add this script to m_model.Scripts, that's the caller's choice.
				////	m_model.Scenes.CollectionChanged += HandleScenesCollectionChanged;
				////	Dispatcher.BeginInvoke(() => HandleScenesCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)));
				//}

				// Handlers
				//void HandleScenesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				//{
				//	PopulateAvailableScenes();
				//}
			}
		}
		private Model m_model = null!;

		/// <summary>The loaded assets</summary>
		public ICollectionView Assets { get; }

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
