using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualStudio.DebuggerVisualizers;
using Rylogic.Extn;

namespace Rylogic
{
	/// <summary>Base class for a visualiser</summary>
	public abstract class VisualiserBase<TType, TForm> :DialogDebuggerVisualizer where TForm : Control
	{
		/// <summary>Show the visualiser</summary>
		protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
		{
			// Note: Async visualisers aren't possible. It seems VS loads the visualiser in an
			// AppDomain and then kills it and everything it creates when this function returns.
			try
			{
				var obj = (TType)objectProvider.GetObject();
				using (var dlg = (TForm)Activator.CreateInstance(typeof(TForm), obj))
					windowService.ShowDialog(dlg);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Visualiser Error: " + ex.Message);
				MessageBox.Show(ex.MessageFull());
			}
		}
	}

	public abstract class VisualiserObjectSourceBase<TType> :VisualizerObjectSource
	{
		public override void GetData(object target, Stream outgoingData)
		{
			base.GetData((object)(TType)target, outgoingData);
		}
	}
}
