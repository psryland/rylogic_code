using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pr.container;

namespace Tradee
{
	public class AlarmModel :IDisposable
	{
		public AlarmModel()
		{
			AlarmList = new BindingSource<Alarm> { DataSource = new BindingListEx<Alarm>() };
		}
		public virtual void Dispose()
		{ }

		/// <summary>The collection of alarms</summary>
		public BindingSource<Alarm> AlarmList { get; private set; }

		/// <summary>Add a reminder item to the alarm list</summary>
		public void AddReminder()
		{
			AlarmList.Add(new AlarmReminder(DateTimeOffset.Now));
		}
	}

	/// <summary>Base class for entries in the alarm list</summary>
	public class Alarm
	{
		public Alarm(DateTimeOffset when)
		{
			Id = Guid.NewGuid();
			When = when;
		}

		/// <summary>A unique ID for the alarm</summary>
		public Guid Id { get; private set; }

		/// <summary>When the alarm should trigger</summary>
		public DateTimeOffset When { get; set; }
	}

	public class AlarmReminder :Alarm
	{
		public AlarmReminder(DateTimeOffset when) :base(when)
		{
		}
	}
}
