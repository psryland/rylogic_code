using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

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
				DestroyOnClose = false,
			};
			Model = model;
			Assets = CollectionViewSource.GetDefaultView(Model.Assets);
			
			AddAsset = Command.Create(this, AddAssetInternal);
			DataContext = this;
		}
		public void Dispose()
		{
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
		private Model Model { get; }

		/// <summary>The loaded assets</summary>
		public ICollectionView Assets { get; }

		/// <summary>Load an asset from a file</summary>
		public void LoadFile(string? filepath = null, IList<SceneUI>? scenes = null)
		{
			// Prompt for a filepath if not given
			if (filepath == null || filepath.Length == 0)
			{
				var dlg = new OpenFileDialog { Title = "Load Script", Filter = Model.AssetFilesFilter };
				if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;
				filepath = dlg.FileName ?? throw new Exception("Invalid filepath selected");
			}

			// Load the asset file
			var name = Path_.FileName(filepath);
			var asset = Model.Assets.Add2(new AssetUI(Model, name, filepath, Guid.NewGuid()));
			if (scenes != null) asset.Context.SelectedScenes = scenes;
		}

		/// <summary></summary>
		public Command AddAsset { get; }
		private void AddAssetInternal()
		{
			LoadFile();
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
