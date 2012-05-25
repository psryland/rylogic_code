using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace TestCS
{
	public class Program
	{
        public static void Main()
		{
			SerialPort port = new SerialPort("COM1", 9600);
			port.NewLine = "\r\n";
			port.ReadTimeout = 1000;
			port.WriteTimeout = 1000;
			port.Open();
			for (;;)
			{
				string msg = Console.ReadLine();
				if (msg.Length > 0) port.Write(msg + "\r\n");

				Thread.Sleep(100);
				if (port.BytesToRead > 0)
				{
					byte[] buf = new byte[port.BytesToRead];
					port.Read(buf, 0, port.BytesToRead);
					string rpy = new ASCIIEncoding().GetString(buf);
					Console.WriteLine(">" + rpy);
				}
			}
		}

		//public static double Min(double lhs, double rhs) { return lhs < rhs ? lhs : rhs; }
		//public static double Max(double lhs, double rhs) { return lhs > rhs ? lhs : rhs; }

		//public static void Main()
		//{
		//    const int count = 500000000;

		//    {
		//        Stopwatch timer = new Stopwatch();
		//        timer.Start();
		//        Random rand = new Random(0);
		//        double lowest = 1;
		//        double highest = -1;
		//        for( int i = 0; i != count; ++i )
		//        {
		//            double v = rand.NextDouble() - 0.5;
		//            lowest  = Math.Min(v, lowest);
		//            highest = Math.Max(v, highest);
		//        }
		//        timer.Stop();
		//        Console.WriteLine("MinMax(A,B): " + timer.ElapsedMilliseconds);
		//    }
			
		//    {
		//        Stopwatch timer = new Stopwatch();
		//        timer.Start();
		//        Random rand = new Random(0);
		//        double lowest = 1;
		//        double highest = -1;
		//        for( int i = 0; i != count; ++i )
		//        {
		//            double v = rand.NextDouble() - 0.5;
		//            lowest  = Math.Min(lowest, v);
		//            highest = Math.Max(highest, v);
		//        }
		//        timer.Stop();
		//        Console.WriteLine("MinMax(B,A): " + timer.ElapsedMilliseconds);
		//    }

		//    {
		//        Stopwatch timer = new Stopwatch();
		//        timer.Start();
		//        Random rand = new Random(0);
		//        double lowest = 1;
		//        double highest = -1;
		//        for( int i = 0; i != count; ++i )
		//        {
		//            double v = rand.NextDouble() - 0.5;
		//            lowest  = Min(v, lowest);
		//            highest = Max(v, highest);
		//        }
		//        timer.Stop();
		//        Console.WriteLine("Local MinMax(A,B): " + timer.ElapsedMilliseconds);
		//    }

		//    {
		//        Stopwatch timer = new Stopwatch();
		//        timer.Start();
		//        Random rand = new Random(0);
		//        double lowest = 1;
		//        double highest = -1;
		//        for( int i = 0; i != count; ++i )
		//        {
		//            double v = rand.NextDouble() - 0.5;
		//            lowest  = Min(lowest, v);
		//            highest = Max(highest, v);
		//        }
		//        timer.Stop();
		//        Console.WriteLine("Local MinMax(B,A): " + timer.ElapsedMilliseconds);
		//    }

		//    {
		//        Stopwatch timer = new Stopwatch();
		//        timer.Start();
		//        Random rand = new Random(0);
		//        double lowest = 1;
		//        double highest = -1;
		//        for( int i = 0; i != count; ++i )
		//        {
		//            double v = rand.NextDouble() - 0.5;
		//            if( v < lowest ) lowest  = v;
		//            if( v > highest) highest = v;
		//        }
		//        timer.Stop();
		//        Console.WriteLine("Ifs used: " + timer.ElapsedMilliseconds);
		//    }
		//}
	}
}
