using System;

namespace EscapeVelocity
{
	public class ResearchEffort
	{
		/// <summary>The generated game constants</summary>
		private readonly GameConstants m_consts;

		/// <summary>The work left to be done for this discovery (man-days)</summary>
		public double m_remaining_effort;

		/// <summary>The fraction of total research resources assigned to this research effort (0->1)</summary>
		public double m_assigned_resources;

		/// <summary>The time remaining until this research effort results in a discovery (s)</summary>
		public double m_time_till_discovery;

		// True if the research is complete
		public bool Complete { get { return m_remaining_effort < double.Epsilon; } }

		public ResearchEffort(double remaining_effort, double resources, GameConstants consts)
		{
			m_consts = consts;
			m_remaining_effort = remaining_effort;
			m_assigned_resources = resources;
			m_time_till_discovery = double.MaxValue;
			Step(0.0);
		}

		// Update the remaining time till discovery based on the assigned resources
		void Step(double elapsed)
		{
			// Reduce the remaining effort by the assigned man power working for 'elapsed' seconds
			double man_power = m_assigned_resources * m_consts.m_total_man_power;
			m_remaining_effort -= TimeSpan.FromSeconds(man_power * elapsed).TotalDays;
			if (m_remaining_effort <= double.Epsilon)
				m_remaining_effort = 0.0;

			// Determine the estimated time till discovery
			m_time_till_discovery = double.MaxValue;
			if (man_power > double.Epsilon)
			{
				m_time_till_discovery -= TimeSpan.FromDays(m_remaining_effort / man_power).TotalSeconds;
				if (m_time_till_discovery <= double.Epsilon)
					m_time_till_discovery = 0.0;
			}
		}
	}
}