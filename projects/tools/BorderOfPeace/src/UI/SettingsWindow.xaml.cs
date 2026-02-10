using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BorderOfPeace.Model;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;

namespace BorderOfPeace.UI
{
	public partial class SettingsWindow : Window
	{
		/// <summary>Bindable wrapper for a ColorPreset</summary>
		public class PresetVM
		{
			public string Name { get; set; } = string.Empty;
			public uint Argb { get; set; }
			public Color WpfColor => Color.FromArgb(
				(byte)((Argb >> 24) & 0xFF),
				(byte)((Argb >> 16) & 0xFF),
				(byte)((Argb >> 8) & 0xFF),
				(byte)(Argb & 0xFF));

			public PresetVM()
			{
			}
			public PresetVM(ColorPreset p)
			{
				Name = p.Name;
				Argb = p.Argb;
			}

			public ColorPreset ToPreset() => new(Name, Argb);
		}

		public ObservableCollection<PresetVM> Presets { get; } = new();

		/// <summary>The resulting settings if the user clicks OK</summary>
		public Settings ResultSettings { get; private set; }

		public SettingsWindow(Settings settings)
		{
			ResultSettings = settings;
			InitializeComponent();

			foreach (var p in settings.Presets)
				Presets.Add(new PresetVM(p));

			m_list_presets.ItemsSource = Presets;
		}

		private void HandleAdd(object sender, RoutedEventArgs e)
		{
			var dlg = new ColourPickerUI(this, new Colour32(0xFF, 0x80, 0x80, 0x80)) { Width = 440, Height = 400 };
			if (dlg.ShowDialog() == true)
			{
				var colour = dlg.Colour;
				var name = $"#{colour.R:X2}{colour.G:X2}{colour.B:X2}";
				Presets.Add(new PresetVM { Name = name, Argb = colour.ARGB });
			}
		}

		private void HandleEdit(object sender, RoutedEventArgs e)
		{
			if (m_list_presets.SelectedItem is not PresetVM vm)
				return;

			var dlg = new ColourPickerUI(this, new Colour32(vm.Argb)) { Width = 440, Height = 400 };
			if (dlg.ShowDialog() == true)
			{
				var colour = dlg.Colour;
				vm.Argb = colour.ARGB;
				vm.Name = $"#{colour.R:X2}{colour.G:X2}{colour.B:X2}";

				// Refresh the list
				var idx = Presets.IndexOf(vm);
				Presets[idx] = new PresetVM { Name = vm.Name, Argb = vm.Argb };
			}
		}

		private void HandleRemove(object sender, RoutedEventArgs e)
		{
			if (m_list_presets.SelectedItem is not PresetVM vm)
				return;

			Presets.Remove(vm);
		}

		private void HandleDefaults(object sender, RoutedEventArgs e)
		{
			Presets.Clear();
			foreach (var p in Settings.DefaultPresets())
				Presets.Add(new PresetVM(p));
		}

		private void HandleOK(object sender, RoutedEventArgs e)
		{
			ResultSettings = new Settings
			{
				Presets = Presets.Select(vm => vm.ToPreset()).ToList(),
			};
			DialogResult = true;
		}
	}
}
