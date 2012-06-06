using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using NUnit.Framework;
using pr.maths;
using CallerMap = System.Collections.Generic.Dictionary<pr.util.Profile.Instance, pr.util.Profile.Caller>;

namespace pr.util
{
	/// <summary>Profiling objects and methods</summary>
	public static class Profile
	{
		/// <summary>
		/// Profile data block - the lowest level unit of profile data.
		/// Collects:
		///  * number of times called,
		///  * time spent within the block,
		///  * time spent within the block excluding time spent in child blocks</summary>
		public class Block
		{
			/// <summary>The name of the profile block</summary>
			public string m_name;
			
			/// <summary>A unique id for this profile</summary>
			public int  m_id;
			
			/// <summary>The number of times Start()/Stop() has been called for this profile (i.e. call count)</summary>
			public int  m_calls;
			
			/// <summary>The total time spent within Start()/Stop() including calls to child profiles (total time)</summary>
			public long m_incl;
			
			/// <summary>The total time spent within Start()/Stop() excluding calls to child profiles (self time)</summary>
			public long m_excl;

			internal Block(string name, int id) { m_name = name; m_id = id; }
			public override string ToString() { return string.Format("[{0}] calls: {1} incl: {2} excl: {3}" ,m_id ,m_calls ,m_incl ,m_excl); }
		};
		
		/// <summary>Caller data - tracks the tree structure of calls to nested profile blocks</summary>
		public class Caller
		{
			/// <summary>The ID of the profile making the call</summary>
			public int  m_id;
		
			/// <summary>The number of times the owning profile has been called from the profile with id 'm_id'</summary>
			public int  m_count;
		
			/// <summary>The amount of time spent in the owning profile while being called from the profile with id 'm_id'</summary>
			public long m_time;
			
			public override string ToString() { return string.Format("[{0}] count: {1} time: {2}" ,m_id ,m_count ,m_time); }
		};
		
		/// <summary>A profile instance - create one of these for each object/function/entity to be profiled</summary>
		public class Instance :Block, IDisposable
		{
			public CallerMap  m_caller;   // A map of parents of this profile, if parent == 0, then its a global scope profile
			public long       m_start;    // The rtc value on leaving the profile start method
			public long       m_time;     // The time within one Start()/Stop() cycle
			public int        m_active;   // > 0 while this profile is running, == 0 when it is not
			public bool       m_disabled; // True if this profile is disabled
			public Instance   m_parent;   //
			
			internal Instance(string name, int id)
			:base(name, id)
			{
				m_caller = new CallerMap();
				Profile.RegisterProfile(this);
			}
			~Instance()
			{
				Dispose();
			}
			public Instance(string name)
			:this(name, mgr.m_id++)
			{}

			public void Dispose()
			{
				Profile.UnregisterProfile(this);
			}

			/// <summary>Reset the profile data, called after profile output has been generated</summary>
			public void Reset()
			{
				Debug.Assert(m_active == 0, "Resetting an active profile block");
				m_active = 0;
				m_calls  = 0;
				m_incl   = 0;
				m_excl   = 0;
				m_caller.Clear();
			}
		};
		
		/// <summary>Data describing a profiling sample.</summary>
		public class Sample
		{
			/// <summary>The sample count. Represents the number of times Profile.Sample() has been called</summary>
			public int m_sample_count;
			
			/// <summary>The number of frames in this sample. (i.e. the number of times FrameBegin() has been called)</summary>
			public int m_frames;
			
			/// <summary>The total accumulated time for the sample. (i.e. the sum of frame times)</summary>
			public long m_sample_time;
			
			/// <summary>The scaler for converting time values to millisecond times</summary>
			public double m_to_ms;
		}
		
		/// <summary>
		/// Singleton class used on the collection side.
		/// Manages data related to collecting profile timing data.</summary>
		private class Collecter
		{
			public static Collecter        s_instance = null; // Singleton instance
			public int                     m_id;              // A unique id generator for profile instances
			public readonly List<Instance> m_profiles;        // References to the profile data objects
			public readonly Instance       m_root;            // The base instance for all profiles
			public readonly Sample         m_sample;          // Data to be sent with the next sample
			public Instance                m_stack;           // The stack of nested profiles
			public bool                    m_frame_started;   // True when we're between PR_PROFILE_FRAME_BEGIN and PR_PROFILE_FRAME_END
			public long                    m_frame_start;     // Used to calculate m_frame_time
			public Collecter()
			{
				m_id            = 1; // Note: id zero is reserved for the root instance
				m_profiles      = new List<Instance>();
				m_root          = new Instance("Root", 0);
				m_sample        = new Sample{m_to_ms = Stopwatch.Frequency / 1000.0};
				m_stack         = m_root;
				m_frame_started = false;
				m_frame_start   = 0;
			}
		}
		
