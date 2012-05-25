
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace RylogicLimited.Rylogic_VSExtensions
{
	[ClassInterface(ClassInterfaceType.AutoDual)]
	[CLSCompliant(false), ComVisible(true)]
	public class OptionsPageGrid :DialogPage
	{
		[Category("General")]
		[DisplayName("Step Through Filters")]
		[Description("Regular expressions for functions to step through")]
		public string[] StepThruFilters { get; set; }
	};
}