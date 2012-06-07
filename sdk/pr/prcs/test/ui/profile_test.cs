#define PR_PROFILE
using System.Text;
using System.Threading;
using System.Windows.Forms;
using pr.stream;
using pr.util;

namespace test.test.ui
{
	public partial class ProfileTest :Form
	{
		/// <summary>String transform unit tests</summary>
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
		
		public ProfileTest()
		{
			InitializeComponent();
			
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
			m_edit.Text = sb.ToString().Replace("\n","\r\n");
		}
	}
}
