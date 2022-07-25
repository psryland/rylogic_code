using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Rylogic.Common;

namespace TimeTracker
{
	public class TimeTotal :INotifyPropertyChanged
	{
		public TimeTotal(string task_name, IEnumerable<TimePeriod> periods)
		{
			TaskName = task_name;
			Periods = new List<TimePeriod>(periods);
			foreach (var p in Periods)
				p.PropertyChanged += WeakRef.MakeWeak(HandlePropChanged, h => p.PropertyChanged -= h);
			
			void HandlePropChanged(object? sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName == nameof(TimePeriod.Duration))
					NotifyPropertyChanged(nameof(Total));
			}
		}


		/// <summary>The name of the task</summary>
		public string TaskName { get; }

		/// <summary>The collection of time periods that contribute to this total</summary>
		public List<TimePeriod> Periods { get; }

		/// <summary>The total time spend on this task</summary>
		public TimeSpan Total => new TimeSpan(Periods.Sum(x => x.Duration.Ticks));

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
