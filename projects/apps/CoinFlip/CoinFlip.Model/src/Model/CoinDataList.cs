using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using CoinFlip.Settings;
using Rylogic.Extn;

namespace CoinFlip
{
	public class CoinDataList :IReadOnlyList<CoinData>, IList<CoinData>, IList, INotifyCollectionChanged
	{
		// Notes:
		//  - This is a wrapper around the 'Settings.Coins' array.
		//    It allows new coins to be added implicitly.

		public CoinDataList()
		{
		}

		/// <summary>The number of available coins</summary>
		public int Count => SettingsData.Settings.Coins.Length;

		/// <summary>Access the coin data by symbol name</summary>
		public CoinData this[string sym]
		{
			get
			{
				var idx = this.IndexOf(x => x.Symbol == sym);
				return idx >= 0
					? SettingsData.Settings.Coins[idx]
					: new CoinData(sym);
			}
			set
			{
				var idx = this.IndexOf(x => x.Symbol == sym);
				if (idx >= 0)
					this[idx] = value;
				else
					Add(value);
			}
		}

		/// <summary>Access the coin data by index</summary>
		public CoinData this[int index]
		{
			get => SettingsData.Settings.Coins[index];
			set
			{
				var old = this[index];
				var nue = value;

				// Update the coin, and the display order
				SettingsData.Settings.Coins[index] = nue;
				for (int i = 0; i != SettingsData.Settings.Coins.Length; ++i)
					SettingsData.Settings.Coins[i].DisplayOrder = i;

				SettingsData.Settings.NotifySettingChanged(nameof(SettingsData.Settings.Coins));
				SettingsData.Settings.AutoSave();

				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, nue, old));
			}
		}

		/// <summary></summary>
		public bool IsReadOnly => false;

		/// <summary></summary>
		public bool Contains(CoinData item)
		{
			return SettingsData.Settings.Coins.Contains(item);
		}

		/// <summary></summary>
		public int IndexOf(CoinData item)
		{
			return SettingsData.Settings.Coins.IndexOf(item);
		}

		/// <summary></summary>
		public void Clear()
		{
			SettingsData.Settings.Coins = new CoinData[0];
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		/// <summary></summary>
		public void Add(CoinData item)
		{
			Insert(Count, item);
		}

		/// <summary></summary>
		public void Insert(int index, CoinData item)
		{
			SettingsData.Settings.Coins = Array_.New(Count + 1, i =>
			{
				return
					i < index ? SettingsData.Settings.Coins[i] :
					i > index ? SettingsData.Settings.Coins[i - 1] :
					item;
			});

			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
		}

		/// <summary></summary>
		public bool Remove(CoinData item)
		{
			var idx = IndexOf(item);
			if (idx < 0) return false;
			RemoveAt(idx);
			return true;
		}

		/// <summary></summary>
		public void RemoveAt(int index)
		{
			var doomed = SettingsData.Settings.Coins[index];
			SettingsData.Settings.Coins = Array_.New(Count - 1, i =>
			{
				return i < index
				? SettingsData.Settings.Coins[i]
				: SettingsData.Settings.Coins[i + 1];
			});

			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new[] { doomed }));
		}

		/// <summary></summary>
		public void CopyTo(CoinData[] array, int arrayIndex)
		{
			SettingsData.Settings.Coins.CopyTo(array, arrayIndex);
		}

		/// <summary></summary>
		public IEnumerator<CoinData> GetEnumerator()
		{
			return ((IEnumerable<CoinData>)SettingsData.Settings.Coins).GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#region IList
		bool IList.IsFixedSize => false;
		bool ICollection.IsSynchronized => false;
		object ICollection.SyncRoot => throw new NotImplementedException();
		object? IList.this[int index]
		{
			get => this[index];
			set => this[index] = (CoinData?)value ?? throw new ArgumentNullException("value");
		}
		bool IList.Contains(object? value)
		{
			return Contains((CoinData?)value ?? throw new ArgumentNullException("value"));
		}
		int IList.IndexOf(object? value)
		{
			return IndexOf((CoinData?)value ?? throw new ArgumentNullException("value"));
		}
		int IList.Add(object? value)
		{
			Add((CoinData?)value ?? throw new ArgumentNullException("value"));
			return Count - 1;
		}
		void IList.Insert(int index, object? value)
		{
			Insert(index, (CoinData?)value ?? throw new ArgumentNullException("value"));
		}
		void IList.Remove(object? value)
		{
			Remove((CoinData?)value ?? throw new ArgumentNullException("value"));
		}
		void ICollection.CopyTo(Array array, int index)
		{
			SettingsData.Settings.Coins.CopyTo(array, index);
		}
		#endregion

		/// <summary></summary>
		public event NotifyCollectionChangedEventHandler? CollectionChanged;
	}
}
