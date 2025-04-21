using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LDraw.UI
{
	public partial class SourceItemUI : UserControl, IDisposable, INotifyPropertyChanged
	{
		public SourceItemUI(Source source)
		{
			InitializeComponent();
			Source = source;
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

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
