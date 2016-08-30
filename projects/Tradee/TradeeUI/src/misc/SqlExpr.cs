using System.Collections.Generic;
using pr.extn;

namespace Tradee
{
	/// <summary>Sql expression strings</summary>
	public static class SqlExpr
	{
		#region Price Data

		/// <summary>Create a price data table</summary>
		public static string PriceDataTable()
		{
			// Note: this doesn't store the price data history, only the last received price data
			return Str.Build(
				"create table if not exists PriceData (\n",
				"[",nameof(PriceData.AskPrice  ),"] real,\n",
				"[",nameof(PriceData.BidPrice  ),"] real,\n",
				"[",nameof(PriceData.AvrSpread ),"] real,\n",
				"[",nameof(PriceData.LotSize   ),"] real,\n",
				"[",nameof(PriceData.PipSize   ),"] real,\n",
				"[",nameof(PriceData.PipValue  ),"] real,\n",
				"[",nameof(PriceData.VolumeMin ),"] real,\n",
				"[",nameof(PriceData.VolumeStep),"] real,\n",
				"[",nameof(PriceData.VolumeMax ),"] real)"
				);
		}

		/// <summary>Sql expression to get the price data from the db</summary>
		public static string GetPriceData()
		{
			return "select * from PriceData";
		}

		/// <summary>Update the price data</summary>
		public static string UpdatePriceData()
		{
			return Str.Build(
				"insert or replace into PriceData ( ",
				"rowid,",
				"[",nameof(PriceData.AskPrice  ),"],",
				"[",nameof(PriceData.BidPrice  ),"],",
				"[",nameof(PriceData.AvrSpread ),"],",
				"[",nameof(PriceData.LotSize   ),"],",
				"[",nameof(PriceData.PipSize   ),"],",
				"[",nameof(PriceData.PipValue  ),"],",
				"[",nameof(PriceData.VolumeMin ),"],",
				"[",nameof(PriceData.VolumeStep),"],",
				"[",nameof(PriceData.VolumeMax ),"])",
				" values (",
				"1,", // rowid
				"?,", // AskPrice  
				"?,", // BidPrice  
				"?,", // AvrSpread 
				"?,", // LotSize   
				"?,", // PipSize   
				"?,", // PipValue  
				"?,", // VolumeMin 
				"?,", // VolumeStep
				"?)"  // VolumeMax 
				);
		}

		/// <summary>Return the properties of a price data object to match the update command</summary>
		public static object[] UpdatePriceDataParams(PriceData pd)
		{
			return new object[]
			{
				pd.AskPrice  ,
				pd.BidPrice  ,
				pd.AvrSpread ,
				pd.LotSize   ,
				pd.PipSize   ,
				pd.PipValue  ,
				pd.VolumeMin ,
				pd.VolumeStep,
				pd.VolumeMax ,
			};
		}

		#endregion

		#region Candles

		/// <summary>Create a table of candles for a time frame</summary>
		public static string CandleTable(ETimeFrame time_frame)
		{
			return Str.Build(
				"create table if not exists ",time_frame," (\n",
				"[",nameof(Candle.Timestamp),"] integer unique,\n",
				"[",nameof(Candle.Open     ),"] real,\n",
				"[",nameof(Candle.High     ),"] real,\n",
				"[",nameof(Candle.Low      ),"] real,\n",
				"[",nameof(Candle.Close    ),"] real,\n",
				"[",nameof(Candle.Median   ),"] real,\n",
				"[",nameof(Candle.Volume   ),"] real)"
				);
		}

		/// <summary>Insert or replace a candle in table 'time_frame'</summary>
		public static string InsertCandle(ETimeFrame time_frame)
		{
			return Str.Build(
				"insert or replace into ",time_frame," (",
				"[",nameof(Candle.Timestamp),"],",
				"[",nameof(Candle.Open     ),"],",
				"[",nameof(Candle.High     ),"],",
				"[",nameof(Candle.Low      ),"],",
				"[",nameof(Candle.Close    ),"],",
				"[",nameof(Candle.Median   ),"],",
				"[",nameof(Candle.Volume   ),"])",
				" values (",
				"?,", // Timestamp
				"?,", // Open     
				"?,", // High     
				"?,", // Low      
				"?,", // Close    
				"?,", // Median   
				"?)"  // Volume   
				);
		}

		/// <summary>Return the properties of a candle to match an InsertCandle sql statement</summary>
		public static object[] InsertCandleParams(Candle candle)
		{
			return new object[]
			{
				candle.Timestamp,
				candle.Open,
				candle.High,
				candle.Low,
				candle.Close,
				candle.Median,
				candle.Volume,
			};
		}

		#endregion

		#region SnR Levels

		/// <summary>Create a table of support and resistance levels</summary>
		public static string SnRLevelsTable()
		{
			return Str.Build(
				"create table if not exists SnRLevels (\n",
				"[",nameof(SnRLevel.Id),"] text unique,\n",
				"[",nameof(SnRLevel.Price),"] real,\n",
				"[",nameof(SnRLevel.WidthPips),"] real,\n",
				"[",nameof(SnRLevel.MaxTimeFrame),"] int)"
				);
		}

		/// <summary>Sql expression to get all SnR level data</summary>
		public static string GetSnRLevelData()
		{
			return "select * from SnRLevels";
		}

		/// <summary>Insert a support and resistance level into the DB</summary>
		public static string UpdateSnRLevel()
		{
			return Str.Build(
				"insert or replace into SnRLevels (",
				"[",nameof(SnRLevel.Id),"],",
				"[",nameof(SnRLevel.Price),"],",
				"[",nameof(SnRLevel.WidthPips),"],",
				"[",nameof(SnRLevel.MaxTimeFrame),"]",
				") values (",
				"?,", // Id
				"?,", // Price
				"?,", // WidthPips
				"?)"  // MaxTimeFrame
				);
		}

		/// <summary>Parameters for the UpdateSnRLevel statement</summary>
		public static object[] UpdateSnRLevelParams(SnRLevel snr)
		{
			return new object[]
			{
				snr.Id,
				snr.Price,
				snr.WidthPips,
				snr.MaxTimeFrame,
			};
		}

		/// <summary>Remove a support and resistance level from the DB</summary>
		public static string RemoveSnRLevel()
		{
			return Str.Build("delete from SnRLevels where [",nameof(SnRLevel.Id),"] = ?");
		}
		public static object[] RemoveSnRLevelParams(SnRLevel snr)
		{
			return new object[] { snr.Id };
		}

		#endregion
	}
}
