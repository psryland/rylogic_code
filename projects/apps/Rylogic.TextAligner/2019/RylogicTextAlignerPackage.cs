using System;
using System.ComponentModel.Design;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Rylogic.Common;
using Rylogic.Utility;

[assembly: ComVisible(false)]
[assembly: AssemblyTitle("Rylogic.TextAligner")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Rylogic.TextAligner")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: AssemblyVersion("1.11.7")]
[assembly: AssemblyFileVersion("1.11.7")]

namespace Rylogic.TextAligner
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the
	/// IVsPackage interface and uses the registration attributes defined in the framework to
	/// register itself and its components with the shell. These attributes tell the pkgdef creation
	/// utility what data to put into .pkgdef file.
	/// </para>
	/// <para>
	/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
	/// </para>
	/// </remarks>
	[Guid(PackageGuidString)]
	[PackageRegistration(UseManagedResourcesOnly = true/*, AllowsBackgroundLoading = true*/)]                   // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package.
	[InstalledProductRegistration("Rylogic.TextAligner", "Rylogic extensions", "1.11.7", IconResourceID = 400)] // This attribute is used to register the information needed to show this package in the Help/About dialog of Visual Studio.
	[ProvideMenuResource("Menus.ctmenu", 1)]                                                                    // This attribute is needed to let the shell know that this package exposes some menus.
	[ProvideOptionPage(typeof(AlignOptions), "Rylogic", "Align Options", 0, 0, true)]                           // This attribute is needed to let the shell know that this package exposes an options page.
	[ProvideBindingPath]                                                                                        // Include the local directory when resolving dependent assemblies
	public sealed class RylogicTextAlignerPackage :Package, IOleCommandTarget
	{
		/// <summary>Rylogic.TextAlignerPackage GUID string.</summary>
		public const string PackageGuidString = "DF402917-6013-40CA-A4C6-E1640DA86B90";

		/// <summary>Singleton log instance</summary>
		public Log Log { get; } = new Log();

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override void Initialize()
		{
			try
			{
				Log.Write(ELogLevel.Info, "Initializing");

				var root = Path_.Directory(Assembly.GetExecutingAssembly().Location);
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
				Assembly? CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
				{
					try
					{
						var path = Path_.CombinePath(root, new AssemblyName(args.Name).Name + ".dll");
						if (!Path_.FileExists(path)) return null;
						return Assembly.LoadFrom(path);
					}
					catch (Exception ex)
					{
						Log.Write(ELogLevel.Error, ex, $"CurrentDomain_AssemblyResolve failed to resolve {args.Name}");
						return null;
					}
				}

				// When initialized asynchronously, the current thread may be a background thread at this point.
				// Do any initialization that requires the UI thread after switching to the UI thread.
				//await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
				base.Initialize();

				// Add our command handlers for menu
				if (GetService<IMenuCommandService>() is OleMenuCommandService mcs)
				{
					mcs.AddCommand(new AlignMenuCommand(this));
					mcs.AddCommand(new UnalignMenuCommand(this));
				}
			}
			catch (Exception ex)
			{
				Log.Write(Utility.ELogLevel.Error, ex, "Error during Initialize");
				throw;
			}
		}

		/// <summary>Return the VS service of type 'TService'</summary>
		public object GetService<TService>()
		{
			try
			{
				// Note: the return value of 'GetService' is not always cast-able to 'TService'
				ThreadHelper.ThrowIfNotOnUIThread();
				return GetService(typeof(TService));
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Error during GetService");
				throw;
			}
		}
		public Task<object> GetServiceAsync<TService>(CancellationToken cancellation_token)
		{
			return System.Threading.Tasks.Task.FromResult(GetService<TService>());
		}

		/// <summary>Return a dialog page</summary>
		public T GetDialogPage<T>() where T : DialogPage
		{
			try
			{
				return (T)GetDialogPage(typeof(T));
			}
			catch (Exception ex)
			{
				Log.Write(Utility.ELogLevel.Error, ex, "Error during GetDialogPage");
				throw;
			}
		}
	}

	static class PkgCmdIDList
	{
		public const int cmdidAlign = 0x100;
		public const int cmdidUnalign = 0x101;
	}

	static class GuidList
	{
		public const string guidRylogicTextAlignerPkgString = "DF402917-6013-40CA-A4C6-E1640DA86B90";
		public const string guidRylogicTextAlignerCmdSetString = "E695E21D-48BB-4B3E-B442-DF64253991A5";
		public static readonly Guid guidRylogicTextAlignerCmdSet = new Guid(guidRylogicTextAlignerCmdSetString);
	}
}
