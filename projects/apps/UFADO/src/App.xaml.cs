using System;
using System.Windows;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

namespace UFADO;

public partial class App :Application
{
	/// <summary>App singleton access</summary>
	public static new App Current => (App)Application.Current;

	/// <inheritdoc/>
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		try
		{
			Xml_.Config
				.SupportWPFTypes()
				.SupportRylogicMathsTypes()
				.SupportRylogicCommonTypes();

			var settings = new Settings(Settings.DefaultFilepath);
			Model = new Model(settings);
			var ui = new MainWindow(settings, Model);
			ui.Show();
		}
		catch (Exception ex)
		{
			WPFUtil.WaitForDebugger();

			MsgBox.Show(null, $"Crash!\r\n\r\n{ex.MessageFull()}\r\n\r\n{ex.StackTrace}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Error);
			Shutdown();
		}
	}

	/// <inheritdoc/>
	protected override void OnExit(ExitEventArgs e)
	{
		Model = null!;
		base.OnExit(e);
	}

	/// <summary>Application logic</summary>
	private Model Model
	{
		get;
		set
		{
			if (field == value) return;
			Util.Dispose(ref field!);
			field = value;
		}
	} = null!;
}