		/// <summary>Profile collection manager singleton</summary>
		private static Collecter mgr
		{
			get { return Collecter.s_instance ?? (Collecter.s_instance = new Collecter()); }
		}
		
		/// <summary>Profile instances register and unregister themselves on Construction/Dispose</summary>
		private static void RegisterProfile  (Instance profile) { mgr.m_profiles.Add(profile); }
		private static void UnregisterProfile(Instance profile) { mgr.m_profiles.Remove(profile); }
		
		/// <summary>
		/// Starts the time frame over which profiling is taking place.
		/// A profiling frame typically represents a session of multiple calls to the Start()/Stop()
		/// methods on multiple profiling blocks. Note: it is valid to have only one frame for a
		/// profiling session</summary>
		public static void FrameBegin()
		{
			mgr.m_frame_started = true;
			mgr.m_frame_start   = Stopwatch.GetTimestamp();
		}
		
		/// <summary>Ends the time frame over which profiling is taking place</summary>
		public static void FrameEnd()
		{
			mgr.m_sample.m_sample_time += Stopwatch.GetTimestamp() - mgr.m_frame_start;
			mgr.m_frame_started = false;
			++mgr.m_sample.m_frames;
			++mgr.m_sample.m_sample_count;
		}
		
		/// <summary>Reset the sample data</summary>
		public static void SampleReset()
		{
			mgr.m_stack = mgr.m_root;
			mgr.m_sample.m_sample_count++;
			mgr.m_sample.m_frames = 0;
			mgr.m_sample.m_sample_time = 0;
		}
		
		/// <summary>Call to indicate entry into this profile block</summary>
		public static void Start(this Instance profile)
		{
			if (!mgr.m_frame_started || ++profile.m_active > 1) return;
			profile.m_parent = mgr.m_stack;
			mgr.m_stack = profile;
			profile.m_start = Stopwatch.GetTimestamp();
		}
		
		/// <summary>Call to exit from this profile block and accumulate call count and processing time</summary>
		public static void Stop(this Instance profile)
		{
			// Nested call returning, don't stop until active == 0
			if (profile.m_active == 0 || --profile.m_active > 0)
				return;
			
			// Grab the timestamp first so we don't include too much overhead time in the results
			long now = Stopwatch.GetTimestamp();
			Debug.Assert(profile.m_active == 0, "This profile has been stopped more times than it's been started"); 
			Debug.Assert(profile == mgr.m_stack, "This profile hasn't been started or a child profile hasn't been stopped");
			
			// Accumulate times
			profile.m_time  = now - profile.m_start;
			profile.m_incl += profile.m_time;
			profile.m_excl += profile.m_time;
			profile.m_calls++;
			
			// Collect call tree info
			Caller caller = profile.m_caller.GetOrAdd(profile.m_parent, new Caller());
			caller.m_id   = profile.m_parent.m_id;
			caller.m_time += profile.m_time; // The amount of time spent in this profile when called from 'caller'
			caller.m_count++; // the number of times this profile has been called from this parent
			
			// Pop this profile off the stack
			mgr.m_stack = profile.m_parent;
			profile.m_parent = null;
			
			// Remove some of the time overhead from the frame statistics
			long stop_overhead = Stopwatch.GetTimestamp() - now;
			mgr.m_stack.m_excl -= profile.m_time + stop_overhead;
			mgr.m_sample.m_sample_time -= stop_overhead;
		}
		
