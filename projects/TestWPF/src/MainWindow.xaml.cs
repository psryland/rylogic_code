using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using Rylogic.Extn;
using Rylogic.Gui.WPF;

namespace TestWPF
{
	/// <summary>Interaction logic for MainWindow.xaml</summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			Things = new ListCollectionView(m_things);
#if DEBUG
			PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical;
#endif
			m_recent_files.Add("One", false);
			m_recent_files.Add("Two", false);
			m_recent_files.Add("Three");
			m_recent_files.RecentFileSelected += s => Debug.WriteLine(s);

			ShowColourPicker = Command.Create(this, () =>
			{
				new ColourPickerUI().Show();
			});
			ShowChart = Command.Create(this, () =>
			{
				new ChartUI().Show();
			});
			ShowDiagram = Command.Create(this, () =>
			{
				new DiagramUI().Show();
			});
			ShowDockContainer = Command.Create(this, () =>
			{
				new DockContainerUI().Show();
			});
			ShowMsgBox = Command.Create(this, () =>
			{
				MsgBox.Show(this, "Informative isn't it", "Massage Box", MsgBox.EButtons.YesNoCancel, MsgBox.EIcon.Exclamation);
			});
			ShowListUI = Command.Create(this, () =>
			{
				var dlg = new ListUI(this)
				{
					Title = "Listing to the Left",
					Prompt = "Select anything you like",
					SelectionMode = SelectionMode.Multiple,
					AllowCancel = false,
				};
				dlg.Items.AddRange(new[] { "One", "Two", "Three", "Phooore, was that you?" });
				if (dlg.ShowDialog() == true)
					MsgBox.Show(this, $"{string.Join(",", dlg.SelectedItems.Cast<string>())}! Good Choice!", "Result", MsgBox.EButtons.OK);
			});
			ShowLogUI = Command.Create(this, () =>
			{
				var log_ui = new LogControl { LogEntryPattern = new Regex(@"^(?<Tag>.*?)\|(?<Level>.*?)\|(?<Timestamp>.*?)\|(?<Message>.*)", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled) };
				var dlg = new Window { Title = "Log UI", Content = log_ui, ResizeMode = ResizeMode.CanResizeWithGrip };
				dlg.Show();
			});
			ShowPatternEditor = Command.Create(this, () =>
			{
				new PatternEditorUI().Show();
			});
			ShowProgressUI = Command.Create(this, () =>
			{
				var dlg = new ProgressUI(this, "Test Progress", "Testicles", System.Drawing.SystemIcons.Exclamation.ToBitmapSource(), CancellationToken.None, (u, _, p) =>
				{
					for (int i = 0, iend = 100; !u.CancelPending && i != iend; ++i)
					{
						p(new ProgressUI.UserState
						{
							Description = $"Testicle: {i / 10}",
							FractionComplete = 1.0 * i / iend,
							ProgressBarText = $"I'm up to {i}/{iend}",
						});
						Thread.Sleep(100);
					}
				})
				{ AllowCancel = true };

				// Modal
				using (dlg)
				{
					var res = dlg.ShowDialog(500);
					if (res == true) MessageBox.Show("Completed");
					if (res == false) MessageBox.Show("Cancelled");
				}

				// Non-modal
				//dlg.Show();
			});
			ShowPromptUI = Command.Create(this, () =>
			{
				var dlg = new PromptUI(this)
				{
					Title = "Prompting isn't it...",
					Prompt = "I'll have what she's having. Really long message\r\nwith new lines in and \r\n other stuff\r\n\r\nEnter a positive number",
					Value = "Really long value as well, that hopefully wraps around",
					Units = "kgs",
					Validate = x => double.TryParse(x, out var v) && v >= 0 ? ValidationResult.ValidResult : new ValidationResult(false, "Enter a positive number"),
					Image = FindResource("pencil") as BitmapImage,
					MultiLine = true,
				};
				if (dlg.ShowDialog() == true)
					double.Parse(dlg.Value);
			});
			ShowToolWindow = Command.Create(this, () =>
			{
				new ToolUI { Owner = this }.Show();
			});
			ScintillaUI = Command.Create(this, () =>
			{
				new ScintillaUI().Show();
			});
			ShowView3DUI = Command.Create(this, () =>
			{
				new View3dUI().Show();
			});
			Exit = Command.Create(this, Close);
			DataContext = this;
		}

		public Command ShowColourPicker { get; }
		public Command ShowChart { get; }
		public Command ShowDiagram { get; }
		public Command ShowDockContainer { get; }
		public Command ShowMsgBox { get; }
		public Command ShowListUI { get; }
		public Command ShowLogUI { get; }
		public Command ShowPatternEditor { get; }
		public Command ShowProgressUI { get; }
		public Command ShowPromptUI { get; }
		public Command ShowToolWindow { get; }
		public Command ScintillaUI { get; }
		public Command ShowView3DUI { get; }
		public Command Exit { get; }

		/// <summary>Some strings</summary>
		public ICollectionView Things { get; }

		/// <summary>Modify the strings collection</summary>
		private void ComboBoxAutoComplete_Update(object sender, EventArgs e)
		{
			var cb = (ComboBox)sender;
			var view = (ICollectionView)cb.ItemsSource;
			var list = (List<Thing>)view.SourceCollection;

			list.Clear();
			list.AddRange(m_things.Where(x => x.Name.Contains(cb.Text)));
			view.Refresh();
		}
		private List<Thing> m_things = new List<Thing> { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten" };

		/// <summary></summary>
		[DebuggerDisplay("{Name,nq}")]
		private class Thing
		{
			public Thing(string name)
			{
				Name = name;
			}
			public string Name { get; }
			public static implicit operator Thing(string name) { return new Thing(name); }
		}

		private void HandleReorderRowDrop(object sender, DataGrid_.ReorderRowDropEventArgs e)
		{

		}
	}
}
