using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TestWPF
{
	public class Outside
	{
		public class ImageButtonRaw : Button
		{
			private Image m_image;
			private TextBlock m_text;

			public ImageButtonRaw()
			{
				HorizontalContentAlignment = HorizontalAlignment.Left;
				Background = Brushes.Transparent;

				var panel = new StackPanel
				{
					Name = "BtnPanel",
					Orientation = Orientation.Horizontal,
				};
				panel.Children.Add(m_image = new Image
				{
					Name = "BtnImage",
					Margin = new Thickness(0, 0, 10, 0),
					Source = (ImageSource)Resources["check_reject.png"],
				});
				panel.Children.Add(m_text = new TextBlock
				{
					Name = "BtnLabel",
					VerticalAlignment = VerticalAlignment.Center,
					FontSize = 16,
					Text = "Label",
				});
				Content = panel;
			}

			/// <summary>Button text</summary>
			public string Text
			{
				get { return m_text?.Text ?? string.Empty; }
				set { if (m_text != null) m_text.Text = value; }
			}

			/// <summary>The button image</summary>
			public ImageSource Source
			{
				get { return m_image?.Source; }
				set { if (m_image != null) m_image.Source = value; }
			}

			// Dependency Props
			public static readonly DependencyProperty TextProperty = TextBlock.TextProperty;
			public static readonly DependencyProperty SourceProperty = Image.SourceProperty;
		}
	}

	internal class ImageButtonRaw : Outside.ImageButtonRaw
	{ }
}
