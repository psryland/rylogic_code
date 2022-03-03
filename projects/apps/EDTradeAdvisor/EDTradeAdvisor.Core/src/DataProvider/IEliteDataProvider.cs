using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EDTradeAdvisor.DomainObjects;
using Rylogic.Maths;

namespace EDTradeAdvisor
{
	public interface IEliteDataProvider : IDisposable
	{
		/// <summary>Search the map for nearby systems</summary>
		IEnumerable<StarSystemRef> Search(v4 position, double radius);

		/// <summary>Return a star system by Name/ID</summary>
		Task<StarSystem> GetStarSystem(string name);
		Task<StarSystem> GetStarSystem(long id);

		/// <summary>Return a station by Name/ID</summary>
		Task<Station> GetStation(long system_id, string name);
		Task<Station> GetStation(long id);

		/// <summary>Return the market data for a given station (by id)</summary>
		Task<Market> GetMarketData(long station_id);

		/// <summary>Enumerate all stars</summary>
		IEnumerable<StarSystem> EnumStarSystems(
			string match_name = null,
			int? max_count = null,
			bool? ignore_permitted = null,
			bool buffered = true
		);

		/// <summary>Enumerate stations</summary>
		IEnumerable<Station> EnumStations(
			long? system_id = null,
			string match_name = null,
			int? max_count = null,
			long? max_station_distance = null,
			ELandingPadSize? required_pad_size = null,
			EFacilities? facilities_incl = null,
			EFacilities? facilities_excl = null,
			bool? ignore_planetary = null,
			bool buffered = true
		);

		/// <summary>Enumerate star systems that contains a station matching 'match_station_name'</summary>
		IEnumerable<StarSystem> EnumStarSystemsContainingStation(
			string match_station_name,
			int? max_count = null,
			bool? ignore_permitted = null,
			bool buffered = true
		);

		/// <summary>Ensure caches are up to date</summary>
		/// <returns></returns>
		Task BuildCache(bool force_rebuild);

		/// <summary>Merge market data for a station (produced by the ED journal file)</summary>
		Task MergeMarketUpdate(string market_json);
	}
}
