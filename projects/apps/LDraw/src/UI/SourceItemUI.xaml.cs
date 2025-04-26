using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace LDraw.UI
{
	public partial class SourceItemUI : UserControl, IDisposable, INotifyPropertyChanged
	{
		public SourceItemUI(Source source)
		{
			InitializeComponent();
			Source = source;

			RemoveSource = Command.Create(this, RemoveSourceInternal);
			DataContext = this;
		}
		public void Dispose()
		{
			// Remove objects from all scenes
		//todo	Context.SelectedScenes = Array.Empty<SceneUI>();
		//todo	Model.Sources.Remove(this);

		//todo	Context = null!;
			GC.SuppressFinalize(this);
		}

		/// <summary>The source this item represents</summary>
		public Source Source { get; }

		/// <summary>Full "filepath" of the source (might be an IP:Port or something else)</summary>
		public string FilePath => Source.Name; //todo

		/// <summary>App logic</summary>
		//todo private Model Model => Context.Model;

		public bool CanEdit() => false; //todo: needs an associated text file

		/// <summary>Remove this source from the Model</summary>
		public Command RemoveSource { get; }
		private void RemoveSourceInternal()
		{
			try
			{
				Source.Remove();
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Info, ex, "Error removing source", string.Empty, 0);
				MsgBox.Show(Window.GetWindow(this), $"Error removing source.\n{ex.Message}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Information);
			}
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
