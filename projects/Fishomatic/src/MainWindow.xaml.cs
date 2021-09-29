using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Rylogic.Extn.Windows;
using Rylogic.Gui.WPF;
using Rylogic.Utility;
using Rectangle = System.Drawing.Rectangle;

namespace Fishomatic
{
	public partial class MainWindow :Window
	{
		public MainWindow()
		{
			try
			{
				InitializeComponent();
				Settings = new Settings(SettingsFilepath);
				FishFinder = new FishFinder(Settings);

				SetSearchArea = Command.Create(this, SetSearchAreaInternal);
				ShowOptions = Command.Create(this, ShowOptionsInternal);
				ToggleFishing = Command.Create(this, ToggleFishingInternal);

				Closed += delegate { Util.Dispose(FishFinder!); };
				DataContext = this;
			}
			catch (Exception)
			{
				throw;
			}
		}

		/// <summary></summary>
		public Settings Settings { get; }

		/// <summary></summary>
		public FishFinder FishFinder { get; }

		/// <summary></summary>
		public ICommand SetSearchArea { get; }
		private void SetSearchAreaInternal()
		{
			// Get the search area relative to the target window
			var wrect = (FishFinder.TargetWnd?.WindowRectangle ?? new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight)).ToRectD();
			var area = Settings.LargeSearchArea(wrect);

			// 'offset' is the position of the mouse relative to 'frame'
			var offset = (Point?)null;
			var frame = new Window
			{
				Owner = this,
				Title = "Bobber search area",
				ShowInTaskbar = true,
				ShowActivated = false,
				AllowsTransparency = true,
				WindowStyle = WindowStyle.None,
				WindowStartupLocation = WindowStartupLocation.Manual,
				ResizeMode = ResizeMode.CanResizeWithGrip,
				Visibility = Visibility.Collapsed,
				Background = new SolidColorBrush(Color.FromArgb(0xD0, 0x80, 0x80, 0xff)),
				Left = area.Left,
				Top = area.Top,
				Width = area.Width,
				Height = area.Height,
				Opacity = 0.5,
			};
			frame.MouseDown += delegate (object i, MouseButtonEventArgs e)
			{
				if (e.LeftButton != MouseButtonState.Pressed) return;
				offset = e.GetPosition(frame);
				frame.CaptureMouse();
				e.Handled = true;
			};
			frame.MouseMove += delegate (object i, MouseEventArgs e)
			{
				if (offset == null) return;
				var pos = frame.PointToScreen(e.GetPosition(frame));
				frame.Left = pos.X - offset.Value.X;
				frame.Top = pos.Y - offset.Value.Y;
				e.Handled = true;
			};
			frame.MouseUp += delegate (object i, MouseButtonEventArgs e)
			{
				frame.ReleaseMouseCapture();
				offset = null;
			};
			frame.KeyDown += delegate (object i, KeyEventArgs e)
			{
				if (e.Key == Key.Return)
					frame.DialogResult = true;
				if (e.Key == Key.Escape)
					frame.DialogResult = false;
			};
			frame.ShowDialog();

			// Record the search area
			if (frame.DialogResult == true)
				Settings.SearchArea = Rect_.ToThickness(wrect, frame.Bounds());

			//CaptureScreen(ref m_small_grab, m_props.SearchArea);
		}

		/// <summary></summary>
		public ICommand ShowOptions { get; }
		private void ShowOptionsInternal()
		{
		}

		/// <summary>Enable/Disable fishing</summary>
		public ICommand ToggleFishing { get; }
		private void ToggleFishingInternal()
		{
			FishFinder.Run = !FishFinder.Run;
		}

		/// <summary></summary>
		private static string SettingsFilepath => Util.ResolveUserDocumentsPath("Rylogic", "Fishomatic", "Settings.xml");
	}
}
