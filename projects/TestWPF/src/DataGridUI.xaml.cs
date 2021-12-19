﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Rylogic.Gui.WPF;

namespace TestWPF
{
	public partial class DataGridUI :Window
	{
		public DataGridUI()
		{
			InitializeComponent();
			Things = new List<Thing>
			{
				new Thing("One"  , 1.0),
				new Thing("Two"  , 2.0),
				new Thing("Three", 3.0),
				new Thing("Four" , 4.0),
				new Thing("Five" , 5.0),
				new Thing("Six"  , 6.0),
				new Thing("Seven", 7.0),
				new Thing("Eight", 8.0),
				new Thing("Nine" , 9.0),
				new Thing("Ten"  , 10.0),
			};
			ThingsView = new ListCollectionView(Things);
			DataContext = this;
		}

		/// <summary>Some strings</summary>
		public ICollectionView ThingsView { get; }
		private List<Thing> Things { get; }

		/// <summary>Comma separated list of selected things</summary>
		public string SelectedDescription => string.Join(",", Things.Where(x => x.IsChecked).Select(x => x.Name));

		/// <summary></summary>
		private void HandleReorderRowDrop(object sender, DataGrid_.ReorderRowDropEventArgs args)
		{

		}

		/// <summary></summary>
		[DebuggerDisplay("{Name,nq}")]
		private class Thing
		{
			public Thing(string name, double value)
			{
				Name = name;
				Value = value;
			}
			public string Name { get; }
			public double Value { get; set; }
			public bool IsChecked { get; set; }
		}
	}
}
