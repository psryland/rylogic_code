using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tradee
{
	public enum ETrend
	{
		/// <summary>No clear trend</summary>
		None,

		/// <summary>Trending upward</summary>
		Up,

		/// <summary>Trending downward</summary>
		Down,

		/// <summary>Oscillating about a fixed level</summary>
		Ranging,
	}

	//public class Trend :IndicatorBase
	//{
	//	// Determines the trend state of an instrument

	//	public Trend(MainModel model)
	//		:base(model)
	//	{}

	//	/// <summary>Returns the trend state of 'instr' </summary>
	//	public ETrend TrendState(Instrument instr, ETimeFrame time_frame)
	//	{
	//		return ETrend.None;
	//	}
	//}
}
