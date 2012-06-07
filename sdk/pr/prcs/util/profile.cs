#define PR_PROFILE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using NUnit.Framework;
using pr.extn;
using pr.maths;
using pr.stream;
using CallerMap = System.Collections.Generic.Dictionary<int, pr.util.Profile.Caller>;

namespace pr.util
{
	/// <summary>Profiling objects and methods</summary>
	public static class Profile
	{
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
			
			public override string ToString() { return string.Format("[{0}] count: {1} time: {2}" ,ProfileId ,CallCount ,TotalCallTime); }
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
			public void Reset()
			{
				CallCount              = 0;
				SelfInclChildTimeTicks = 0;
				SelfExclChildTimeTicks = 0;
				Callers.Clear();
			}
			public override string ToString()
			{
				return string.Format("[{0} {1}] calls: {2} incl: {3} excl: {4}" ,Name ,ProfileId ,CallCount ,SelfInclChildTimeTicks ,SelfExclChildTimeTicks);
			}
		};
		
		/// <summary>
		/// A profile instance - Create, using Profile.Create(), one of these
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
					Caller caller = Callers.GetOrAdd(parent.ProfileId, ()=>new Caller());
					caller.ProfileId = parent.ProfileId; // Record the id of the guy we're being called from
					caller.TotalCallTime += time;        // The amount of time spent in this profile when called from 'caller'
					caller.CallCount++;                  // The number of times this profile has been called from 'caller'
					
