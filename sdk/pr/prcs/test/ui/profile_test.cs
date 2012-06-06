using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using pr.util;

namespace test.test.ui
{
	public partial class ProfileTest :Form
	{
		/// <summary>String transform unit tests</summary>
		public class Test
		{
			private readonly Profile.Instance prof1 = new Profile.Instance("Func1");
			private readonly Profile.Instance prof2 = new Profile.Instance("Func2");
			private readonly Profile.Instance prof3 = new Profile.Instance("Func3");
			
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
			
			// A stream for linking collected profile data to the profile output
			var ms = new MemoryStream();
			
			// Collect profile data
			for (int f = 0; f != 10; ++f)
			{
				Profile.FrameBegin();
				var test = new Test();
				test.Func1();
				Profile.FrameEnd();
			}
			Profile.Output(ms);
			
			// Receive and display profile data
			var result = Profile.Collect(ms);
			result.SortByInclTime();
			m_edit.Text = result.Summary;
		}
	}
}