		/// <summary>
		/// Sample the current collected profile data and send it out a stream.
		/// A sample represents one or more profiling frames and allows averaged statistics such as
		/// calls per frame, time per frame, etc. The user is free to choose where FrameBegin()/FrameEnd() calls
		/// are made, but a sample should only be called after FrameEnd() and before the next FrameBegin().
		/// 'ouput' is a stream so that raw profiling data can be piped to an external process/machine
		/// if necessary. Returns the provided stream.</summary>
		public static Stream Output(Stream output)
		{
			// No frames collected
			if (mgr.m_sample.m_frames == 0)
				return output;
			
			var bw = new BinaryWriter(output);
			
			// Output the sample data
			bw.Write(mgr.m_sample);
			bw.Write(mgr.m_profiles.Count);
			foreach (var p in mgr.m_profiles)
			{
				// Output each profile instance and it's callers
				bw.Write(p.m_name);
				bw.Write(p);
				bw.Write(p.m_caller.Count);
				foreach (var c in p.m_caller)
					bw.Write(c.Value);
				
				p.Reset();
			}
			SampleReset();
			return output;
		}
		
		// Above here are types for profile data collection
		// ***************************************************************************
		// Below here are types for profile data processing and display
		
		/// <summary>Helper extension methods</summary>
		private static void Write(this BinaryWriter bw, Block block)
		{
			bw.Write(block.m_name);
			bw.Write(block.m_id);
			bw.Write(block.m_calls);
			bw.Write(block.m_incl);
			bw.Write(block.m_excl);
		}
		private static Block ReadBlock(this BinaryReader br)
		{
			return new Block(br.ReadString(), br.ReadInt32())
			{
				m_calls = br.ReadInt32(),
				m_incl  = br.ReadInt64(),
				m_excl  = br.ReadInt64()
			};
		}
		private static void Write(this BinaryWriter bw, Caller caller)
		{
			bw.Write(caller.m_id);
			bw.Write(caller.m_count);
			bw.Write(caller.m_time);
		}
		private static Caller ReadCaller(this BinaryReader br)
		{
			return new Caller
			{
				m_id    = br.ReadInt32(),
				m_count = br.ReadInt32(),
				m_time  = br.ReadInt64(),
			};
		}
		private static void Write(this BinaryWriter bw, Sample sample)
		{
			bw.Write(sample.m_sample_count);
			bw.Write(sample.m_frames);
			bw.Write(sample.m_sample_time);
			bw.Write(sample.m_to_ms);
		}
		private static Sample ReadSample(this BinaryReader br)
		{
			return new Sample
			{
				m_sample_count = br.ReadInt32(),
				m_frames       = br.ReadInt32(),
				m_sample_time  = br.ReadInt64(),
				m_to_ms        = br.ReadDouble()
			};
		}
		
		/// <summary>
		/// Read piped profile data from a stream.
		/// This method should be the symmetric opposite to Profile.Output()</summary>
		public static Results Collect(Stream istream)
		{
			var br = new BinaryReader(istream);
			
			Sample sample = br.ReadSample();
			
			Results results = new Results(sample);
			
			// Read sample data
			int pcount = br.ReadInt32();
			for (int i = 0; i != pcount; ++i)
			{
				ResultData presult = new ResultData(br.ReadBlock(), sample);
				int ccount = br.ReadInt32();
				for (int j = 0; j != ccount; ++j)
					presult.Callers.Add(br.ReadCaller());
			}
			return results;
		}
		
		/// <summary>Data representing a collection of results over a frame</summary>
		public class ResultData
		{
			/// <summary>The name of the associated profile</summary>
			public string Name;
			
			/// <summary>The number of times this profile block was called per frame</summary>
			public double CallsPerFrame;
			
			/// <summary>The average time spent in this profile block per frame (in ms)</summary>
			public double SelfInclChildTimeMS;
			
			/// <summary>The average time spent in this profile block per frame (in ms) excluding child profile blocks</summary>
			public double SelfExclChildTimeMS;
			
			/// <summary>Profile blocks that contain calls to this called this</summary>
			public List<Caller> Callers = new List<Caller>();
			
			public ResultData() {}
			public ResultData(Block block, Sample sample)
			{
				CallsPerFrame       = block.m_calls * sample.m_to_ms / sample.m_frames;
				SelfInclChildTimeMS = block.m_incl  * sample.m_to_ms / sample.m_frames;
				SelfExclChildTimeMS = block.m_excl  * sample.m_to_ms / sample.m_frames;
			}
		}
		
		/// <summary>A collecter of profile results</summary>
		public class Results
		{
			private readonly List<ResultData> m_data = new List<ResultData>();
			
			/// <summary>The average length of a frame in milli seconds</summary>
			public double AvrFrameTimeMS;
			
			public Results(Sample sample)
			{
				AvrFrameTimeMS = (double)sample.m_sample_time / sample.m_frames;
			}
			
