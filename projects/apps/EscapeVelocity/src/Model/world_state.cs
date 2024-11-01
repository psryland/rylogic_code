using System.ComponentModel;
using Rylogic.Utility;

namespace EscapeVelocity
{
	public class WorldState :INotifyPropertyChanged
	{
		/// <summary>The generated game constants</summary>
		private readonly GameConstants m_consts;

		/// <summary>The time remaining till the star goes nova (s)</summary>
		public double TimeTillNova
		{
			get { return m_time_till_nova; }
			private set { m_time_till_nova = value; OnPropertyChanged(nameof(TimeTillNova)); }
		}
		private double m_time_till_nova;

		// The distance to the star
		public ResearchEffort m_star_distance_research;

		// The mass of the star
		public ResearchEffort m_star_mass_research;

		/// <summary>The acceleration required of the ship in order to reach escape velocity in time (m/s²)</summary>
		public double m_required_acceleration;

		/// <summary>The average local temperature, this increases as the star nears super nova (°C)</summary>
		public double AverageLocalTemperature;

		public WorldState(GameConstants consts)
		{
			m_consts                    = consts;
			TimeTillNova                = consts.InitialTimeTillNova;
			m_star_distance_research    = new ResearchEffort(consts.m_star_distance_discovery_effort, 0.0, consts);
			m_star_mass_research        = new ResearchEffort(consts.m_star_mass_discovery_effort, 0.0, consts);
			m_required_acceleration     = 0;
			AverageLocalTemperature = 0;
		}

		// Advance the world state by 'elapsed' seconds
		// Returns false if the star has gone nova
		public bool Step(double elapsed)
		{
			elapsed *= m_consts.m_time_scaler;

			// Reduce the count down
			TimeTillNova -= elapsed;
			if (TimeTillNova <= 0.0)
				return false;

			// Given the time remaining, this is the average acceleration that the ship needs to escape
			m_required_acceleration = m_consts.EscapeVelocity / TimeTillNova;

			// todo
			AverageLocalTemperature = 16.18;
				//pr::KelvinToCelsius(Consts.m_star_temperature * sqrt(Consts.m_star_radius
			return true;
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		protected virtual void OnPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}