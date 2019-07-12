using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ExchApi.Common
{
#if false
	public class SubscriptionContainer
	{
		public SubscriptionContainer(IExchangeApi api)
		{
			Api = api;
			Subs = new HashSet<Subscription>();
			Active = new Dictionary<int, Subscription>();
		}

		/// <summary>The owning API instance</summary>
		private IExchangeApi Api { get; }

		/// <summary>The subscription instances</summary>
		public HashSet<Subscription> Subs { get; }

		/// <summary>Subscriptions that are active (i.e. have a channel id)</summary>
		public Dictionary<int, Subscription> Active { get; }

		/// <summary>O(1) lookup of a subscription by channel id</summary>
		public Subscription this[int channel_id]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				return Active.TryGetValue(channel_id, out var sub) ? sub : null;
			}
		}

		/// <summary>Find a subscription of type 'T' that satisfies 'pred'</summary>
		public T Find<T>(Func<T, bool> pred)
		{
			Debug.Assert(Misc.AssertMainThread());
			return Subs.OfType<T>().FirstOrDefault(pred);
		}
		public T Find<T>()
		{
			return Find<T>(x => true);
		}

		/// <summary>Add a subscription instance</summary>
		public async Task Add(Subscription sub)
		{
			Debug.Assert(Misc.AssertMainThread());
			Debug.Assert(sub.Api == null);
			sub.Api = Api;

			// If there are existing subscriptions equal to 'sub' stop and remove them
			var tasks = Subs.Where(x => Equals(x, sub)).Select(x => Remove(x));
			await Task.WhenAll(tasks);

			// Add 'sub' to the collection
			Subs.Add(sub);
			await sub.Start();
		}

		/// <summary>Stop and remove a subscription</summary>
		public async Task Remove(Subscription sub)
		{
			Debug.Assert(Misc.AssertMainThread());

			// Stop if active
			await sub.Stop();

			// Remove from the active subscriptions lookup table
			if (sub.ChannelId != null)
				Active.Remove(sub.ChannelId.Value);

			// Remove from the subs collection
			Subs.Remove(sub);
			sub.Api = null;
		}
		public async Task Remove(int channel_id)
		{
			if (Active.TryGetValue(channel_id, out var sub))
				await Remove(sub);
		}
		public async Task RemoveIf(Func<Subscription, bool> pred)
		{
			var tasks = Subs.Where(x => pred(x)).Select(x => Remove(x));
			await Task.WhenAll(tasks);
		}

		/// <summary>Stop and remove all subscriptions</summary>
		public async Task Clear()
		{
			await StopAll();
			Subs.Clear();
			Active.Clear();
		}

		/// <summary>Start all subscriptions that are not already running</summary>
		public async Task StartAll()
		{
			Debug.Assert(Misc.AssertMainThread());
			var tasks = Subs.Where(x => x.State == Subscription.EState.Initial).Select(x => x.Start());
			await Task.WhenAll(tasks);
		}
		public async Task StopAll()
		{
			Debug.Assert(Misc.AssertMainThread());
			var tasks = Subs.Where(x => x.State == Subscription.EState.Running).Select(x => x.Stop());
			await Task.WhenAll(tasks);
		}
	}
#endif
}