					// Remove some of the time overhead from the parent statistics
					long stop_overhead = Stopwatch.GetTimestamp() - now;
					parent.SelfExclChildTimeTicks -= time + stop_overhead;
					parent.SelfInclChildTimeTicks -= stop_overhead;
				}
			}
		};
		
		/// <summary>Profile collection manager singleton</summary>
		private static Collecter mgr
		{
			[DebuggerStepThrough] get { return Collecter.s_instance ?? (Collecter.s_instance = new Collecter()); }
		}
		
		// ReSharper disable MemberHidesStaticFromOuterClass
		/// <summary>
		/// Singleton class used on the collection side. Acts as a container of profiles.
		/// Manages data related to collecting profile timing data.</summary>
		internal class Collecter :List<Instance>
		{
			internal static Collecter  s_instance = null; // Singleton instance
			internal readonly Instance m_root;            // The base instance for all profiles
			internal Instance          m_stack;           // The stack of nested profiles
			internal int               m_sample_count;    // The number of times a sample has been captured
			internal long              m_frame_start;     // Used to calculate m_frame_time
			internal double            m_ticks_per_ms;    // The scaler to convert from ticks to milliseconds
			private  int               m_id;              // A unique id generator for profile instances
			internal Collecter()
			{
				m_id           = 0;
				m_root         = new Instance("Root", m_id++);
				m_stack        = null;
				m_sample_count = 0;
				m_frame_start  = 0;
				m_ticks_per_ms = Stopwatch.Frequency / 1000.0;
				Add(m_root);
			}
			internal Instance get(string name)
			{
				Instance inst = Find(i => i.Name == name);
				if (inst == null) Add(inst = new Instance(name, m_id++));
				return inst;
			}
			internal bool FrameStarted
			{
				get { return m_root.m_active != 0; }
			}
			internal void FrameBegin()
			{
				m_root.Start();
			}
			internal void FrameEnd()
			{
				m_root.Stop();
			}
			internal void Sample(Stream output)
			{
				// No frames collected
				if (m_root.CallCount == 0)
					return;
				
				// Output the sample data - Note: symmetric with Results.Collect()
				var bw = new BinaryWriter(output);
				bw.Write(m_sample_count);
				bw.Write(m_ticks_per_ms);
				bw.Write(m_root.CallCount);
				bw.Write(Count);
				foreach (var p in mgr)
				{
					// Output each profile instance and it's callers
					bw.Write(p.Name);
					bw.Write(p.ProfileId);
					bw.Write(p.CallCount);
					bw.Write(p.SelfInclChildTimeTicks);
					bw.Write(p.SelfExclChildTimeTicks);
					bw.Write(p.Callers.Count);
					foreach (var c in p.Callers)
					{
						bw.Write(c.Value.ProfileId);
						bw.Write(c.Value.CallCount);
						bw.Write(c.Value.TotalCallTime);
					}
					p.Reset();
				}
				
				++m_sample_count;
			}
		}
		// ReSharper restore MemberHidesStaticFromOuterClass
		
		/// <summary>Factory method for creating profile instances</summary>
		public static Instance Create(string name)
		{
			#if PR_PROFILE
			return mgr.get(name);
			#else
			return null;
			#endif
		}
		
		/// <summary>
		/// Starts the time frame over which profiling is taking place.
		/// A profiling frame typically represents a session of multiple calls to the Start()/Stop()
		/// methods on multiple profiling blocks. Note: it is valid to have only one frame for a
		/// profiling session</summary>
		[Conditional("PR_PROFILE")] public static void FrameBegin()
		{
			mgr.FrameBegin();
		}
		
		/// <summary>Ends the time frame over which profiling is taking place</summary>
		[Conditional("PR_PROFILE")] public static void FrameEnd()
		{
			mgr.FrameEnd();
		}
		
		/// <summary>
		/// Sample the current collected profile data and send it out a stream.
		/// A sample represents one or more profiling frames and allows averaged statistics such as
		/// calls per frame, time per frame, etc. The user is free to choose where FrameBegin()/FrameEnd() calls
		/// are made, but a sample should only be called after FrameEnd() and before the next FrameBegin().
		/// 'ouput' is a stream so that raw profiling data can be piped to an external process/machine
		/// if necessary. Returns the provided stream.</summary>
		[Conditional("PR_PROFILE")] public static void Sample(Stream output)
		{
			mgr.Sample(output);
		}
		
		// Above here are types for profile data collection
		// ***************************************************************************
		// Below here are types for profile data processing and display
		
		/// <summary>Data representing the average results over one frame</summary>
		public class ResultData :Block
		{
			/// <summary>The number of times this profile block was called per frame</summary>
			public double CallsPerFrame;
			
			/// <summary>The average time spent in this profile block per frame (in ms)</summary>
			public double SelfInclChildTimeMS;
			
			/// <summary>The average time spent in this profile block per frame (in ms) excluding child profile blocks</summary>
			public double SelfExclChildTimeMS;
			
			/// <summary>Profile blocks that where called within this profile block</summary>
			public List<ResultData> Child = new List<ResultData>();
			
			public ResultData() :base(null,0) {}
			public ResultData(Block block, int frames, double ticks_per_ms) :base(block)
			{
				CallsPerFrame       = (double)CallCount / frames;
				SelfInclChildTimeMS = SelfInclChildTimeTicks / (ticks_per_ms * frames);
				SelfExclChildTimeMS = SelfExclChildTimeTicks / (ticks_per_ms * frames);
			}
			public override string ToString()
			{
				return string.Format("[{0} {1}] calls: {2} incl: {3} excl: {4}" ,Name ,ProfileId ,CallsPerFrame ,SelfInclChildTimeMS ,SelfExclChildTimeMS);
			}
		}
		
		/// <summary>A collecter of profile results</summary>
		public class Results
		{
			private readonly List<ResultData> m_data = new List<ResultData>();
			
			/// <summary>The sample number</summary>
			public int SampleCount;
			
			/// <summary>The scaler that converts ticks to milliseconds</summary>
			public double TicksPerMS;
			
			/// <summary>The number of frames in this sample</summary>
			public int Frames;
			
			/// <summary>The average length of a frame in milli seconds</summary>
			public double FrameTimeMS { get { return m_data[0].SelfInclChildTimeMS; } }
			
			/// <summary>
			/// Read piped profile data from a stream.
			/// This method should be the symmetric opposite to Profile.Output()</summary>
			[Conditional("PR_PROFILE")] public void Collect(Stream input)
			{
				var br = new BinaryReader(input);
				
				SampleCount = br.ReadInt32();
				TicksPerMS  = br.ReadDouble();
				Frames      = br.ReadInt32();
				
				// Read sample data
				int block_count = br.ReadInt32();
				for (int i = 0; i != block_count; ++i)
				{
					Block block = new Block(br.ReadString(), br.ReadInt32())
						{
							CallCount = br.ReadInt32(),
							SelfInclChildTimeTicks = br.ReadInt64(),
							SelfExclChildTimeTicks = br.ReadInt64()
						};
					int caller_count = br.ReadInt32();
					for (int j = 0; j != caller_count; ++j)
					{
						Caller caller = new Caller
							{
								ProfileId     = br.ReadInt32(),
								CallCount     = br.ReadInt32(),
								TotalCallTime = br.ReadInt64(),
							};
						block.Callers.Add(caller.ProfileId, caller);
					}
					m_data.Add(new ResultData(block, Frames, TicksPerMS));
				}
				
				// Wire-up caller tree
				m_data.Sort((lhs,rhs) => Maths.Compare(lhs.ProfileId, rhs.ProfileId));
				foreach (var d in m_data)
				{
					foreach (var c in d.Callers)
					{
						Caller caller = c.Value;
						int idx = m_data.BinarySearch(p => p.ProfileId - caller.ProfileId);
						if (idx >= 0) m_data[idx].Child.Add(d);
					}
				}
			}
			
			/// <summary>Sort results by name</summary>
			[Conditional("PR_PROFILE")] public void SortByName()
			{
				Sort((lhs,rhs) => string.Compare(lhs.Name, rhs.Name));
			}
			
			/// <summary>Sort results by calls per frame</summary>
			[Conditional("PR_PROFILE")] public void SortByCallCount()
			{
				Sort((lhs,rhs) => -Maths.Compare(lhs.CallsPerFrame, rhs.CallsPerFrame));
			}
			
			/// <summary>Sort results by inclusive time</summary>
			[Conditional("PR_PROFILE")] public void SortByInclTime()
			{
				Sort((lhs,rhs) => -Maths.Compare(lhs.SelfInclChildTimeMS, rhs.SelfInclChildTimeMS));
			}
			
			/// <summary>Sort results by exclusive time</summary>
			[Conditional("PR_PROFILE")] public void SortByExclTime()
			{
				Sort((lhs,rhs) => -Maths.Compare(lhs.SelfExclChildTimeMS, rhs.SelfExclChildTimeMS));
			}
			
			/// <summary>Sort the results by a custom predicate</summary>
			[Conditional("PR_PROFILE")] public void Sort(Comparison<ResultData> pred)
			{
				m_data.Sort(pred);
			}
			
			/// <summary>Output a basic summary of the collected profile data as a string</summary>
			[Conditional("PR_PROFILE")] public void Summary(StringBuilder sb)
			{
				sb.AppendFormat(
					"Profile Results:\n"+
					" Frame time: {1} ms   Frame rate: {0} Hz\n"+
					"=====================================================================\n"+
					" {2,-16} | {3,10} | {4,20}  | {5,20} |\n"+
					"=====================================================================\n"
					,(1000.0 / FrameTimeMS).ToString("0.00")
					,FrameTimeMS
					,"name"
					,"count"
					,"incl (ms)"
					,"excl (ms)"
				);
				
				// Output each profile
				foreach (var pres in m_data)
				{
					sb.AppendFormat(
						" {0,-16} | {1,10} | {2,20} | {3,20} |\n"
						,pres.Name
						,pres.CallsPerFrame
						,pres.SelfInclChildTimeMS
						,pres.SelfExclChildTimeMS
					);
				}
				
				sb.Append("=====================================================================\n");
			}
		}
	}
	
	/// <summary>String transform unit tests</summary>
	[TestFixture] internal static partial class UnitTests
	{
		public class Test
		{
			private readonly Profile.Instance prof1 = Profile.Create("Func1");
			private readonly Profile.Instance prof2 = Profile.Create("Func2");
			private readonly Profile.Instance prof3 = Profile.Create("Func3");
			
			public void Func1()
			{
				prof1.Start();
				Thread.Sleep(10);
				Func2();
				Func3();
				prof1.Stop();
			}
			public void Func2()
			{
				prof2.Start();
				Thread.Sleep(20);
				for (int i = 0; i != 5; ++i)
					Func3();
				prof2.Stop();
			}
			public void Func3()
			{
				prof3.Start();
				Thread.Sleep(1);
				prof3.Stop();
			}
		}
		
		[Test] public static void TestProfile()
		{
			// Collect profile data
			for (int f = 0; f != 10; ++f)
			{
				Profile.FrameBegin();
				var test = new Test();
				test.Func1();
				Profile.FrameEnd();
			}
			
			// A stream for linking collected profile data to the profile output
			var s = new LinkStream();
			Profile.Sample(s.OStream);
			
			// Receive and display profile data
			Profile.Results results = new Profile.Results();
			results.Collect(s.IStream);
			results.SortByInclTime();
			
			StringBuilder sb = new StringBuilder();
			results.Summary(sb);
			Debug.Write(sb.ToString());
		}
	}
}

