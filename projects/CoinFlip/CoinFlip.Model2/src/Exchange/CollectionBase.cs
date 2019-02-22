using System;
using Rylogic.Container;
using Rylogic.Utility;

namespace CoinFlip
{
	public class CollectionBase<TKey, TValue> : BindingDict<TKey, TValue>
	{
		public CollectionBase(Exchange exch)
		{
			Exch = exch;
			Updated = new ConditionVariable<DateTimeOffset>(DateTimeOffset.MinValue);
		}
		public CollectionBase(CollectionBase<TKey, TValue> rhs)
			: this(rhs.Exch)
		{ }

		/// <summary>The owning exchange</summary>
		public Exchange Exch { get; }

		/// <summary>Wait-able object for update notification</summary>
		public ConditionVariable<DateTimeOffset> Updated { get; }

		/// <summary>The time when data in this collection was last updated. Note: *NOT* when collection changed, when the elements in the collection changed</summary>
		public DateTimeOffset LastUpdated
		{
			get { return m_last_updated; }
			set
			{
				m_last_updated = value;
				Updated.NotifyAll(value);
			}
		}
		private DateTimeOffset m_last_updated;
	}
}
