using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pr.maths;

namespace CoreCalc
{
	public class Model
	{
		public Model(MainUI ui)
		{
			UI = ui;
			CoreInclinationDeg = 90;
			CoreAzimuthDeg = 0;
		}

		/// <summary>The main UI</summary>
		public MainUI UI
		{
			get;
			private set;
		}

		/// <summary>The earth-space normal direction of the core</summary>
		public v4 CoreAxis
		{
			get
			{
				var a = Maths.DegreesToRadians(CoreAzimuthDeg);
				var i = Maths.DegreesToRadians(-CoreInclinationDeg);
				var x = (float)(Math.Sin(a)*Math.Cos(i));
				var y = (float)(Math.Cos(a)*Math.Cos(i));
				var z = (float)(Math.Sin(i));
				return new v4(x,y,z,0);
			}
		}

		/// <summary>The inclination angle of the core axis in degrees</summary>
		public double CoreInclinationDeg
		{
			get { return m_core_incline; }
			set { m_core_incline = Maths.Clamp(value, -90, +90); }
		}
		private double m_core_incline;

		/// <summary>The azimuth angle of the core axis in degrees</summary>
		public double CoreAzimuthDeg
		{
			get { return m_core_azimuth; }
			set { m_core_azimuth = Maths.Clamp(value, 0, 360); }
		}
		private double m_core_azimuth;
	}
}
