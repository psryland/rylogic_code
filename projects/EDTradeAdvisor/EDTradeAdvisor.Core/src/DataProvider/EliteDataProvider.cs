using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EDTradeAdvisor.DomainObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace EDTradeAdvisor.DataProviders
{
	public class EliteDataProvider : IEliteDataProvider
	{
		// Notes:
		//  - A data provider based on the useful bits of EDDB and EDSM
		//  - Neither EDDB or EDSM are ideal on their own, but using both
		//    should do the job

		public EliteDataProvider(Web web, CancellationToken shutdown)
		{
			Web = web;
			Shutdown = shutdown;

			m_cache_star_systems = new Cache<long, StarSystem> { ThreadSafe = true, Capacity = 0 };
			m_cache_stations = new Cache<long, Station> { ThreadSafe = true, Capacity = 0 };
			m_cache_market = new Cache<long, Market> { ThreadSafe = true, Capacity = 0 };
			Map = new Map();

			// Connect to the database and generate the tables
			Path_.CreateDirs(Path_.Directory(Filepath));
			DB = new SQLiteConnection(DBConnectionString);
			InitDBTables();
		}
		public void Dispose()
		{
			DB = null;
		}

		/// <summary>HTTP client for access REST API on EDSM</summary>
		private Web Web { get; }

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

		/// <summary>The DB filepath</summary>
		public static string Filepath => Path_.CombinePath(Settings.Instance.DataPath, "cache.db");
		private static string DBConnectionString = $"Data Source={Filepath};Version=3;journal mode=Memory;synchronous=Off";

		/// <summary>Search the map for nearby systems</summary>
		public IEnumerable<StarSystemRef> Search(v4 position, double radius) => Map.Search(position, radius);
		private Map Map { get; }

		/// <summary>Return a star system by Name/ID</summary>
		public async Task<StarSystem> GetStarSystem(string name)
		{
			return await DB.QuerySingleOrDefaultAsync<StarSystem>(new CommandDefinition(
				$"select * from {Table.StarSystems} " +
				$"where [{nameof(StarSystem.Name)}] = @name",
				parameters: new { name },
				cancellationToken: Shutdown));
		}
		public async Task<StarSystem> GetStarSystem(long id)
		{
			return await m_cache_star_systems.GetAsync(id, _ =>
			{
				return DB.QuerySingleOrDefaultAsync<StarSystem>(new CommandDefinition(
					$"select * from {Table.StarSystems} " +
					$"where [{nameof(StarSystem.ID)}] = @id",
					parameters: new { id },
					cancellationToken: Shutdown));
			});
		}
		private Cache<long, StarSystem> m_cache_star_systems;

		/// <summary>Return a station by Name/ID</summary>
		public async Task<Station> GetStation(long system_id, string name)
		{
			return await DB.QuerySingleOrDefaultAsync<Station>(new CommandDefinition(
				$"select * from {Table.Stations} " +
				$"where [{nameof(Station.SystemID)}] = @system_id and [{nameof(Station.Name)}] = @name",
				parameters: new { system_id, name },
				cancellationToken: Shutdown));
		}
		public async Task<Station> GetStation(long id)
		{
			return await m_cache_stations.GetAsync(id, _ =>
			{
				return DB.QuerySingleOrDefaultAsync<Station>(new CommandDefinition(
					$"select * from {Table.Stations} " +
					$"where [{nameof(Station.ID)}] = @id",
					parameters: new { id },
					cancellationToken: Shutdown));
			});
		}
		private Cache<long, Station> m_cache_stations;

		/// <summary>Return the market data for a given station (by id)</summary>
		public async Task<Market> GetMarketData(long station_id)
		{
			return await m_cache_market.GetAsync(station_id, async _ =>
			{
				var station = await GetStation(station_id);
				var market = new Market(station);
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

				var listings = await DB.QueryAsync<Market.Listing>(new CommandDefinition(sql, new { station_id }, cancellationToken: Shutdown));
				market.Listings.AddRange(listings);
				return market;
			});
		}
		private Cache<long, Market> m_cache_market;

		/// <summary>Enumerate all stars</summary>
		public IEnumerable<StarSystem> EnumStarSystems(
			string match_name = null,
			int? max_count = null,
			bool? ignore_permitted = null,
			bool buffered = true
		)
		{
			return DB.Query<StarSystem>(new CommandDefinition(
				$"select * from {Table.StarSystems}\n" +
				$"where 1\n" +
				(match_name != null ? $"and [{nameof(StarSystem.Name)}] like @match_name\n" : string.Empty) +
				(ignore_permitted == true ? $"and [{nameof(StarSystem.NeedPermit)}] = 0\n" : string.Empty) +
				(max_count != null ? $"limit @max_count\n" : string.Empty),
				new
				{
					match_name = $"{match_name}%",
					max_count,
				},
				flags: buffered ? CommandFlags.Buffered : CommandFlags.None));
		}

		/// <summary>Enumerate stations</summary>
		public IEnumerable<Station> EnumStations(
			long? system_id = null,
			string match_name = null,
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
				(match_name != null ? $"and [{nameof(Station.Name)}] like @match_name\n" : string.Empty) +
				(max_station_distance != null ? $"and [{nameof(Station.Distance)}] <= @max_station_distance\n" : string.Empty) +
				(required_pad_size != null ? $"and [{nameof(Station.MaxPadSize)}] >= @required_pad_size\n" : string.Empty) +
				(facilities_incl != null ? $"and ([{nameof(Station.Facilities)}] & @facilities_incl) = @facilities_incl\n" : string.Empty) +
				(facilities_excl != null ? $"and ([{nameof(Station.Facilities)}] | @facilities_excl) = 0\n" : string.Empty) +
				(ignore_planetary != null ? $"and [{nameof(Station.Planetary)}] = 0\n" : string.Empty) +
				(max_count != null ? $"limit @max_count\n" : string.Empty),
				new
				{
					system_id,
					match_name = $"{match_name}%",
					max_station_distance,
					required_pad_size = (int?)required_pad_size,
					facilities_incl = (int?)facilities_incl,
					facilities_excl = (int?)facilities_excl,
					max_count,
				},
				flags: buffered ? CommandFlags.Buffered : CommandFlags.None));
		}

		/// <summary>Enumerate star systems that contains a station matching 'match_station_name'</summary>
		public IEnumerable<StarSystem> EnumStarSystemsContainingStation(
			string match_station_name,
			int? max_count = null,
			bool? ignore_permitted = null,
			bool buffered = true
		)
		{
			return DB.Query<StarSystem>(new CommandDefinition(
				$"select y.* from {Table.StarSystems} as y\n" +
				$"inner join {Table.Stations} s on s.[{nameof(Station.SystemID)}] = y.[{nameof(StarSystem.ID)}]\n" +
				$"where s.[{nameof(Station.Name)}] like @match_station_name\n" +
				(ignore_permitted == true ? $"and [{nameof(StarSystem.NeedPermit)}] = 0\n" : string.Empty) +
				(max_count != null ? $"limit @max_count\n" : string.Empty),
				new
				{
					match_station_name = $"{match_station_name}%",
					max_count,
				},
				flags: buffered ? CommandFlags.Buffered : CommandFlags.None));
		}

		/// <summary>Populate the cache database</summary>
		public async Task BuildCache(bool force_rebuild)
		{
			using (StatusStack.NewStatusMessage("Building Cache Database..."))
			{
				// Ensure the latest dump files have been downloaded
				var output_dir = Settings.Instance.DataPath;
				var updates = (await Task.WhenAll(
					Web.DownloadFile(SourceFiles.SystemsPopulated, output_dir, Settings.Instance.DataAge),
					Web.DownloadFile(SourceFiles.Stations, output_dir, Settings.Instance.DataAge),
					Web.DownloadFile(SourceFiles.Commodities, output_dir, Settings.Instance.DataAge),
					Web.DownloadFile(SourceFiles.Listings, output_dir, Settings.Instance.DataAge),
					Web.DownloadFile(SourceFiles.LiveListings, output_dir, Settings.Instance.DataAge)
					)).ToDictionary(x => x.FileUrl, x => (Filepath: x.OutputFilepath, Downloaded: x.Downloaded));

				// Determine which tables need updating
				var merge_list_listings = force_rebuild || updates[SourceFiles.LiveListings].Downloaded;
				var build_commodities_table = force_rebuild || updates[SourceFiles.Commodities].Downloaded;
				var build_listings_table = force_rebuild || build_commodities_table || updates[SourceFiles.Listings].Downloaded;
				var build_stations_table = force_rebuild || updates[SourceFiles.Stations].Downloaded;
				var build_systems_table = force_rebuild || build_stations_table || updates[SourceFiles.SystemsPopulated].Downloaded;

				if (build_systems_table || build_stations_table || build_commodities_table || build_listings_table)
				{
					// Drop tables that are out of date (in order of foreign key reference dependencies)
					try
					{
						await DB.ExecuteAsync(new CommandDefinition(
							(build_listings_table ? $"drop table {Table.Listings};\n" : string.Empty) +
							(build_commodities_table ? $"drop table {Table.Commodities};\n" : string.Empty) +
							(build_commodities_table ? $"drop table {Table.CommodityCategories};\n" : string.Empty) +
							(build_stations_table ? $"drop table {Table.Stations};\n" : string.Empty) +
							(build_systems_table ? $"drop table {Table.StarSystems};\n" : string.Empty),
							cancellationToken: Shutdown));
					}
					catch (SQLiteException ex) when ((SQLiteErrorCode)ex.ErrorCode == SQLiteErrorCode.Corrupt)
					{
						Log.Write(ELogLevel.Error, ex, $"Cache database is corrupt, rebuilding");
						DB = null;
						Path_.DelFile(Filepath);
						DB = new SQLiteConnection(DBConnectionString);
						force_rebuild = true;
					}

					// Replace the now missing tables
					InitDBTables();
				}

				// Build the star systems table
				if (force_rebuild || build_systems_table)
					BuildSystemsTable(updates[SourceFiles.SystemsPopulated].Filepath);

				// Build the stations table
				if (force_rebuild || build_stations_table)
					BuildStationsTable(updates[SourceFiles.Stations].Filepath);

				// Build the commodities and commodity categories tables
				if (force_rebuild || build_commodities_table)
					BuildCommodityCategortiesTable(updates[SourceFiles.Commodities].Filepath);
				if (force_rebuild || build_commodities_table)
					BuildCommoditiesTable(updates[SourceFiles.Commodities].Filepath);

				// Build the listings table
				if (force_rebuild || build_listings_table)
					BuildListingsTable(updates[SourceFiles.Listings].Filepath);

				// Merge live listings data
				if (force_rebuild || merge_list_listings)
					MergeListings(updates[SourceFiles.LiveListings].Filepath);

				// Rebuild the star map
				if (Map.BuildNeeded)
					Map.BuildSystemMap(EnumStarSystems(buffered: false));
			}
		}

		/// <summary>Rebuild the systems table</summary>
		private void BuildSystemsTable(string systems_populated_jsonl)
		{
			Log.Write(ELogLevel.Info, $"Building star systems table");
			using (var msg = StatusStack.NewStatusMessage($"Building Systems Table: {0:P2}"))
			{
				m_cache_star_systems.Flush();
				Map.Invalidate();

				// Read each system by line
				using (var sr = new StreamReader(File.OpenRead(systems_populated_jsonl), Encoding.UTF8, false))
				using (var transaction = DB.BeginTransaction())
				using (var cmd = DB.CreateCommand())
				{
					// Create a command for inserting systems
					cmd.CommandText =
						$"insert into {Table.StarSystems} (\n" +
						$"  [{nameof(StarSystem.ID)}],\n" +
						$"  [{nameof(StarSystem.EdsmID)}],\n" +
						$"  [{nameof(StarSystem.Name)}],\n" +
						$"  [{nameof(StarSystem.X)}],\n" +
						$"  [{nameof(StarSystem.Y)}],\n" +
						$"  [{nameof(StarSystem.Z)}],\n" +
						$"  [{nameof(StarSystem.NeedPermit)}],\n" +
						$"  [{nameof(StarSystem.UpdatedAt)}]\n" +
						$") values (\n" +
						$"  @id, @edsm_id, @name, @x, @y, @z, @need_permit, @updated_at\n" +
						$")\n";
					cmd.Parameters.Add("@id", R<StarSystem>.Type(x => x.ID).DbType());
					cmd.Parameters.Add("@name", R<StarSystem>.Type(x => x.Name).DbType());
					cmd.Parameters.Add("@x", R<StarSystem>.Type(x => x.X).DbType());
					cmd.Parameters.Add("@y", R<StarSystem>.Type(x => x.Y).DbType());
					cmd.Parameters.Add("@z", R<StarSystem>.Type(x => x.Z).DbType());
					cmd.Parameters.Add("@need_permit", R<StarSystem>.Type(x => x.NeedPermit).DbType());
					cmd.Parameters.Add("@updated_at", R<StarSystem>.Type(x => x.UpdatedAt).DbType());
					cmd.Parameters.Add("@edsm_id", R<StarSystem>.Type(x => x.EdsmID).DbType());
					cmd.Transaction = transaction;

					// Parse each object
					var last_progress_update = 0.0;
					var stream_length = sr.BaseStream.Length;
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
						cmd.Parameters["@need_permit"].Value = jobj["needs_permit"].Value<bool>();
						cmd.Parameters["@updated_at"].Value = jobj["updated_at"].Value<long>();
						cmd.Parameters["@edsm_id"].Value = jobj["edsm_id"].Value<long?>();
						cmd.ExecuteNonQuery();

						// Report progress
						var fraction_done = (double)sr.BaseStream.Position / stream_length;
						if (fraction_done - last_progress_update >= 0.01)
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
		private void BuildStationsTable(string stations_jsonl)
		{
			Log.Write(ELogLevel.Info, $"Building stations table");
			using (var msg = StatusStack.NewStatusMessage($"Building Stations Table: {0:P2}"))
			{
				m_cache_stations.Flush();

				// Read each station by line
				using (var sr = new StreamReader(File.OpenRead(stations_jsonl), Encoding.UTF8, false))
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
						$"  @id, @name, @system_id, @type, @distance, @facilities, @max_pad_size, @planetary, @updated_at\n" +
						$")\n";
					cmd.Parameters.Add("@id", R<Station>.Type(x => x.ID).DbType());
					cmd.Parameters.Add("@name", R<Station>.Type(x => x.Name).DbType());
					cmd.Parameters.Add("@system_id", R<Station>.Type(x => x.SystemID).DbType());
					cmd.Parameters.Add("@type", R<Station>.Type(x => x.Type).DbType());
					cmd.Parameters.Add("@distance", R<Station>.Type(x => x.Distance).DbType());
					cmd.Parameters.Add("@facilities", R<Station>.Type(x => x.Facilities).DbType());
					cmd.Parameters.Add("@max_pad_size", R<Station>.Type(x => x.MaxPadSize).DbType());
					cmd.Parameters.Add("@planetary", R<Station>.Type(x => x.Planetary).DbType());
					cmd.Parameters.Add("@updated_at", R<Station>.Type(x => x.UpdatedAt).DbType());
					cmd.Transaction = transaction;

					var last_progress_update = 0.0;
					var stream_length = sr.BaseStream.Length;
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
						cmd.Parameters["@updated_at"].Value = jobj["updated_at"].Value<long>();
						cmd.ExecuteNonQuery();

						// Report progress
						var fraction_done = (double)sr.BaseStream.Position / stream_length;
						if (fraction_done - last_progress_update >= 0.01)
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

		/// <summary>Rebuild the commodity categories table</summary>
		private void BuildCommodityCategortiesTable(string commodities_json)
		{
			Log.Write(ELogLevel.Info, $"Building commodity categories tables");
			using (var msg = StatusStack.NewStatusMessage($"Building Commodity Categories Tables: {0:P2}"))
			{
				m_cache_market.Flush();

				// Read commodities and find the unique categories
				using (var sr = new StreamReader(File.OpenRead(commodities_json), Encoding.UTF8, false))
				using (var reader = new JsonTextReader(sr))
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

					var last_progress_update = 0.0;
					var stream_length = sr.BaseStream.Length;
					var serializer = new JsonSerializer();
					var categories = new HashSet<string>();
					for (; reader.Read();)
					{
						if (reader.TokenType != JsonToken.StartObject) continue;
						var jobj = serializer.Deserialize<JObject>(reader);
						if (categories.Contains(jobj["id"].Value<string>()))
							continue;

						Shutdown.ThrowIfCancellationRequested();

						cmd.Reset();
						cmd.Parameters["@id"].Value = jobj["id"].Value<long>();
						cmd.Parameters["@name"].Value = jobj["name"].Value<string>();
						cmd.ExecuteNonQuery();

						// Report progress
						var fraction_done = (double)sr.BaseStream.Position / stream_length;
						if (fraction_done - last_progress_update >= 0.01)
						{
							msg.Message = $"Building Commodity Categories Table: {fraction_done:P2}";
							last_progress_update = fraction_done;
						}
					}

					transaction.Commit();
					msg.Message = $"Building Commodity Categories Table: {1:P2}";
				}
			}
		}

		/// <summary>Rebuild the commodities table</summary>
		private void BuildCommoditiesTable(string commodities_json)
		{
			Log.Write(ELogLevel.Info, $"Building commodities tables");
			using (var msg = StatusStack.NewStatusMessage($"Building Commodities Tables: {0:P2}"))
			{
				m_cache_market.Flush();

				// Read commodities
				using (var sr = new StreamReader(File.OpenRead(commodities_json), Encoding.UTF8, false))
				using (var reader = new JsonTextReader(sr))
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

					var last_progress_update = 0.0;
					var stream_length = sr.BaseStream.Length;
					var serializer = new JsonSerializer();
					for (; reader.Read();)
					{
						if (reader.TokenType != JsonToken.StartObject) continue;
						var jobj = serializer.Deserialize<JObject>(reader);
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
						var fraction_done = (double)sr.BaseStream.Position / stream_length;
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
		private void BuildListingsTable(string listings_csv)
		{
			Log.Write(ELogLevel.Info, $"Building listings table");
			using (var msg = StatusStack.NewStatusMessage($"Building Listings Table: {0:P2}"))
			{
				m_cache_market.Flush();

				// Read each listing by row
				var rows = CSVData.Parse(listings_csv, false, x => msg.Message = $"Building Listings Table: {x:P2}");
				using (var transaction = DB.BeginTransaction())
				using (var cmd = DB.CreateCommand())
				{
					cmd.CommandText =
						$"insert into {Table.Listings} (\n" +
						$"  [{nameof(Listing.ID)}],\n" +
						$"  [{nameof(Listing.StationID)}],\n" +
						$"  [{nameof(Listing.CommodityID)}],\n" +
						$"  [{nameof(Listing.Supply)}],\n" +
						$"  [{nameof(Listing.SupplyBracket)}],\n" +
						$"  [{nameof(Listing.BuyPrice)}],\n" +
						$"  [{nameof(Listing.SellPrice)}],\n" +
						$"  [{nameof(Listing.Demand)}],\n" +
						$"  [{nameof(Listing.DemandBracket)}],\n" +
						$"  [{nameof(Listing.UpdatedAt)}]\n" +
						$") values (\n" +
						$"  @id, @station_id, @commodity_id, @supply, @supply_bracket, @buy_price, @sell_price, @demand, @demand_bracket, @updated_at\n" +
						$")\n";
					cmd.Parameters.Add("@id", R<Listing>.Type(x => x.ID).DbType());
					cmd.Parameters.Add("@station_id", R<Listing>.Type(x => x.StationID).DbType());
					cmd.Parameters.Add("@commodity_id", R<Listing>.Type(x => x.CommodityID).DbType());
					cmd.Parameters.Add("@supply", R<Listing>.Type(x => x.Supply).DbType());
					cmd.Parameters.Add("@supply_bracket", R<Listing>.Type(x => x.SupplyBracket).DbType());
					cmd.Parameters.Add("@buy_price", R<Listing>.Type(x => x.BuyPrice).DbType());
					cmd.Parameters.Add("@sell_price", R<Listing>.Type(x => x.SellPrice).DbType());
					cmd.Parameters.Add("@demand", R<Listing>.Type(x => x.Demand).DbType());
					cmd.Parameters.Add("@demand_bracket", R<Listing>.Type(x => x.DemandBracket).DbType());
					cmd.Parameters.Add("@updated_at", R<Listing>.Type(x => x.UpdatedAt).DbType());
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

		/// <summary>Merge listings data into the listings table</summary>
		private void MergeListings(string live_listings_csv)
		{
			Log.Write(ELogLevel.Info, $"Merging live listings");
			using (var msg = StatusStack.NewStatusMessage($"Merging Live Listings Data: {0:P2}"))
			{
				m_cache_market.Flush();

				// Read each listing by row
				var rows = CSVData.Parse(live_listings_csv, false, x => msg.Message = $"Merging Live Listings: {x:P2}");
				using (var transaction = DB.BeginTransaction())
				using (var cmd = DB.CreateCommand())
				{
					cmd.CommandText =
						$"insert or replace into {Table.Listings} (\n" +
						$"  [{nameof(Listing.ID)}],\n" +
						$"  [{nameof(Listing.StationID)}],\n" +
						$"  [{nameof(Listing.CommodityID)}],\n" +
						$"  [{nameof(Listing.Supply)}],\n" +
						$"  [{nameof(Listing.SupplyBracket)}],\n" +
						$"  [{nameof(Listing.BuyPrice)}],\n" +
						$"  [{nameof(Listing.SellPrice)}],\n" +
						$"  [{nameof(Listing.Demand)}],\n" +
						$"  [{nameof(Listing.DemandBracket)}],\n" +
						$"  [{nameof(Listing.UpdatedAt)}]\n" +
						$") select\n" +
						$"  @id, @station_id, @commodity_id, @supply, @supply_bracket, @buy_price, @sell_price, @demand, @demand_bracket, @updated_at\n" +
						$"where (select 1 from {Table.Stations   } where [{nameof(Station.ID)  }] = @station_id)\n" +
						$"  and (select 1 from {Table.Commodities} where [{nameof(Commodity.ID)}] = @commodity_id)\n";
					cmd.Parameters.Add("@id", R<Listing>.Type(x => x.ID).DbType());
					cmd.Parameters.Add("@station_id", R<Listing>.Type(x => x.StationID).DbType());
					cmd.Parameters.Add("@commodity_id", R<Listing>.Type(x => x.CommodityID).DbType());
					cmd.Parameters.Add("@supply", R<Listing>.Type(x => x.Supply).DbType());
					cmd.Parameters.Add("@supply_bracket", R<Listing>.Type(x => x.SupplyBracket).DbType());
					cmd.Parameters.Add("@buy_price", R<Listing>.Type(x => x.BuyPrice).DbType());
					cmd.Parameters.Add("@sell_price", R<Listing>.Type(x => x.SellPrice).DbType());
					cmd.Parameters.Add("@demand", R<Listing>.Type(x => x.Demand).DbType());
					cmd.Parameters.Add("@demand_bracket", R<Listing>.Type(x => x.DemandBracket).DbType());
					cmd.Parameters.Add("@updated_at", R<Listing>.Type(x => x.UpdatedAt).DbType());
					cmd.Transaction = transaction;

					long ParseInt64(string s) => s.Length != 0 ? long.Parse(s) : 0L;
					int ParseInt32(string s) => s.Length != 0 ? int.Parse(s) : 0;

					// Update the listings table
					foreach (var row in rows.Skip(1))
					{
						try
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
						catch (Exception ex)
						{
							Log.Write(ELogLevel.Error, ex, $"Failed to update listing. Row data: '{string.Join(",", row)}'");
						}
					}

					transaction.Commit();
					msg.Message = $"Merging Live Listings: {1:P2}";
				}
			}
		}

		/// <summary>Merge market data for a station (produced by the ED journal file)</summary>
		public async Task MergeMarketUpdate(string market_json)
		{
			using (var msg = StatusStack.NewStatusMessage($"Merging Market Update: {0:P2}"))
			{
				try
				{
					var serializer = new JsonSerializer();
					using (var sr = new StreamReader(File.OpenRead(market_json)))
					using (var reader = new JsonTextReader(sr))
					using (var transaction = DB.BeginTransaction())
					{
						var jobj = serializer.Deserialize<JObject>(reader);
						var system = GetStarSystem(jobj["StarSystem"].Value<string>()).Result;
						var station = GetStation(system.ID, jobj["StationName"].Value<string>()).Result;
						Log.Write(ELogLevel.Info, $"Merging market data for {system.Name}/{station.Name}");
						m_cache_market.Invalidate(station.ID);

						foreach (var item in (JArray)jobj["Items"])
						{
							await DB.ExecuteAsync(new CommandDefinition(
								$"insert or replace into {Table.Listings} (\n" +
								$"  [{nameof(Listing.ID)}],\n" +
								$"  [{nameof(Listing.StationID)}],\n" +
								$"  [{nameof(Listing.CommodityID)}],\n" +
								$"  [{nameof(Listing.Supply)}],\n" +
								$"  [{nameof(Listing.SupplyBracket)}],\n" +
								$"  [{nameof(Listing.BuyPrice)}],\n" +
								$"  [{nameof(Listing.SellPrice)}],\n" +
								$"  [{nameof(Listing.Demand)}],\n" +
								$"  [{nameof(Listing.DemandBracket)}],\n" +
								$"  [{nameof(Listing.UpdatedAt)}]\n" +
								$") select\n" +
								$"  l.[{nameof(Listing.ID)}]," +
								$"  @station_id," +
								$"  c.[{nameof(Commodity.ID)}]," +
								$"  @supply," +
								$"  @supply_bracket," +
								$"  @buy_price," +
								$"  @sell_price," +
								$"  @demand," +
								$"  @demand_bracket," +
								$"  @updated_at\n" +
								$"from [Listings] as l\n" +
								$"inner join {Table.Commodities} c on l.[{nameof(Listing.CommodityID)}] = c.[{nameof(Commodity.ID)}]\n" +
								$"where c.[{nameof(Commodity.EDID)}] = @edid\n" +
								$"  and l.[{nameof(Listing.StationID)}] = @station_id",
								new
								{
									edid = item["id"].Value<long>(),
									station_id = station.ID,
									supply = item["Stock"].Value<int>(),
									supply_bracket = item["StockBracket"].Value<int>(),
									buy_price = item["BuyPrice"].Value<int>(),
									sell_price = item["SellPrice"].Value<int>(),
									demand = item["Demand"].Value<long>(),
									demand_bracket = item["DemandBracket"].Value<int>(),
									updated_at = DateTimeOffset.Now.ToUnixTimeSeconds(),
								},
								cancellationToken: Shutdown));
						}

						transaction.Commit();
						msg.Message = $"Merging Market Update: {1:P2}";
					}
				}
				catch (Exception ex)
				{
					Log.Write(ELogLevel.Error, ex, $"Parsing market data failed: '{market_json}'");
				}
			}
		}

		/// <summary>Initialise the DB tables</summary>
		private void InitDBTables()
		{
			using (var sql_stream = GetType().Assembly.GetManifestResourceStream($"EDTradeAdvisor.Core.src.DataProvider.CacheSetup.sql"))
			using (var sr = new StreamReader(sql_stream, Encoding.UTF8))
				DB.Execute(sr.ReadToEnd());
		}

		/// <summary>Bulk data files</summary>
		public static class SourceFiles
		{
			public const string SystemsPopulated = "https://eddb.io/archive/v6/systems_populated.jsonl";
			public const string Stations = "https://eddb.io/archive/v6/stations.jsonl";
			public const string Commodities = "https://eddb.io/archive/v6/commodities.json";
			public const string Modules = "https://eddb.io/archive/v6/modules.json";
			public const string Listings = "https://eddb.io/archive/v6/listings.csv";
			public const string LiveListings = "http://elite.tromador.com/files/listings-live.csv";
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
