using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TestWPF
{
	public partial class TreeViewMSUI :Window
	{
		public TreeViewMSUI()
		{
			InitializeComponent();
		}

		private void GetSelectionsButton_OnClick(object sender, RoutedEventArgs e)
		{
			var selectedMesg = "";
			var selectedItems = multiSelectTreeView.SelectedItems;

			if (selectedItems.Count > 0)
			{
				selectedMesg = selectedItems.Cast<FoodItem>()
					.Where(modelItem => modelItem != null)
					.Aggregate(selectedMesg, (current, modelItem) => current + modelItem.Name + Environment.NewLine);
			}
			else
				selectedMesg = "No selected items!";

			MessageBox.Show(selectedMesg, "MultiSelect TreeView Demo", MessageBoxButton.OK);
		}
	}


	public sealed class DemoViewModel
	{
		public DemoViewModel()
		{
			var redMeat = new FoodItem { Name = "Reds" };
			redMeat.Add(new FoodItem { Name = "Beef" });
			redMeat.Add(new FoodItem { Name = "Buffalo" });
			redMeat.Add(new FoodItem { Name = "Lamb" });

			var whiteMeat = new FoodItem { Name = "Whites" };
			whiteMeat.Add(new FoodItem { Name = "Chicken" });
			whiteMeat.Add(new FoodItem { Name = "Duck" });
			whiteMeat.Add(new FoodItem { Name = "Pork" });
			var meats = new FoodItem { Name = "Meats", Children = { redMeat, whiteMeat } };

			var veggies = new FoodItem { Name = "Vegetables" };
			veggies.Add(new FoodItem { Name = "Potato" });
			veggies.Add(new FoodItem { Name = "Corn" });
			veggies.Add(new FoodItem { Name = "Spinach" });

			var fruits = new FoodItem { Name = "Fruits" };
			fruits.Add(new FoodItem { Name = "Apple" });
			fruits.Add(new FoodItem { Name = "Orange" });
			fruits.Add(new FoodItem { Name = "Pear" });

			FoodGroups = new ObservableCollection<FoodItem> { meats, veggies, fruits };
		}
		public ObservableCollection<FoodItem> FoodGroups { get; set; }
	}
	public sealed class FoodItem
	{
		public string Name { get; set; } = string.Empty;
		public ObservableCollection<FoodItem> Children { get; set; } = new ObservableCollection<FoodItem>();
		public void Add(FoodItem item) => Children.Add(item);
	}
}
