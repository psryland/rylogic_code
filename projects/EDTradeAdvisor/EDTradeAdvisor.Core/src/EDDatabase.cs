using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EDTradeAdvisor.DomainObjects;
using Newtonsoft.Json.Linq;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace EDTradeAdvisor
{
	public class EDDatabase : IDisposable
	{
		// Notes:
		//  - BuildXX functions are not async for performance reasons
		//  - transactions speed up sqlite inserts

		public EDDatabase(CancellationToken shutdown)
		{
			// Connect to the database
			DB = new SQLiteConnection($"Data Source={Filepath};Version=3;journal mode=Memory;synchronous=Off");

			// Ensure tables exist
			using (var sql_stream = typeof(EDDatabase).Assembly.GetManifestResourceStream($"EDTradeAdvisor.Core.src.Misc.EDDatabaseSetup.sql"))
			using (var sr = new StreamReader(sql_stream, Encoding.UTF8))
			{
				var sql = sr.ReadToEnd();
				DB.Execute(sql);
			}
		}
		public void Dispose()
		{
			DB = null;
		}

		/// <summary>The DB filepath</summary>
		public static string Filepath => Path_.CombinePath(Settings.Instance.DataPath, "cache.db");

		/// <summary>Database access</summary>
		private SQLiteConnection DB
		{
			[DebuggerStepThrough]
			get { return m_db; }
			set
			{
				if (m_db == value) return;
				if (m_db != null)
				{
					m_db.Close();
					Util.Dispose(ref m_db);
				}
				m_db = value;
				if (m_db != null)
				{
					m_db.Open();
				}
			}
		}
		private SQLiteConnection m_db;

		/// <summary>App shutdown token</summary>
		private CancellationToken Shutdown { get; }

		/// <summary>Return a star system by ID</summary>
		public StarSystem GetStarSystem(long id)
		{
			return DB.QuerySingleOrDefault<StarSystem>(new CommandDefinition(
				$"select * from {Table.StarSystems} " +
				$"where [{nameof(StarSystem.ID)}] = @id",
				new { id }));
		}

		/// <summary>Return a station by ID</summary>
		public Station GetStation(long id)
		{
			return DB.QuerySingleOrDefault<Station>(new CommandDefinition(
				$"select * from {Table.Stations} " +
				$"where [{nameof(Station.ID)}] = @id",
				new { id }));
		}

		/// <summary>Return the market data for a given station (by id)</summary>
		public async Task<Market> GetMarketData(long station_id)
		{
			var market = new Market(GetStation(station_id));
			var sql = $"select\n" +
				$"  c.[{nameof(Commodity.ID)}] as [{nameof(Market.Listing.CommodityID)}],\n" +
				$"  c.[{nameof(Commodity.Name)}] as [{nameof(Market.Listing.CommodityName)}],\n" +
				$"  l.[{nameof(Listing.BuyPrice)}] as [{nameof(Market.Listing.BuyPrice)}],\n" +
				$"  l.[{nameof(Listing.SellPrice)}] as [{nameof(Market.Listing.SellPrice)}],\n" +
				$"  l.[{nameof(Listing.Supply)}] as [{nameof(Market.Listing.Supply)}],\n" +
				$"  l.[{nameof(Listing.Demand)}] as [{nameof(Market.Listing.Demand)}]\n" +
				$"from {Table.Listings} as l\n" +
				$"inner join {Table.Commodities} as c on l.[{nameof(Listing.CommodityID)}] = c.[{nameof(Commodity.ID)}]\n" +
				$"where l.[{nameof(Listing.StationID)}] = @station_id";
			market.Listings.AddRange(await DB.QueryAsync<Market.Listing>(new CommandDefinition(sql, new { station_id }, cancellationToken:Shutdown)));
			return market;
		}

		/// <summary>Enumerate all stars</summary>
		public IEnumerable<StarSystem> EnumStarSystems(
			string match_name = null,
			int? max_count = null,
			Range? pop_range = null,
			bool buffered = true
		) {
			return DB.Query<StarSystem>(new CommandDefinition(
				$"select * from {Table.StarSystems}\n" +
				$"where 1\n" +
				(match_name != null ? $"and [{nameof(StarSystem.Name)}] like @match_name\n" : string.Empty) +
				(pop_range != null ? $"and [{nameof(StarSystem.Population)}] >= @pop_beg and [{nameof(StarSystem.Population)}] < @pop_end\n" : string.Empty) +
				(max_count != null ? $"limit @max_count\n" : string.Empty),
				new
				{
					match_name = $"{match_name}%",
					pop_beg = pop_range?.Beg,
					pop_end = pop_range?.End,
					max_count,
				},
				flags: buffered ? CommandFlags.Buffered : CommandFlags.None));
		}

		/// <summary>Enumerate stations</summary>
		public IEnumerable<Station> EnumStations(
			long? system_id = null,
			int? max_count = null,
			long? max_station_distance = null,
			ELandingPadSize? required_pad_size = null,
			EFacilities? facilities_incl = null,
			EFacilities? facilities_excl = null,
			bool? ignore_planetary = null,
			bool buffered = true
		)
		{
			return DB.Query<Station>(new CommandDefinition(
				$"select * from {Table.Stations}\n" +
				$"where 1\n" +
				(system_id != null ? $"and [{nameof(Station.SystemID)}] = @system_id\n" : string.Empty) +
				(max_station_distance != null ? $"and [{nameof(Station.Distance)}] <= @max_station_distance\n" : string.Empty) +
				(required_pad_size != null ? $"and [{nameof(Station.MaxPadSize)}] >= @required_pad_size\n" : string.Empty) +
				(facilities_incl != null ? $"and ([{nameof(Station.Facilities)}] & @facilities_incl) = @facilities_incl\n" : string.Empty) +
				(facilities_excl != null ? $"and ([{nameof(Station.Facilities)}] | @facilities_excl) = 0\n" : string.Empty) +
				(ignore_planetary != null ? $"and [{nameof(Station.Planetary)}] = 0\n" : string.Empty) +
				(max_count != null ? $"limit @max_count\n" : string.Empty),
				new
				{
					system_id,
					max_station_distance,
					required_pad_size = (int?)required_pad_size,
					facilities_incl = (int?)facilities_incl,
					facilities_excl = (int?)facilities_excl,
					max_count,
				},
				flags: buffered ? CommandFlags.Buffered: CommandFlags.None));
		}

		/// <summary>Rebuild the systems table</summary>
		public void BuildSystemsTable(string systems_populated_jsonl)
		{
			using (var msg = StatusStack.NewStatusMessage($"Building Systems Table: {0:P2}"))
			{
				// Delete all content from the table
				DB.Execute($"delete from {Table.StarSystems}");

				// Read each system by line
				using (var sr = new StreamReader(systems_populated_jsonl, Encoding.UTF8, false))
				using (var transaction = DB.BeginTransaction())
				using (var cmd = DB.CreateCommand())
				{
					// Create a command to reuse the parameters object
					cmd.CommandText =
						$"insert into {Table.StarSystems} (\n" +
						$"  [{nameof(StarSystem.ID)}],\n" +
						$"  [{nameof(StarSystem.Name)}],\n" +
						$"  [{nameof(StarSystem.X)}],\n" +
						$"  [{nameof(StarSystem.Y)}],\n" +
						$"  [{nameof(StarSystem.Z)}],\n" +
						$"  [{nameof(StarSystem.Population)}],\n" +
						$"  [{nameof(StarSystem.NeedPermit)}],\n" +
						$"  [{nameof(StarSystem.UpdatedAt)}]\n" +
						$") values (\n" +
						$"  @id, @name, @x, @y, @z, @population, @need_permit, @updated_at\n" +
						$")\n";
					cmd.Parameters.Add("@id", R<StarSystem>.Type(x => x.ID).DbType());
					cmd.Parameters.Add("@name", R<StarSystem>.Type(x => x.Name).DbType());
					cmd.Parameters.Add("@x", R<StarSystem>.Type(x => x.X).DbType());
					cmd.Parameters.Add("@y", R<StarSystem>.Type(x => x.Y).DbType());
					cmd.Parameters.Add("@z", R<StarSystem>.Type(x => x.Z).DbType());
					cmd.Parameters.Add("@population", R<StarSystem>.Type(x => x.Population).DbType());
					cmd.Parameters.Add("@need_permit", R<StarSystem>.Type(x => x.NeedPermit).DbType());
					cmd.Parameters.Add("@updated_at", R<StarSystem>.Type(x => x.UpdatedAt).DbType());
					cmd.Transaction = transaction;

					var last_progress_update = 0.0;
					for (; !sr.EndOfStream;)
					{
						var line = sr.ReadLine();
						var jobj = JObject.Parse(line);
						Shutdown.ThrowIfCancellationRequested();

						cmd.Reset();
						cmd.Parameters["@id"].Value = jobj["id"].Value<long>();
						cmd.Parameters["@name"].Value = jobj["name"].Value<string>();
						cmd.Parameters["@x"].Value = jobj["x"].Value<double>();
						cmd.Parameters["@y"].Value = jobj["y"].Value<double>();
						cmd.Parameters["@z"].Value = jobj["z"].Value<double>();
						cmd.Parameters["@population"].Value = jobj["population"].Value<long>();
						cmd.Parameters["@need_permit"].Value = jobj["needs_permit"].Value<bool>();
						cmd.Parameters["@updated_at"].Value = jobj["updated_at"].Value<long>();
						cmd.ExecuteNonQuery();

						// Report progress
						var fraction_done = (double)sr.BaseStream.Position / sr.BaseStream.Length;
						if (fraction_done - last_progress_update > 0.01)
						{
							msg.Message = $"Building Systems Table: {fraction_done:P2}";
							last_progress_update = fraction_done;
						}
					}

					transaction.Commit();
					msg.Message = $"Building Systems Table: {1:P2}";
				}
			}
		}

		/// <summary>Rebuild the stations table</summary>
		public void BuildStationsTable(string stations_jsonl)
		{
			using (var msg = StatusStack.NewStatusMessage($"Building Stations Table: {0:P2}"))
			{
				// Delete all content from the table
				DB.Execute($"delete from {Table.Stations}");

				// Read each station by line
				using (var sr = new StreamReader(stations_jsonl, Encoding.UTF8, false))
				using (var transaction = DB.BeginTransaction())
				using (var cmd = DB.CreateCommand())
				{
					// Create a command to reuse the parameters object
					cmd.CommandText =
						$"insert into {Table.Stations} (\n" +
						$"  [{nameof(Station.ID)}],\n" +
						$"  [{nameof(Station.Name)}],\n" +
						$"  [{nameof(Station.SystemID)}],\n" +
						$"  [{nameof(Station.Type)}],\n" +
						$"  [{nameof(Station.Distance)}],\n" +
						$"  [{nameof(Station.Facilities)}],\n" +
						$"  [{nameof(Station.MaxPadSize)}],\n" +
						$"  [{nameof(Station.Planetary)}],\n" +
						$"  [{nameof(Station.UpdatedAt)}]\n" +
						$") values (\n" +
						$"  @id, @name, @system_id, @type, @distance, @facilities, @max_pad_size, @planetary, @updated\n" +
						$")\n";
					cmd.Parameters.Add("@id", R<Station>.Type(x => x.ID).DbType());
					cmd.Parameters.Add("@name", R<Station>.Type(x => x.Name).DbType());
					cmd.Parameters.Add("@system_id", R<Station>.Type(x => x.SystemID).DbType());
					cmd.Parameters.Add("@type", R<Station>.Type(x => x.Type).DbType());
					cmd.Parameters.Add("@distance", R<Station>.Type(x => x.Distance).DbType());
					cmd.Parameters.Add("@facilities", R<Station>.Type(x => x.Facilities).DbType());
					cmd.Parameters.Add("@max_pad_size", R<Station>.Type(x => x.MaxPadSize).DbType());
					cmd.Parameters.Add("@planetary", R<Station>.Type(x => x.Planetary).DbType());
					cmd.Parameters.Add("@updated", R<Station>.Type(x => x.UpdatedAt).DbType());
					cmd.Transaction = transaction;

					var last_progress_update = 0.0;
					for (; !sr.EndOfStream;)
					{
						var line = sr.ReadLine();
						var jobj = JObject.Parse(line);
						Shutdown.ThrowIfCancellationRequested();

						cmd.Reset();
						cmd.Parameters["@id"].Value = jobj["id"].Value<long>();
						cmd.Parameters["@name"].Value = jobj["name"].Value<string>();
						cmd.Parameters["@system_id"].Value = jobj["system_id"].Value<long>();
						cmd.Parameters["@type"].Value = GetStationType(jobj);
						cmd.Parameters["@distance"].Value = jobj["distance_to_star"].Value<long?>();
						cmd.Parameters["@facilities"].Value = GetFacilities(jobj);
						cmd.Parameters["@max_pad_size"].Value = GetMaxPadSize(jobj);
						cmd.Parameters["@planetary"].Value = jobj["is_planetary"].Value<bool>();
						cmd.Parameters["@updated"].Value = jobj["updated_at"].Value<long>();
						cmd.ExecuteNonQuery();

						// Report progress
						var fraction_done = (double)sr.BaseStream.Position / sr.BaseStream.Length;
						if (fraction_done - last_progress_update > 0.01)
						{
							msg.Message = $"Building Stations Table: {fraction_done:P2}";
							last_progress_update = fraction_done;
						}
					}

					transaction.Commit();
					msg.Message = $"Building Stations Table: {1:P2}";
				}
			}

			EStationType GetStationType(JObject jobj)
			{
				var type = jobj["type_id"].Value<long?>();
				return type != null ? (EStationType)type.Value : EStationType.Unknown;
			}
			EFacilities GetFacilities(JObject jobj)
			{
				var fac = EFacilities.None;
				if (jobj["has_market"].Value<bool>()) fac |= EFacilities.Market;
				if (jobj["has_blackmarket"].Value<bool>()) fac |= EFacilities.BlackMarket;
				if (jobj["has_refuel"].Value<bool>()) fac |= EFacilities.Refuel;
				if (jobj["has_repair"].Value<bool>()) fac |= EFacilities.Repair;
				if (jobj["has_rearm"].Value<bool>()) fac |= EFacilities.Rearm;
				if (jobj["has_outfitting"].Value<bool>()) fac |= EFacilities.Outfitting;
				if (jobj["has_shipyard"].Value<bool>()) fac |= EFacilities.Shipyard;
				if (jobj["has_docking"].Value<bool>()) fac |= EFacilities.Docking;
				if (jobj["has_commodities"].Value<bool>()) fac |= EFacilities.Commodities;
				return fac;
			}
			ELandingPadSize GetMaxPadSize(JObject jobj)
				{
					switch (jobj["max_landing_pad_size"].Value<string>().ToUpperInvariant())
					{
					default: return ELandingPadSize.None;
					case "L": return ELandingPadSize.Large;
					case "M": return ELandingPadSize.Medium;
					case "S": return ELandingPadSize.Small;
					}
				}
		}

		/// <summary>Rebuild the commodities table</summary>
		public void BuildCommoditiesTable(string commodities_json)
		{
			using (var msg = StatusStack.NewStatusMessage($"Building Commodities Tables: {0:P2}"))
			{
				// Delete all content from the tables
				DB.Execute($"delete from {Table.Commodities}");
				DB.Execute($"delete from {Table.CommodityCategories}");

				// Read all commodities into memory
				var jarr = JArray.Parse(File.ReadAllText(commodities_json));
				var categories = jarr.ToDictionary(x => x["id"].Value<int>(), x => x["name"].Value<string>());
				double last_progress_update;
				int i;

				// Build the categories table first
				using (var transaction = DB.BeginTransaction())
				using (var cmd = DB.CreateCommand())
				{
					cmd.CommandText =
						$"insert into {Table.CommodityCategories} (\n" +
						$"  [{nameof(CommodityCategory.ID)}],\n" +
						$"  [{nameof(CommodityCategory.Name)}]\n" +
						$") values (\n" +
						$"  @id, @name\n" +
						$")\n";
					cmd.Parameters.Add("@id", R<CommodityCategory>.Type(x => x.ID).DbType());
					cmd.Parameters.Add("@name", R<CommodityCategory>.Type(x => x.Name).DbType());
					cmd.Transaction = transaction;

					last_progress_update = 0.0; i = 0;
					foreach (var category in categories)
					{
						Shutdown.ThrowIfCancellationRequested();

						cmd.Reset();
						cmd.Parameters["@id"].Value = category.Key;
						cmd.Parameters["@name"].Value = category.Value;
						cmd.ExecuteNonQuery();

						// Report progress
						var fraction_done = (double)i++ / categories.Count;
						if (fraction_done - last_progress_update > 0.01)
						{
							msg.Message = $"Building Commodity Categories Table: {fraction_done:P2}";
							last_progress_update = fraction_done;
						}
					}

					transaction.Commit();
					msg.Message = $"Building Commodity Categories Table: {1:P2}";
				}

				// Build the commodities table
				using (var transaction = DB.BeginTransaction())
				using (var cmd = DB.CreateCommand())
				{
					cmd.CommandText =
						$"insert into {Table.Commodities} (\n" +
						$"  [{nameof(Commodity.ID)}],\n" +
						$"  [{nameof(Commodity.Name)}],\n" +
						$"  [{nameof(Commodity.CategoryID)}],\n" +
						$"  [{nameof(Commodity.IsRare)}],\n" +
						$"  [{nameof(Commodity.MinBuyPrice)}],\n" +
						$"  [{nameof(Commodity.MaxBuyPrice)}],\n" +
						$"  [{nameof(Commodity.MinSellPrice)}],\n" +
						$"  [{nameof(Commodity.MaxSellPrice)}],\n" +
						$"  [{nameof(Commodity.AveragePrice)}],\n" +
						$"  [{nameof(Commodity.BuyPriceLowerAverage)}],\n" +
						$"  [{nameof(Commodity.SellPriceUpperAverage)}],\n" +
						$"  [{nameof(Commodity.IsNonMarketable)}],\n" +
						$"  [{nameof(Commodity.EDID)}]\n" +
						$") values (\n" +
						$"  @id, @name, @category_id, @is_rare, @min_buy_price, @max_buy_price, @min_sell_price, @max_sell_price, @average_price, @buy_price_lower_average, @sell_price_upper_average, @is_non_marketable, @ed_id\n" +
						$")\n";
					cmd.Parameters.Add("@id", R<Commodity>.Type(x => x.ID).DbType());
					cmd.Parameters.Add("@name", R<Commodity>.Type(x => x.Name).DbType());
					cmd.Parameters.Add("@category_id", R<Commodity>.Type(x => x.CategoryID).DbType());
					cmd.Parameters.Add("@is_rare", R<Commodity>.Type(x => x.IsRare).DbType());
					cmd.Parameters.Add("@min_buy_price", R<Commodity>.Type(x => x.MinBuyPrice).DbType());
					cmd.Parameters.Add("@max_buy_price", R<Commodity>.Type(x => x.MaxBuyPrice).DbType());
					cmd.Parameters.Add("@min_sell_price", R<Commodity>.Type(x => x.MinSellPrice).DbType());
					cmd.Parameters.Add("@max_sell_price", R<Commodity>.Type(x => x.MaxSellPrice).DbType());
					cmd.Parameters.Add("@average_price", R<Commodity>.Type(x => x.AveragePrice).DbType());
					cmd.Parameters.Add("@buy_price_lower_average", R<Commodity>.Type(x => x.BuyPriceLowerAverage).DbType());
					cmd.Parameters.Add("@sell_price_upper_average", R<Commodity>.Type(x => x.SellPriceUpperAverage).DbType());
					cmd.Parameters.Add("@is_non_marketable", R<Commodity>.Type(x => x.IsNonMarketable).DbType());
					cmd.Parameters.Add("@ed_id", R<Commodity>.Type(x => x.EDID).DbType());
					cmd.Transaction = transaction;

					last_progress_update = 0.0; i = 0;
					foreach (var jobj in jarr)
					{
						Shutdown.ThrowIfCancellationRequested();

						cmd.Reset();
						cmd.Parameters["@id"].Value = jobj["id"].Value<long>();
						cmd.Parameters["@name"].Value = jobj["name"].Value<string>();
						cmd.Parameters["@category_id"].Value = jobj["category_id"].Value<long>();
						cmd.Parameters["@is_rare"].Value = jobj["is_rare"].Value<bool>();
						cmd.Parameters["@min_buy_price"].Value = jobj["min_buy_price"].Value<int?>();
						cmd.Parameters["@max_buy_price"].Value = jobj["max_buy_price"].Value<int?>();
						cmd.Parameters["@min_sell_price"].Value = jobj["min_sell_price"].Value<int?>();
						cmd.Parameters["@max_sell_price"].Value = jobj["max_sell_price"].Value<int?>();
						cmd.Parameters["@average_price"].Value = jobj["average_price"].Value<int?>();
						cmd.Parameters["@buy_price_lower_average"].Value = jobj["buy_price_lower_average"].Value<int>();
						cmd.Parameters["@sell_price_upper_average"].Value = jobj["sell_price_upper_average"].Value<int>();
						cmd.Parameters["@is_non_marketable"].Value = jobj["is_non_marketable"].Value<bool>();
						cmd.Parameters["@ed_id"].Value = jobj["ed_id"].Value<long>();
						cmd.ExecuteNonQuery();

						// Report progress
						var fraction_done = (double)i++ / jarr.Count;
						if (fraction_done - last_progress_update > 0.01)
						{
							msg.Message = $"Building Commodities Table: {fraction_done:P2}";
							last_progress_update = fraction_done;
						}
					}

					transaction.Commit();
					msg.Message = $"Building Commodities Table: {1:P2}";
				}
			}
		}

		/// <summary>Rebuild the listings table</summary>
		public void BuildListingsTable(string listings_csv)
		{
			using (var msg = StatusStack.NewStatusMessage($"Building Listings Table: {0.0:P2}"))
			{
				// Delete all content from the table
				DB.Execute($"delete from {Table.Listings}");

				// Read each listing by row
				var rows = CSVData.Parse(listings_csv, false, x => msg.Message = $"Building Listings Table: {x:P2}");
				using (var transaction = DB.BeginTransaction())
				using (var cmd = DB.CreateCommand())
				{
					cmd.CommandText =
						$"insert into {Table.Listings} (\n" +
						$"  [{nameof(Listing.ID           )}],\n" +
						$"  [{nameof(Listing.StationID    )}],\n" +
						$"  [{nameof(Listing.CommodityID  )}],\n" +
						$"  [{nameof(Listing.Supply       )}],\n" +
						$"  [{nameof(Listing.SupplyBracket)}],\n" +
						$"  [{nameof(Listing.BuyPrice     )}],\n" +
						$"  [{nameof(Listing.SellPrice    )}],\n" +
						$"  [{nameof(Listing.Demand       )}],\n" +
						$"  [{nameof(Listing.DemandBracket)}],\n" +
						$"  [{nameof(Listing.UpdatedAt    )}]\n" +
						$") values (\n" +
						$"  @id, @station_id, @commodity_id, @supply, @supply_bracket, @buy_price, @sell_price, @demand, @demand_bracket, @updated_at\n" +
						$")\n";
					cmd.Parameters.Add("@id"             ,R<Listing>.Type(x => x.ID).DbType());
					cmd.Parameters.Add("@station_id"     ,R<Listing>.Type(x => x.StationID    ).DbType());
					cmd.Parameters.Add("@commodity_id"   ,R<Listing>.Type(x => x.CommodityID  ).DbType());
					cmd.Parameters.Add("@supply"         ,R<Listing>.Type(x => x.Supply       ).DbType());
					cmd.Parameters.Add("@supply_bracket" ,R<Listing>.Type(x => x.SupplyBracket).DbType());
					cmd.Parameters.Add("@buy_price"      ,R<Listing>.Type(x => x.BuyPrice     ).DbType());
					cmd.Parameters.Add("@sell_price"     ,R<Listing>.Type(x => x.SellPrice    ).DbType());
					cmd.Parameters.Add("@demand"         ,R<Listing>.Type(x => x.Demand       ).DbType());
					cmd.Parameters.Add("@demand_bracket" ,R<Listing>.Type(x => x.DemandBracket).DbType());
					cmd.Parameters.Add("@updated_at"     ,R<Listing>.Type(x => x.UpdatedAt    ).DbType());
					cmd.Transaction = transaction;

					long ParseInt64(string s) => s.Length != 0 ? long.Parse(s) : 0L;
					int ParseInt32(string s) => s.Length != 0 ? int.Parse(s) : 0;

					// Build the listings table
					foreach (var row in rows.Skip(1))
					{
						Shutdown.ThrowIfCancellationRequested();

						cmd.Reset();
						cmd.Parameters["@id"].Value = ParseInt64(row[0]);
						cmd.Parameters["@station_id"].Value = ParseInt64(row[1]);
						cmd.Parameters["@commodity_id"].Value = ParseInt64(row[2]);
						cmd.Parameters["@supply"].Value = ParseInt32(row[3]);
						cmd.Parameters["@supply_bracket"].Value = ParseInt32(row[4]);
						cmd.Parameters["@buy_price"].Value = ParseInt32(row[5]);
						cmd.Parameters["@sell_price"].Value = ParseInt32(row[6]);
						cmd.Parameters["@demand"].Value = ParseInt64(row[7]);
						cmd.Parameters["@demand_bracket"].Value = ParseInt32(row[8]);
						cmd.Parameters["@updated_at"].Value = ParseInt64(row[9]);
						cmd.ExecuteNonQuery();
					}

					transaction.Commit();
					msg.Message = $"Building Listings Table: {1.0:P2}";
				}
			}
		}

		/// <summary>Table names</summary>
		private static class Table
		{
			public const string StarSystems = "[StarSystems]";
			public const string Stations = "[Stations]";
			public const string CommodityCategories = "[CommodityCategories]";
			public const string Commodities = "[Commodities]";
			public const string Listings = "[Listings]";
		}
	}
}
