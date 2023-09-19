using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTracker
{
	public class TimeTotal :INotifyPropertyChanged
	{
		public TimeTotal(string task_name, IEnumerable<TimePeriod> periods)
		{
			TaskName = task_name;
			Periods = new List<TimePeriod>(periods);
		}

		/// <summary>The name of the task</summary>
		public string TaskName { get; }

		/// <summary>The collection of time periods that contribute to this total</summary>
		public List<TimePeriod> Periods { get; }

		/// <summary>The total time spend on this task</summary>
		public TimeSpan Total => new(Periods.Sum(x => x.Duration.Ticks));

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
