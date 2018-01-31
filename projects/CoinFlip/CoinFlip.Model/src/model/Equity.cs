using System;
using System.Collections;
using System.Collections.Generic;
using Rylogic.Container;
using Rylogic.Extn;

namespace CoinFlip
{
	///// <summary>A container of currency amounts over time</summary>
	//public class EquityMap :IEnumerable<KeyValuePair<Coin, EquityMap.Table>>
	//{
	//	// Notes:
	//	//  - Equity is calculated backward in time from the current account
	//	//    balance and the available history of trades. This means tables
	//	//    are in reverse time order.

	//	private readonly LazyDictionary<Coin, Table> m_map;
	//	public EquityMap()
	//	{
	//		m_map = new LazyDictionary<Coin, Table>(k => new Table(this));
	//	}

	//	/// <summary>Reset the data</summary>
	//	public void Reset()
	//	{
	//		m_map.Clear();
	//		RaiseDataChanged();
	//	}

	//	/// <summary>Access the table for 'coin'</summary>
	//	public Table this[Coin coin]
	//	{
	//		get { return m_map[coin]; }
	//	}

	//	/// <summary>Raised whenever data is added/removed</summary>
	//	public event EventHandler DataChanged;
	//	private void RaiseDataChanged()
	//	{
	//		DataChanged.Raise(this);
	//	}

	//	#region IEnumerable
	//	public IEnumerator<KeyValuePair<Coin, Table>> GetEnumerator()
	//	{
	//		return m_map.GetEnumerator();
	//	}
	//	IEnumerator IEnumerable.GetEnumerator()
	//	{
	//		return m_map.GetEnumerator();
	//	}
	//	#endregion

	//	/// <summary>A list of equity data for a single currency</summary>
	//	public class Table :IEnumerable<Point>
	//	{
	//		private readonly EquityMap m_map;
	//		private readonly List<Point> m_list;

	//		public Table(EquityMap map)
	//		{
	//			m_map = map;
	//			m_list = new List<Point>();
	//		}

	//		/// <summary>The number of values in this table</summary>
	//		public int Count
	//		{
	//			get { return m_list.Count; }
	//		}

	//		/// <summary>Add a data point</summary>
	//		public void Add(Point pt)
	//		{
	//			m_list.Add(pt);
	//			m_map.RaiseDataChanged();
	//		}

	//		#region IEnumerable
	//		public IEnumerator<Point> GetEnumerator()
	//		{
	//			return m_list.GetEnumerator();
	//		}
	//		IEnumerator IEnumerable.GetEnumerator()
	//		{
	//			return m_list.GetEnumerator();
	//		}
	//		#endregion
	//	}

	//	/// <summary>A single point in a table</summary>
	//	public struct Point
	//	{
	//		public Point(long timestamp, double volume)
	//		{
	//			Timestamp = timestamp;
	//			Volume = volume;
	//		}
	//		public long Timestamp;
	//		public double Volume;
	//	}
	//}
}
