namespace EDTradeAdvisor.DomainObjects
{
	public class Location
	{
		public Location(StarSystem system, Station station)
		{
			System = system;
			Station = station;
		}

		/// <summary></summary>
		public StarSystem System { get; set; }

		/// <summary></summary>
		public Station Station { get; set; }
	}
}
