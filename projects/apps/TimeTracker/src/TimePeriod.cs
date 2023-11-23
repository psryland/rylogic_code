using System;
using System.Collections.Generic;
using System.ComponentModel;
using Rylogic.Extn;

namespace TimeTracker
{
	public class TimePeriod :INotifyPropertyChanged
	{
		public TimePeriod(DateTimeOffset start, string task_name, List<TimePeriod> collection)
		{
			m_collection = collection;
			TaskName = task_name;
			Start = start;
		}
		public TimePeriod(string task_name, List<TimePeriod> collection)
			:this(DateTimeOffset.Now, task_name, collection)
		{}

		/// <summary>The ordered collection that the time period is in</summary>
		private readonly List<TimePeriod> m_collection;
		private int m_idx = -1;

		/// <summary>The task done in this time period</summary>
		public string TaskName
		{
			get => m_task_name;
			set
			{
				if (m_task_name == value) return;
				m_task_name = value;
				NotifyPropertyChanged(nameof(TaskName));
			}
		}
		private string m_task_name = String.Empty;

		/// <summary>When the time period started</summary>
		public DateTimeOffset Start
		{
			get => m_start;
			set
			{
				if (m_start == value) return;
				m_start = value;
				m_collection.Sort(x => x.Start, -1);
				NotifyPropertyChanged(nameof(Start));
				NotifyPropertyChanged(nameof(Duration));
				Next?.NotifyPropertyChanged(nameof(Duration));
				Prev?.NotifyPropertyChanged(nameof(Duration));
			}
		}
		private DateTimeOffset m_start;

		/// <summary>The length of this time period</summary>
		public TimeSpan Duration
		{
			get
			{
				// Find the duration until the next task
				return Next is TimePeriod next
					? next.Start - Start
					: DateTimeOffset.Now - Start;
			}
		}

		/// <summary>Return the next task</summary>
		private TimePeriod? Next
		{
			get
			{
				// Get the index of 'this' in 'm_collection'
				var idx = m_idx == -1 || m_idx >= m_collection.Count || !ReferenceEquals(m_collection[m_idx], this)
					? m_idx = m_collection.IndexOf(this)
					: m_idx;

				// Return the next task in the collection
				return idx > 0
					? m_collection[idx - 1]
					: null;
			}
		}

		/// <summary>Return the previous task</summary>
		private TimePeriod? Prev
		{
			get
			{
				// Get the index of 'this' in 'm_collection'
				var idx = m_idx == -1 || m_idx >= m_collection.Count || !ReferenceEquals(m_collection[m_idx], this)
					? m_idx = m_collection.IndexOf(this)
					: m_idx;

				// Return the previous task in the collection
				return idx < m_collection.Count - 1
					? m_collection[idx + 1]
					: null;
			}
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
