using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Rylogic.Gui.WPF;

namespace TestWPF
{
	public partial class TreeGridUI : Window
	{
		public TreeGridUI()
		{
			InitializeComponent();
			Data = new List<Datum>();
			DataView = CollectionViewSource.GetDefaultView(Data);
			GenerateData(10);
			GenData = Command.Create(this, GenDataInternal);
			AddItem = Command.Create(this, AddItemInternal);
			RemoveItem = Command.Create(this, RemoveItemInternal);
			DataContext = this;
		}

		/// <summary>Populate 'Data'</summary>
		private void GenerateData(int count)
		{
			Data.Clear();
			if (count >= 1) Data.Add(new Datum("Root1", "Root1", 1, new List<Datum>
				{
					new Datum("Branch11", "Root1=>Branch11", 11, new List<Datum>
					{
						new Datum("Leaf111", "Root1=>Branch11=>Leaf111", 111),
						new Datum("Leaf112", "Root1=>Branch11=>Leaf112", 112),
						new Datum("Leaf113", "Root1=>Branch11=>Leaf113", 113),
					}),
					new Datum("Branch12", "Root1=>Branch12", 12, new List<Datum>
					{
						new Datum("Leaf121", "Root1=>Branch12=>Leaf121", 121),
						new Datum("Leaf122", "Root1=>Branch12=>Leaf122", 122),
						new Datum("Leaf123", "Root1=>Branch12=>Leaf123", 123),
					}),
				}));
			if (count >= 2) Data.Add(new Datum("Root2", "Root2", 2, new List<Datum>
				{
					new Datum("Branch21", "Root2=>Branch21", 21, new List<Datum>
					{
						new Datum("Leaf211", "Root2=>Branch21=>Leaf211", 211),
						new Datum("Leaf212", "Root2=>Branch21=>Leaf212", 212),
					}),
					new Datum("Branch22", "Root2=>Branch22", 22, new List<Datum>
					{
						new Datum("Leaf221", "Root2=>Branch22=>Leaf221", 221),
						new Datum("Leaf222", "Root2=>Branch22=>Leaf222", 222),
						new Datum("Leaf223", "Root2=>Branch22=>Leaf223", 223),
					}),
					new Datum("Branch23", "Root2=>Branch23", 23),
				}));
			if (count >= 3) Data.Add(new Datum("Root3", "Root3", 3));
			if (count >= 4) Data.Add(new Datum("Root4", "Root4", 4, new List<Datum>
				{
					new Datum("Branch41", "Root4=>Branch41", 41),
				}));
			DataView.Refresh();
		}

		/// <summary>Tree data</summary>
		public List<Datum> Data { get; }

		/// <summary>Tree data view</summary>
		public ICollectionView DataView { get; }

		/// <summary>Generate data</summary>
		public ICommand GenData { get; }
		private void GenDataInternal()
		{
			GenerateData(m_rng.Next(1, 4));
		}
		private Random m_rng = new();

		/// <summary>Dynamically change the data</summary>
		public ICommand AddItem { get; }
		private void AddItemInternal()
		{
			if (Data.Count != 4) return;
			Data[3].Child.Add(new Datum("Branch42", "Root4=>Branch42", 42));
			Data.Add(new Datum("Root5", "Root5", 5, new List<Datum>
			{
				new Datum("Branch51", "Root5=>Branch51", 51),
			}));
			DataView.Refresh();
		}

		/// <summary>Dynamically change the data</summary>
		public ICommand RemoveItem { get; }
		private void RemoveItemInternal()
		{
			if (Data.Count != 5) return;
			Data.RemoveAt(4);
			Data[3].Child.RemoveAt(1);
			DataView.Refresh();
		}

		/// <summary>Row elements</summary>
		[DebuggerDisplay("{Name,nq}")]
		public class Datum
		{
			public Datum(string name, string desc, int value, List<Datum>? children = null)
			{ 
				Name = name;
				Description = desc;
				Image = (BitmapImage)Application.Current.Resources[(value & 1) == 0 ? "one" : "two"];
				Value = value;
				Child = children ?? new List<Datum>();
			}

			public string Name { get; set; }

			public BitmapImage Image { get; set; }

			public string Description { get; set; }

			public int Value { get; set; }

			public List<Datum> Child { get; set; }
		}
	}
}
