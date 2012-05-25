using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace RylogicLimited.Rylogic_VSExtensions
{
	// This seems to get called when VS creates an editable text file
	// It lets us hook command filters into that apply to that text file (I guess)
	[Export(typeof(IVsTextViewCreationListener))]
	[ContentType("code")]
	[TextViewRole(PredefinedTextViewRoles.Editable)]
	class VsTextViewCreationListener : IVsTextViewCreationListener
	{
		[Import]
		IVsEditorAdaptersFactoryService AdaptersFactory = null;
		
		public void VsTextViewCreated(IVsTextView textViewAdapter)
		{
			var wpfTextView = AdaptersFactory.GetWpfTextView(textViewAdapter);
			if (wpfTextView == null)
			{
				Debug.Fail("Unable to get IWpfTextView from text view adapter");
				return;
			}
			
			Cmd_AlignAssignments.Register(textViewAdapter, wpfTextView);
		}
	}
}