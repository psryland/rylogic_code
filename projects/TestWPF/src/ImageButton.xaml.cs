using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TestWPF
{
	/// <summary>Interaction logic for ImageButton.xaml</summary>
	public partial class ImageButton : UserControl
	{
		public ImageButton()
		{
			InitializeComponent();
		}

		/// <summary>Button text</summary>
		public string Text
		{
			get { return BtnLabel?.Text ?? string.Empty; }
			set { if (BtnLabel != null) BtnLabel.Text = value; }
		}

		/// <summary>The button image</summary>
		public ImageSource Source
		{
			get => BtnImage.Source;
			set { if (BtnImage != null) BtnImage.Source = value; }
		}

		// Dependency Props
		public static readonly DependencyProperty TextProperty = TextBlock.TextProperty;
		public static readonly DependencyProperty SourceProperty = Image.SourceProperty;
	}
}
