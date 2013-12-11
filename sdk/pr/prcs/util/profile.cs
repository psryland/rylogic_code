#define PR_PROFILE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace pr.util
{
	/// <summary>Profiling objects and methods</summary>
	public static class Profile
	{
		/// <summary>
		/// Factory method for getting (or creating) a profile instances by name.
		/// This method has O(log(n)) complexity where n is the number of existing
		/// profile instances. In most cases called Profile.Get("name").Start() will
		/// be fast enough. If not then you can always store the Instance reference.</summary>
		public static Instance Get(string name)
		{
			return mgr.get(name);
		}

		/// <summary>Creates (or gets) an RAII profile scope by name.</summary>
		public static Scope Scope(string name)
		{
			var p = Get(name);
			return util.Scope.Create(
				() => p.Start(),
				() => p.Stop());
		}

		/// <summary>
		/// Starts the time frame over which profiling is taking place.
		/// A profiling frame typically represents a session of multiple calls to the Start()/Stop()
		/// methods on multiple profiling blocks. Note: it is valid to have only one frame for a
		/// profiling session</summary>
		[Conditional("PR_PROFILE")] public static void FrameBegin()
		{
			mgr.FrameBeginImpl();
		}

		/// <summary>
		/// Ends the time frame over which profiling is taking place.
		/// After ending a frame the profiling data can be sampled.</summary>
		[Conditional("PR_PROFILE")] public static void FrameEnd()
		{
			mgr.FrameEndImpl();
		}

		/// <summary>Creates and RAII scope for a frame</summary>
		public static Scope FrameScope()
		{
			return util.Scope.Create(
				() => FrameBegin(),
				() => FrameEnd());
		}

		/// <summary>
		/// Sample the current collected profile data and send it out a stream.
		/// A sample represents one or more profiling frames and allows averaged statistics such as
		/// calls per frame, time per frame, etc. The user is free to choose where FrameBegin()/FrameEnd() calls
		/// are made, but a sample should only be called after FrameEnd() and before the next FrameBegin().
		/// 'ouput' is a stream so that raw profiling data can be piped to an external process/machine
		/// if necessary. Pass null if you want to sample but have nothing to send the results to.</summary>
		[Conditional("PR_PROFILE")] public static void Sample(BinaryWriter output, bool reset_frame_data)
		{
			mgr.SampleImpl(output, reset_frame_data);
		}

		/// <summary>
		/// A helper for generating a string summary of the current profile data.
		/// Intended for simple use. More advanced profiling should use the 'Sample' method.</summary>
		public static string Summary
		{
			get
			{
				// A stream for linking collected profile data to the profile output
				var s = new MemoryStream();
				var bw = new BinaryWriter(s);
				var br = new BinaryReader(s);
				Sample(bw, true);
				s.Position = 0;

				// Receive and display profile data
				var results = new Results();
				results.Collect(br);
				results.Data.SortByInclTime();
				return results.Summary;
			}
		}

		/// <summary>A marker tag to help synchronise profile data between client and server</summary>
		public const uint PacketStartTag = 0xfeeffeef;

		/// <summary>Default maximum profile packet size</summary>
		public const uint DefaultMaxPacketSize = 65536;

		/// <summary>The expected maximum size of profiling packets
		/// created in Profile.Sample() and received in Results.Collect()</summary>
		public static uint MaxPacketSize = DefaultMaxPacketSize;

		/// <summary>A map from profile id to associated caller data</summary>
		public class CallerMap :Dictionary<int, Caller> {};

		/// <summary>
		/// Caller data - tracks the tree structure of calls to nested profile blocks.
		/// Block 'A' has a collection of 'Caller's which are all the parent profile blocks that 'A' is nested within</summary>
		public class Caller
		{
			/// <summary>The ID of the profile making the call</summary>
			public int  ProfileId;

			/// <summary>The number of times the owning profile has been called from the profile with id 'ProfileId'</summary>
			public int  CallCount;

			/// <summary>The amount of time spent in the owning profile while being called from the profile with id 'ProfileId'</summary>
			public long TotalCallTime;

			internal void Reset()
			{
				CallCount = 0;
				TotalCallTime = 0;
			}
			public override string ToString()
			{
				return string.Format("[{0}] count: {1} time: {2}" ,ProfileId ,CallCount ,TotalCallTime);
			}

			/// <summary>Combine the statistics of 'caller' with this Caller</summary>
			public void Merge(Caller rhs)
			{
				CallCount     += rhs.CallCount;
				TotalCallTime += rhs.TotalCallTime;
			}
		};

		/// <summary>
		/// Profile data block - the lowest level unit of profile data.
		/// Collects:
		///  * number of times called,
		///  * time spent within the block,
		///  * time spent within the block excluding time spent in child blocks</summary>
		public class Block
		{
			/// <summary>The name of the profile block</summary>
			public string Name;

			/// <summary>A unique id for this profile</summary>
			public int  ProfileId;

			/// <summary>The number of times Start()/Stop() has been called for this profile (i.e. call count)</summary>
			public int  CallCount;

			/// <summary>The total time spent within Start()/Stop() including calls to child profiles (total time)</summary>
			public long SelfInclChildTimeTicks;

			/// <summary>The total time spent within Start()/Stop() excluding calls to child profiles (self time)</summary>
			public long SelfExclChildTimeTicks;

			/// <summary>The collection of parent profile blocks that call this profile block</summary>
			public CallerMap Callers;

			internal Block(string name, int id)
			{
				Name                   = name;
				ProfileId              = id;
				Callers                = new CallerMap();
				SelfInclChildTimeTicks = 0;
				SelfExclChildTimeTicks = 0;
			}
			internal Block(Block rhs)
			{
				Name                   = rhs.Name;
				ProfileId              = rhs.ProfileId;
				CallCount              = rhs.CallCount;
				SelfInclChildTimeTicks = rhs.SelfInclChildTimeTicks;
				SelfExclChildTimeTicks = rhs.SelfExclChildTimeTicks;
				Callers                = rhs.Callers;
			}
			internal void Reset()
			{
				CallCount              = 0;
				SelfInclChildTimeTicks = 0;
				SelfExclChildTimeTicks = 0;
				foreach (var c in Callers) c.Value.Reset();
			}
			public override string ToString()
			{
				return string.Format("[{0} {1}] calls: {2} incl: {3} excl: {4}" ,Name ,ProfileId ,CallCount ,SelfInclChildTimeTicks ,SelfExclChildTimeTicks);
			}

			/// <summary>Combine the data in 'rhs' with this block</summary>
			public void Merge(Block rhs)
			{
				CallCount              += rhs.CallCount;
				SelfInclChildTimeTicks += rhs.SelfInclChildTimeTicks;
				SelfExclChildTimeTicks += rhs.SelfExclChildTimeTicks;
				foreach (var c in rhs.Callers)
				{
					Caller rhs_caller = c.Value;
					Caller caller = GetOrAdd(Callers, c.Key, () => rhs_caller);
					if (!ReferenceEquals(caller, rhs_caller)) caller.Merge(rhs_caller);
				}
			}
		};

		/// <summary>
		/// A profile instance - Create, using Profile.Get(), one of these
		/// for each object/module/function/etc to be profiled.</summary>
		public class Instance :Block
		{
			internal long     m_start;    // The rtc value on leaving the profile start method
			internal int      m_active;   // > 0 while this profile is running, == 0 when it is not
			internal Instance m_parent;   // Transient parent profile block

			internal Instance(string name, int id)
			:base(name, id)
			{}

			/// <summary>Reset the profile data, called after profile output has been generated</summary>
			[Conditional("PR_PROFILE")] internal new void Reset()
			{
				Debug.Assert(m_active == 0, "Resetting an active profile block");
				base.Reset();
				m_active = 0;
			}

			/// <summary>Call to indicate entry into this profile block</summary>
			[Conditional("PR_PROFILE")] public void Start()
			{
				// If this isn't the root profile and the frame hasn't started, ignore the start call
				// Or, if start calls are reentrant and we've already started, track the reentrancy count.
				if ((this != mgr.m_root && !mgr.FrameStarted) || ++m_active > 1) return;
				m_parent    = mgr.m_stack; // Record the profile block that we're currently within as the parent
				mgr.m_stack = this;        // Push this profile onto the stack
				m_start     = Stopwatch.GetTimestamp(); // Start the clock
			}

			/// <summary>Call to exit from this profile block and accumulate call count and processing time</summary>
			[Conditional("PR_PROFILE")] public void Stop()
			{
				// Reentrant call returning, don't stop until active == 0
				if (m_active == 0 || --m_active > 0)
					return;

				// Grab the timestamp now so we don't include too much overhead time in the results
				long now = Stopwatch.GetTimestamp();
				Debug.Assert(m_active == 0, "This profile has been stopped more times than it's been started");
				Debug.Assert(this == mgr.m_stack, "This profile hasn't been started or a child profile hasn't been stopped");

				// Accumulate times
				long time = now - m_start;
				SelfInclChildTimeTicks += time;
				SelfExclChildTimeTicks += time;
				CallCount++;

				Instance parent = m_parent;

				// Pop this profile off the stack
				mgr.m_stack = m_parent;
				m_parent = null;

				// If not the root...
				if (parent != null)
				{
					// Collect call tree info
					Caller caller = GetOrAdd(Callers, parent.ProfileId, ()=>new Caller());
					caller.ProfileId = parent.ProfileId; // Record the id of the guy we're being called from
					caller.TotalCallTime += time;        // The amount of time spent in this profile when called from 'caller'
					caller.CallCount++;                  // The number of times this profile has been called from 'caller'

					// Remove some of the time overhead from the parent statistics
					long stop_overhead = Stopwatch.GetTimestamp() - now;
					parent.SelfExclChildTimeTicks -= time + stop_overhead;
					parent.SelfInclChildTimeTicks -= stop_overhead;
				}
			}

			///// <summary>A sorry substitute for RAII</summary>
			//public ScopeType Scope() { return new ScopeType(this); }
			//public class ScopeType :IDisposable
			//{
			//	private readonly Instance m_inst;
			//	public ScopeType(Instance inst) { (m_inst = inst).Start(); }
			//	public void Dispose()           { m_inst.Stop();  }
			//}
		}

		/// <summary>Profile collection manager singleton</summary>
		private static Collecter mgr
		{
			[DebuggerStepThrough] get { return Collecter.s_instance ?? (Collecter.s_instance = new Collecter()); }
		}

		/// <summary>
		/// Singleton class used on the collection side. Acts as a container of profiles.
		/// Manages data related to collecting profile timing data.</summary>
		internal class Collecter :List<Instance>
		{
			internal static Collecter  s_instance = null; // Singleton instance
			internal readonly Instance m_root;            // The base instance for all profiles
			internal Instance          m_stack;           // The stack of nested profiles
			internal int               m_sample_count;    // The number of times a sample has been captured
			internal double            m_ticks_per_ms;    // The scaler to convert from ticks to milliseconds
			private  int               m_id;              // A unique id generator for profile instances
			internal Collecter()
			{
				m_id                  = 0;
				m_root                = new Instance("Root", m_id++);
				m_stack               = null;
				m_sample_count        = 0;
				m_ticks_per_ms        = Stopwatch.Frequency / 1000.0;
				Add(m_root);
			}

			/// <summary>Add or create a profile instance. Each unique name creates a new profile instance.</summary>
			internal Instance get(string name)
			{
				int idx = BinarySearch<Instance>(this, i => string.CompareOrdinal(i.Name, name));
				if (idx >= 0) return this[idx];
				var inst = new Instance(name, m_id++);
				Insert(~idx, inst);
				return inst;
			}

			/// <summary>True while a profiling frame is active</summary>
			internal bool FrameStarted
			{
				get { return m_root.m_active != 0; }
			}

			/// <summary>Called to mark the start of a profiling frame</summary>
			internal void FrameBeginImpl()
			{
				m_root.Start();
			}

			/// <summary>Called to mark the end of a profiling frame</summary>
			internal void FrameEndImpl()
			{
				m_root.Stop();
			}

			/// <summary>
			/// Read the collected profiling data and send it to 'output'.
			/// Resets the profile instances ready for a frame to begin again.
			/// Calling this method with 'null' has the effect of resetting the profiling data.</summary>
			internal void SampleImpl(BinaryWriter output, bool reset) // Note: symmetric with Results.Collect()
			{
				Debug.Assert(FrameStarted == false, "Do not sample profiling data midway through a frame");

				// No frames collected
				if (m_root.CallCount == 0)
					return;

				// If output is null, just reset the profile blocks
				if (output == null)
				{
					if (reset) { foreach (var p in mgr) p.Reset(); }
					++m_sample_count;
					return;
				}

				// Build the packet in a memory buffer first
				MemoryStream mem;
				using (var pkt = new BinaryWriter(mem = new MemoryStream())) // BinaryWriter will dispose the mem stream as well
				{
					pkt.Write(m_sample_count);
					pkt.Write(m_ticks_per_ms);
					pkt.Write(m_root.CallCount);
					pkt.Write(Count);
					foreach (var p in mgr)
					{
						// Output each profile instance and it's callers
						pkt.Write(p.Name);
						pkt.Write(p.ProfileId);
						pkt.Write(p.CallCount);
						pkt.Write(p.SelfInclChildTimeTicks);
						pkt.Write(p.SelfExclChildTimeTicks);
						pkt.Write(p.Callers.Count);
						foreach (var c in p.Callers)
						{
							pkt.Write(c.Value.ProfileId);
							pkt.Write(c.Value.CallCount);
							pkt.Write(c.Value.TotalCallTime);
						}
						if (reset) p.Reset();
					}
					++m_sample_count;

					// Determine the packet length
					var pkt_length = (uint)mem.Length;
					if (pkt_length > MaxPacketSize)
						throw new ApplicationException("Sampled profile data is too large");

					// Output the packet to 'output'
					output.Write(PacketStartTag);  // header tag
					output.Write(pkt_length);      // size of following packet
					mem.Position = 0;
					mem.CopyTo(output.BaseStream); // packet data
				}
			}
		}

		/// <summary>Data representing the average results over one frame</summary>
		public class ResultData :Block
		{
			private readonly double m_ticks_per_ms;
			private readonly double m_frames;

			/// <summary>The number of times Start()/Stop() has been called for this profile (i.e. call count)</summary>
			public int    TotalCallCount           { get { return CallCount; } }

			/// <summary>The total time spent within Start()/Stop() including calls to child profiles (total time)</summary>
			public double TotalSelfInclChildTimeMS { get { return SelfInclChildTimeTicks / m_ticks_per_ms; } }

			/// <summary>The total time spent within Start()/Stop() excluding calls to child profiles (self time)</summary>
			public double TotalSelfExclChildTimeMS { get { return SelfExclChildTimeTicks / m_ticks_per_ms; } }

			/// <summary>The number of times this profile block was called per frame</summary>
			public double AvrCallsPerFrame         { get { return CallCount / m_frames; } }

			/// <summary>The average time spent in this profile block per frame (in ms)</summary>
			public double AvrSelfInclChildTimeMS   { get { return SelfInclChildTimeTicks / (m_ticks_per_ms * m_frames); } }

			/// <summary>The average time spent in this profile block per frame (in ms) excluding child profile blocks</summary>
			public double AvrSelfExclChildTimeMS   { get { return SelfExclChildTimeTicks / (m_ticks_per_ms * m_frames); } }

			/// <summary>Profile blocks that where called within this profile block</summary>
			public ResultSample Child = new ResultSample();

			public ResultData() :base(null,0)
			{
				m_frames       = 1.0;
				m_ticks_per_ms = 1.0;
			}
			public ResultData(Block block, int frames, double ticks_per_ms) :base(block)
			{
				m_frames       = frames;
				m_ticks_per_ms = ticks_per_ms;
			}
			public override string ToString()
			{
				return string.Format("[{0} {1}] calls: {2} incl: {3} excl: {4}" ,Name ,ProfileId ,AvrCallsPerFrame ,AvrSelfInclChildTimeMS ,AvrSelfExclChildTimeMS);
			}

			/// <summary>Combine the statistics of 'rhs' with this ResultData</summary>
			public void Merge(ResultData rhs)
			{
				base.Merge(rhs);
				foreach (var c in rhs.Child)
					AddIfUnique(Child, c, (l,r) => l.ProfileId == r.ProfileId);
			}
		}

		/// <summary>A collection of ResultData</summary>
		public class ResultSample :List<ResultData>
		{
			/// <summary>Sort results by profile id</summary>
			public void SortProfileId()
			{
				Sort((lhs,rhs) => Compare(lhs.ProfileId, rhs.ProfileId));
			}

			/// <summary>Sort results by name</summary>
			public void SortByName()
			{
				Sort((lhs,rhs) => string.CompareOrdinal(lhs.Name, rhs.Name));
			}

			/// <summary>Sort results by calls per frame</summary>
			public void SortByCallCount()
			{
				Sort((lhs,rhs) => -Compare(lhs.CallCount, rhs.CallCount));
			}

			/// <summary>Sort results by inclusive time</summary>
			public void SortByInclTime()
			{
				Sort((lhs,rhs) => -Compare(lhs.SelfInclChildTimeTicks, rhs.SelfInclChildTimeTicks));
			}

			/// <summary>Sort results by exclusive time</summary>
			public void SortByExclTime()
			{
				Sort((lhs,rhs) => -Compare(lhs.SelfExclChildTimeTicks, rhs.SelfExclChildTimeTicks));
			}
		}

		/// <summary>A history of result data</summary>
		public class ResultHistory :List<ResultSample> {}

		/// <summary>A collector of profile results</summary>
		public class Results
		{
			/// <summary>A history of sampled profile data</summary>
			public readonly ResultHistory m_history = new ResultHistory();

			/// <summary>The call tree</summary>
			public ResultSample Data = new ResultSample();

			/// <summary>The sample number</summary>
			public int SampleCount;

			/// <summary>The scaler that converts ticks to milliseconds</summary>
			public double TicksPerMS;

			/// <summary>The number of frames in this sample</summary>
			public int Frames;

			/// <summary>The average length of a frame in milli seconds</summary>
			public double FrameTimeMS { get { return Data.Count != 0 ? Data[0].AvrSelfInclChildTimeMS : 0; } }

			/// <summary>The length of history to keep</summary>
			public int MaxHistoryLength { get; set; }

			public Results()
			{
				MaxHistoryLength = 10;
			}

			/// <summary>
			/// Read piped profile data from a stream.
			/// This method should be the symmetric opposite to Profile.Output()</summary>
			public void Collect(BinaryReader input) // Note: symmetric with Profile.Sample()
			{
				// Find the start of packet marker then read the packet into a buffer
				for (uint mark = input.ReadUInt32(); mark != PacketStartTag; mark = input.ReadUInt32()) {}
				uint pkt_length = input.ReadUInt32();
				if (pkt_length > MaxPacketSize)
				{
					input.BaseStream.Flush();
					return; // Ignore invalid packet data
				}

				// Read the packet into 'buf'
				var buf = new byte[pkt_length];
				for (int read = input.Read(buf, 0, buf.Length); read != buf.Length; read += input.Read(buf, read, buf.Length - read))
				{}

				// Read the sample from the packet data
				var sample = new ResultSample();
				using (var br = new BinaryReader(new MemoryStream(buf, false)))
				{
					SampleCount     = br.ReadInt32();
					TicksPerMS      = br.ReadDouble();
					Frames          = br.ReadInt32();
					int block_count = br.ReadInt32();
					for (int i = 0; i != block_count; ++i)
					{
						var block = new Block(br.ReadString(), br.ReadInt32())
							{
								CallCount = br.ReadInt32(),
								SelfInclChildTimeTicks = br.ReadInt64(),
								SelfExclChildTimeTicks = br.ReadInt64()
							};
						int caller_count = br.ReadInt32();
						for (int j = 0; j != caller_count; ++j)
						{
							var caller = new Caller
								{
									ProfileId     = br.ReadInt32(),
									CallCount     = br.ReadInt32(),
									TotalCallTime = br.ReadInt64(),
								};
							block.Callers.Add(caller.ProfileId, caller);
						}
						sample.Add(new ResultData(block, Frames, TicksPerMS));
					}
				}

				// Wire-up caller tree
				sample.SortProfileId();
				foreach (var d in sample)
				{
					foreach (var c in d.Callers)
					{
						Caller caller = c.Value;
						int idx = BinarySearch(sample, p => p.ProfileId - caller.ProfileId);
						if (idx >= 0) sample[idx].Child.Add(d);
					}
				}

				// Push the current sample to history
				m_history.Insert(0, Data);
				if (m_history.Count == MaxHistoryLength) m_history.RemoveAt(MaxHistoryLength);

				// Update to the new sample
				Data = sample;
			}

			/// <summary>Merge another results instance with this one</summary>
			public void Merge(Results result)
			{
				SampleCount = Math.Max(SampleCount, result.SampleCount);
				Frames     += result.Frames;
				Data.AddRange(result.Data);
				Data.SortProfileId();
				for (int i = 0; i != Data.Count - 1; ++i)
				{
					if (Data[i].ProfileId != Data[i+1].ProfileId) continue;
					Data[i].Merge(Data[i+1]);
					Data.RemoveAt(i+1);
					--i;
				}
			}

			/// <summary>Output a basic summary of the collected profile data as a string</summary>
			public string Summary
			{
				get
				{
					const string row_format = " {0,-50} | {1,10} | {2,20}  | {3,20} |";
					var title_line = string.Format(row_format,"name" ,"count" ,"incl (ms)" ,"excl (ms)");
					var horiz_line = new string('=',title_line.Length);

					var sb = new StringBuilder();
					sb.AppendLine("Profile Results:");
					sb.AppendLine(string.Format(" Frame time: {0} ms   Frame rate: {1} Hz", FrameTimeMS, (1000.0 / FrameTimeMS).ToString("0.00")));
					sb.AppendLine(horiz_line);
					sb.AppendLine(title_line);
					sb.AppendLine(horiz_line);

					// Output each profile
					foreach (var pres in Data)
						sb.AppendLine(string.Format(row_format, pres.Name, pres.AvrCallsPerFrame, pres.AvrSelfInclChildTimeMS, pres.AvrSelfExclChildTimeMS));

					sb.AppendLine(horiz_line);
					return sb.ToString();
				}
			}
		}

		#region Helper methods

		private static int Compare(int lhs, int rhs)       { return (lhs < rhs) ? -1 : (lhs > rhs) ? 1 : 0; }
		private static int Compare(double lhs, double rhs) { return (lhs < rhs) ? -1 : (lhs > rhs) ? 1 : 0; }

		/// <summary>Return the value for 'key', if it doesn't exist, insert and return the result of calling 'def'</summary>
		private static V GetOrAdd<K,V>(Dictionary<K,V> dic, K key, Func<V> def)
		{
			V value;
			if (dic.TryGetValue(key, out value)) return value;
			dic.Add(key, value = def()); // 'def' is only evaluated if 'key' is not found
			return value;
		}

		/// <summary>Binary search using for an element using only a predicate function.
		/// Returns the index of the element if found or the 2s-complement of the first
		/// element larger than the one searched for.
		/// 'cmp' should return -1 if T is less than the target, +1 if greater, or 0 if equal</summary>
		private static int BinarySearch<T>(List<T> list, Func<T,int> cmp)
		{
			if (list.Count == 0) return ~0;
			for (int b = 0, e = list.Count;;)
			{
				int m = b + ((e - b) >> 1); // prevent overflow
				int c = cmp(list[m]);       // <0 means list[m] is less than the target element
				if (c == 0) { return m; }
				if (c <  0) { if (m == b){return ~e;} b = m; continue; }
				if (c >  0) { if (m == b){return ~b;} e = m; continue; }
			}
		}

		/// <summary>
		/// Add 'item' to the list if it's not already there.
		/// Uses 'are_equal(list[i],item)' to test for uniqueness.
		/// Returns true if 'item' was added, false if it was a duplicate</summary>
		private static void AddIfUnique<T>(List<T> list, T item, Func<T,T,bool> are_equal)
		{
			foreach (var i in list) if (are_equal(i,item)) return;
			list.Add(item);
		}

		#endregion
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;
	using util;

	[TestFixture] internal static partial class UnitTests
	{
		internal static class TestProfile
		{
			public class Test
			{
				public void Func1()
				{
					using (Profile.Scope("Func1"))
					{
						Func2();
						Func3(10);
					}
				}
				public void Func2()
				{
					using (Profile.Scope("Func2"))
					{
						for (int i = 0; i != 5; ++i)
							Func3(i);
					}
				}
				public void Func3(int i)
				{
					using (Profile.Scope("Func3"))
					{
						if (i == 4)
							return;
					}
				}
			}

			[Test] public static void TestScopes()
			{
				// Collect profile data
				for (var f = 0; f != 10; ++f)
				{
					using (Profile.FrameScope())
					{
						var test = new Test();
						test.Func1();
					}
				}

				Debug.Write(Profile.Summary);
			}
		}
	}
}
#endif