			/// <summary>Sort results by name</summary>
			public void SortByName()
			{
				Sort((lhs,rhs) => string.Compare(lhs.Name, rhs.Name));
			}
			
			/// <summary>Sort results by calls per frame</summary>
			public void SortByCallCount()
			{
				Sort((lhs,rhs) => Maths.Compare(lhs.CallsPerFrame, rhs.CallsPerFrame));
			}

			/// <summary>Sort results by inclusive time</summary>
			public void SortByInclTime()
			{
				Sort((lhs,rhs) => Maths.Compare(lhs.SelfInclChildTimeMS, rhs.SelfInclChildTimeMS));
			}

			/// <summary>Sort results by exclusive time</summary>
			public void SortByExclTime()
			{
				Sort((lhs,rhs) => Maths.Compare(lhs.SelfExclChildTimeMS, rhs.SelfExclChildTimeMS));
			}

			/// <summary>Sort the results by a custom predicate</summary>
			public void Sort(Comparison<ResultData> pred)
			{
				m_data.Sort(pred);
			}

			/// <summary>Output a basic summary of the collected profile data as a string</summary>
			public string Summary
			{
				get
				{
					StringBuilder sb = new StringBuilder();
					sb.AppendFormat(
						"Profile Results:\n"+
						" Frame rate: {0} Hz   Frame time: {1} ms\n"+
						"=====================================================================\n"+
						" name             | count        | incl (ms)  | excl(ms)  |\n"+
						"=====================================================================\n"
						,(1000.0 / AvrFrameTimeMS).ToString("0.00")
						,AvrFrameTimeMS
						);
					
					// Output each profile
					ResultData unaccounted = new ResultData();
					foreach (var pres in m_data)
					{
						unaccounted.SelfInclChildTimeMS += pres.SelfInclChildTimeMS;
						unaccounted.SelfExclChildTimeMS += pres.SelfExclChildTimeMS;
						sb.AppendFormat(
							" {0} | {1} | {2} | {3} |\n"
							,pres.Name
							,pres.CallsPerFrame
							,pres.SelfInclChildTimeMS
							,pres.SelfExclChildTimeMS
							);
					}
					unaccounted.SelfInclChildTimeMS = AvrFrameTimeMS - unaccounted.SelfInclChildTimeMS;
					unaccounted.SelfExclChildTimeMS = AvrFrameTimeMS - unaccounted.SelfExclChildTimeMS;
					sb.Append("=====================================================================\n");
					sb.AppendFormat(
						" {0} | {1} | {2} | {3} |\n"
						,unaccounted.Name
						,unaccounted.CallsPerFrame
						,unaccounted.SelfInclChildTimeMS
						,unaccounted.SelfExclChildTimeMS
						);
					sb.Append("=====================================================================\n");
					return sb.ToString();
				}
			}
		}
	}


	///// <summary>String transform unit tests</summary>
	//[TestFixture] internal static partial class UnitTests
	//{
	//    public class Test
	//    {
	//        private readonly Profile.Instance prof1 = new Profile.Instance("Func1");
	//        private readonly Profile.Instance prof2 = new Profile.Instance("Func2");
	//        private readonly Profile.Instance prof3 = new Profile.Instance("Func3");
			
	//        public void Func1()
	//        {
	//            prof1.Start();
	//            Thread.Sleep(10);
	//            Func2();
	//            Func3();
	//            prof1.Stop();
	//        }
	//        public void Func2()
	//        {
	//            prof2.Start();
	//            Thread.Sleep(20);
	//            for (int i = 0; i != 5; ++i)
	//                Func3();
	//            prof2.Stop();
	//        }
	//        public void Func3()
	//        {
	//            prof3.Start();
	//            Thread.Sleep(1);
	//            prof3.Stop();
	//        }
	//    }

	//    [Test] public static void TestProfile()
	//    {
	//        // A stream for linking collected profile data to the profile output
	//        var ms = new MemoryStream();
			
	//        // Collect profile data
	//        for (int f = 0; f != 10; ++f)
	//        {
	//            Profile.FrameBegin();
	//            var test = new Test();
	//            test.Func1();
	//            Profile.FrameEnd();
	//        }
	//        Profile.Output(ms);
			
	//        // Receive and display profile data
	//        var result = Profile.Collect(ms);
	//        result.SortByInclTime();
	//        Debug.Write(result.Summary);
	//    }
	//}
}

