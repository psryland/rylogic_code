using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell;

namespace RylogicLimited.Rylogic_VSExtensions
{
	public class Cmd_StepThru
	{
		/// <summary>Create and register the Cmd_StepThru command.</summary>
		public static void Register(OleMenuCommandService mcs)
		{
			var cmd = new Cmd_StepThru();
			var cmd_id = new CommandID(GuidList.guidRylogic_VSExtensionsCmdSet, (int)PkgCmdIDList.CmdId_StepThru);
			var mu_cmd = new MenuCommand(cmd.DoCmd, cmd_id);
			mcs.AddCommand(mu_cmd);
		}
		
		/// <summary>A list of step through filters</summary>
		private readonly List<Regex> m_step_through_filter = new List<Regex>();
		
		/// <summary>Use the register method</summary>
		private Cmd_StepThru() {}
		
		/// <summary>Execute the command</summary>
		public void DoCmd(object sender, EventArgs args)
		{
			Func<string,bool> Match = (fn)=>
				{
					foreach (var filter in m_step_through_filter)
						if (filter.Match(fn).Success) return true;
					return false;
				};
			
					//handled = true;
			//Debugger2 debugger=(Debugger2)GetServem_dte.Debugger;
			//do { debugger.StepInto(); }
			//while (Match(debugger.CurrentStackFrame.FunctionName));

			//// Show a Message Box to prove we were here
			//IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
			//Guid clsid = Guid.Empty;
			//int result;
			//Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
			//            0,
			//            ref clsid,
			//            "Rylogic.VSExtensions",
			//            string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()),
			//            string.Empty,
			//            0,
			//            OLEMSGBUTTON.OLEMSGBUTTON_OK,
			//            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
			//            OLEMSGICON.OLEMSGICON_INFO,
			//            0,        // false
			//            out result));
		}
	}
}