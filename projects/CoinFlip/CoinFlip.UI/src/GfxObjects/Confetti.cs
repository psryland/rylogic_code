using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CoinFlip.Settings;
using Rylogic.Extn;
using Rylogic.Extn.Windows;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;

namespace CoinFlip.UI.GfxObjects
{
	public class Confetti :IDisposable
	{
		// Notes:
		//  - Confetti manages graphics per item displayed on a chart.
		//  - Typically, the graphics include an icon and a description string

		private Dictionary<IItem, GfxItem> m_gfx;

		public Confetti()
		{
			m_gfx = new Dictionary<IItem, GfxItem>();
			Position = x => v4.Origin;
		}
		public void Dispose()
		{
			ClearScene();
		}

		/// <summary>Callback function used to get the position of an item (in chart space)</summary>
		public Func<IItem, v4> Position { get; set; }

		/// <summary>Remove all graphics from the scene</summary>
		public void ClearScene()
		{
			foreach (var gfx in m_gfx)
				gfx.Value.Dispose();

			m_gfx.Clear();
		}

		/// <summary>Update the scene for the given transfers</summary>
		public void BuildScene(IEnumerable<IItem> items, IEnumerable<IItem> highlighted, ChartControl chart)
		{
			var in_scene = items.ToHashSet(0);
			var highlight = highlighted?.ToHashSet(0) ?? new HashSet<IItem>();

			// Remove all gfx that is no longer in the scene
			var remove = m_gfx.Where(kv => !in_scene.Contains(kv.Key)).ToList();
			foreach (var gfx in remove)
			{
				m_gfx.Remove(gfx.Key);
				gfx.Value.Dispose();
			}

			// Add graphics objects for each item in the scene
			foreach (var item in items)
			{
				// Get the associated graphics objects
				var gfx = m_gfx.GetOrAdd(item, k => new GfxItem(this, item));

				// Update the position
				var pt = Position(item);
				var s = chart.ChartToScene(pt);
				var hl = highlight.Contains(item);
				gfx.Update(chart.Overlay, s.ToPointD(), hl);
			}
		}

		/// <summary>Raised when an item is selected</summary>
		public event EventHandler<ItemEventArgs> ItemSelected;
		private void NotifySelected(IItem item)
		{
			ItemSelected?.Invoke(this, new ItemEventArgs(item));
		}

		/// <summary>Raised when an item is double clicked</summary>
		public event EventHandler<ItemEventArgs> EditItem;
		private void NotifyEdit(IItem item)
		{
			EditItem?.Invoke(this, new ItemEventArgs(item));
		}

		/// <summary>The item for which confetti is being displayed for</summary>
		public interface IItem
		{
			/// <summary>The icon representation of the item</summary>
			Polygon Icon { get; }

			/// <summary>The string description to display next to the icon</summary>
			TextBlock Label { get; }

			/// <summary>The tint colour for the icon and label</summary>
			Colour32 Colour { get; }
		}

		/// <summary>Graphics for one Confet</summary>
		private class GfxItem :IDisposable
		{
			private readonly Confetti m_owner;
			private readonly IItem m_item;
			public GfxItem(Confetti owner, IItem item)
			{
				m_owner = owner;
				m_item = item;

				Icon = m_item.Icon;
				Label = m_item.Label;
				Icon.MouseLeftButtonDown += HandleMouseLeftButtonDown;
			}
			public void Dispose()
			{
				Icon.MouseLeftButtonDown -= HandleMouseLeftButtonDown;
				Icon.Detach();
				Label.Detach();
			}

			/// <summary>The icon representation of the item</summary>
			private Polygon Icon { get; }

			/// <summary>The string description to display next to the icon</summary>
			private TextBlock Label { get; }

			/// <summary>The tint colour for the icon and label</summary>
			private Colour32 Colour => m_item.Colour;

			/// <summary>Resize and reposition the graphics</summary>
			public void Update(Canvas overlay, Point s, bool highlight)
			{
				Icon.Fill = Colour.ToMediaBrush();
				Icon.RenderTransform = new MatrixTransform(1, 0, 0, 1, s.X, s.Y);
				overlay.Adopt(Icon);

				Label.Foreground = Colour.ToMediaBrush();
				Label.Visibility = SettingsData.Settings.Chart.ConfettiDescriptionsVisible ? Visibility.Visible : Visibility.Collapsed;
				Label.FontSize = SettingsData.Settings.Chart.ConfettiLabelSize;
				Label.FontWeight = highlight ? FontWeights.Bold : FontWeights.Normal;
				Label.Measure(Rylogic.Extn.Windows.Size_.Infinity);
				Label.Background = new SolidColorBrush(Colour32.White.Alpha(1.0f - (float)SettingsData.Settings.Chart.ConfettiLabelTransparency).ToMediaColor());
				Label.RenderTransform = SettingsData.Settings.Chart.ConfettiLabelsToTheLeft
					? new MatrixTransform(1, 0, 0, 1, s.X - Label.DesiredSize.Width - 7.0, s.Y - Label.DesiredSize.Height / 2)
					: new MatrixTransform(1, 0, 0, 1, s.X + 7.0, s.Y - Label.DesiredSize.Height / 2);
				overlay.Adopt(Label);
			}

			/// <summary>Handle selection and double clicks</summary>
			private void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
			{
				if (e.ClickCount == 2)
					m_owner.NotifyEdit(m_item);
				else if (e.ClickCount == 1)
					m_owner.NotifySelected(m_item);
			}
		}

		/// <summary>Event args for 'OrderGfx' interactions</summary>
		public class ItemEventArgs :EventArgs
		{
			public ItemEventArgs(IItem item)
			{
				Item = item;
			}

			/// <summary>The selected item</summary>
			public IItem Item { get; }
		}
	}
}
