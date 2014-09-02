//***************************************************
// Maths test
//	(c)opyright Paul Ryland 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Text;
using PR;

namespace TestCS
{
    class Program
    {
        static void Main(string[] args)
        {
			v4 v = v4.Origin;
			v4 u = v; u.x = 1f;
			Console.WriteLine(v);
			Console.WriteLine(u);
			
			v4 x_axis = new v4(1f, 0f, 0f, 0f);
			v4 y_axis = new v4(0f, 1f, 0f, 0f);
			v4 z_axis = v4.Cross3(x_axis, y_axis);
			v4 origin = v4.Origin;
			m4x4 mat = new m4x4(x_axis, y_axis, z_axis, origin);
			
			Console.WriteLine(mat);
			Console.ReadLine();
        }
    }
}
