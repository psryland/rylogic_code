using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.util;

namespace CppPad
{
	public sealed class Settings :SettingsBase<Settings>
	{
		public Settings()
		{
			CompilerType         = ECompiler.MSVC;
			MSVC                 = new MSVCCompilerSettings();
			LastProject          = string.Empty;
			RecentProjects       = string.Empty;
			RecentFiles          = string.Empty;
			SaveAllBeforeCompile = true;
			UILayout             = null;

			AutoSaveOnChanges = true;
		}
		public Settings(string filepath) :base(filepath)
		{
			AutoSaveOnChanges = true;
		}
		public Settings(Settings rhs, bool read_only = false) :base(rhs, read_only)
		{
			AutoSaveOnChanges = true;
		}

		/// <summary>The type of compiler to use</summary>
		public ECompiler CompilerType
		{
			get { return get(x => x.CompilerType); }
			set { set(x => x.CompilerType, value); }
		}
		public enum ECompiler
		{
			MSVC,
		}

		/// <summary>MSVC Compiler settings</summary>
		public MSVCCompilerSettings MSVC
		{
			get { return get(x => x.MSVC); }
			set
			{
				if (value == null) throw new ArgumentNullException("Setting '{0}' cannot be null".Fmt(nameof(Settings.MSVC)));
				set(x => x.MSVC, value);
			}
		}

		/// <summary>The last project loaded</summary>
		public string LastProject
		{
			get { return get(x => x.LastProject); }
			set { set(x => x.LastProject, value); }
		}

		/// <summary>Recent projects list</summary>
		public string RecentProjects
		{
			get { return get(x => x.RecentProjects); }
			set { set(x => x.RecentProjects, value); }
		}

		/// <summary>Recent files list</summary>
		public string RecentFiles
		{
			get { return get(x => x.RecentFiles); }
			set { set(x => x.RecentFiles, value); }
		}

		/// <summary>Save all open files before compiling</summary>
		public bool SaveAllBeforeCompile
		{
			get { return get(x => x.SaveAllBeforeCompile); }
			set { set(x => x.SaveAllBeforeCompile, value); }
		}

		/// <summary>The dock panel layout</summary>
		public XElement UILayout
		{
			get { return get(x => x.UILayout); }
			set { set(x => x.UILayout, value); }
		}
	}

	/// <summary>Compiler options</summary>
	[TypeConverter(typeof(MSVCCompilerSettings.TyConv))]
	public class MSVCCompilerSettings :SettingsSet<MSVCCompilerSettings>
	{
		public MSVCCompilerSettings()
		{
			Switches                = new List<string>();
			Defines                 = new List<string>();
			IncludePaths            = new List<string>();
			LibraryPaths            = new List<string>();
			EnvironmentIncludePaths = new List<string>();
			EnvironmentLibraryPaths = new List<string>();
			CompilerLibraryPaths    = new List<string>();
			WindowsLibPaths         = new List<string>();
			EnvironmentVars         = new Dictionary<string, string>();

			VSVersion              = "14.0";
			VSDirectory            = @"C:\Program Files (x86)\Microsoft Visual Studio 14.0";
			WinSDK                 = @"C:\Program Files (x86)\Windows Kits\10";
			DotNetTools            = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools";
			DotNetFramework        = @"C:\Windows\Microsoft.NET\Framework";
			DotNetFrameworkVersion = @"v4.0.30319";
			WinKitVersion          = "10.0.10240.0";

			CompilerExePath = Path_.CombinePath(VSDirectory, "\\VC\\bin\\cl.exe");
			LinkerExePath   = Path_.CombinePath(VSDirectory, "\\VC\\bin\\link.exe");

			Switches .Add(new[] { "/nologo", "/EHsc" });
			Defines  .Add(new[] { "NOMINMAX" });

			#region EnvironmentIncludePaths
			EnvironmentIncludePaths.Add(Path_.CombinePath(VSDirectory, "\\vc\\include"));
			EnvironmentIncludePaths.Add(Path_.CombinePath(VSDirectory, "\\vc\\atlmfc\\include"));
			EnvironmentIncludePaths.Add(Path_.CombinePath(WinSDK, "\\include\\", WinKitVersion, "\\ucrt"));
			EnvironmentIncludePaths.Add(Path_.CombinePath(WinSDK, "\\include\\", WinKitVersion, "\\shared"));
			EnvironmentIncludePaths.Add(Path_.CombinePath(WinSDK, "\\include\\", WinKitVersion, "\\um"));
			EnvironmentIncludePaths.Add(Path_.CombinePath(WinSDK, "\\include\\", WinKitVersion, "\\winrt"));
			EnvironmentIncludePaths.Add(Path_.CombinePath(WinSDK, "\\..\\NETFXSDK\\4.6\\include\\um"));
			#endregion

			#region EnvironmentLibraryPaths
			EnvironmentLibraryPaths.Add(Path_.CombinePath(VSDirectory, "\\vc\\lib"));
			EnvironmentLibraryPaths.Add(Path_.CombinePath(VSDirectory, "\\vc\\atlmfc\\lib"));
			EnvironmentLibraryPaths.Add(Path_.CombinePath(WinSDK, "\\lib\\", WinKitVersion, "\\ucrt\\x86"));
			EnvironmentLibraryPaths.Add(Path_.CombinePath(WinSDK, "\\lib\\", WinKitVersion, "\\um\\x86"));
			EnvironmentLibraryPaths.Add(Path_.CombinePath(WinSDK, "\\..\\NETFXSDK\\4.6\\lib\\um\\x86"));
			#endregion

			#region CompilerLibraryPaths
			CompilerLibraryPaths.Add(Path_.CombinePath(DotNetFramework));
			CompilerLibraryPaths.Add(Path_.CombinePath(VSDirectory, "\\vc\\lib"));
			CompilerLibraryPaths.Add(Path_.CombinePath(VSDirectory, "\\vc\\atlmfc\\lib"));
			CompilerLibraryPaths.Add(Path_.CombinePath(WinSDK, "\\UnionMetadata"));
			CompilerLibraryPaths.Add(Path_.CombinePath(WinSDK, "\\References"));
			CompilerLibraryPaths.Add(Path_.CombinePath(WinSDK, "\\References\\Windows.Foundation.UniversalApiContract\\1.0.0.0"));
			CompilerLibraryPaths.Add(Path_.CombinePath(WinSDK, "\\References\\Windows.Foundation.FoundationContract\\1.0.0.0"));
			CompilerLibraryPaths.Add(Path_.CombinePath(WinSDK, "\\References\\Windows.Networking.Connectivity.WwanContract\\1.0.0.0"));
			CompilerLibraryPaths.Add(Path_.CombinePath(WinSDK, "\\ExtensionSDKs\\Microsoft.VCLibs\\14.0\\References\\CommonConfiguration\\neutral"));
			#endregion

			#region WindowsLibPaths
			WindowsLibPaths.Add(Path_.CombinePath(WinSDK, "\\UnionMetadata"));
			WindowsLibPaths.Add(Path_.CombinePath(WinSDK, "\\References"));
			WindowsLibPaths.Add(Path_.CombinePath(WinSDK, "\\References\\Windows.Foundation.UniversalApiContract\\1.0.0.0"));
			WindowsLibPaths.Add(Path_.CombinePath(WinSDK, "\\References\\Windows.Foundation.FoundationContract\\1.0.0.0"));
			WindowsLibPaths.Add(Path_.CombinePath(WinSDK, "\\References\\Windows.Networking.Connectivity.WwanContract\\1.0.0.0"));
			#endregion

			UpdateEnvironmentVars();
		}

		/// <summary>The full path to the compiler</summary>
		[Description("The full path to the compiler")]
		public string CompilerExePath
		{
			get { return get(x => x.CompilerExePath); }
			set { set(x => x.CompilerExePath, value); }
		}

		/// <summary>The full path to the linker</summary>
		[Description("The full path to the linker")]
		public string LinkerExePath
		{
			get { return get(x => x.LinkerExePath); }
			set { set(x => x.LinkerExePath, value); }
		}

		/// <summary>Command line switches</summary>
		[Description("The compiler switches")]
		public List<string> Switches
		{
			get { return get(x => x.Switches); }
			set { set(x => x.Switches, value); }
		}

		/// <summary>Preprocessor defines</summary>
		[Description("Preprocessor defines")]
		public List<string> Defines
		{
			get { return get(x => x.Defines); }
			set { set(x => x.Defines, value); }
		}

		/// <summary>Additional include search paths provided on the command line</summary>
		[Description("Additional include search paths provided on the command line")]
		public List<string> IncludePaths
		{
			get { return get(x => x.IncludePaths); }
			set { set(x => x.IncludePaths, value); }
		}

		/// <summary>Additional library search paths provided on the command line</summary>
		[Description("Additional library search paths provided on the command line")]
		public List<string> LibraryPaths
		{
			get { return get(x => x.LibraryPaths); }
			set { set(x => x.LibraryPaths, value); }
		}

		/// <summary>The include paths that are set as environment variables</summary>
		[Description("The include paths that are set as environment variables")]
		public List<string> EnvironmentIncludePaths
		{
			get { return get(x => x.EnvironmentIncludePaths); }
			set { set(x => x.EnvironmentIncludePaths, value); }
		}

		/// <summary>Library search paths that are set as environment variables</summary>
		[Description("Library search paths that are set as environment variables")]
		public List<string> EnvironmentLibraryPaths
		{
			get { return get(x => x.EnvironmentLibraryPaths); }
			set { set(x => x.EnvironmentLibraryPaths, value); }
		}

		/// <summary>Library search paths that are set as environment variables</summary>
		[Description("Library search paths that are set as environment variables")]
		public List<string> CompilerLibraryPaths
		{
			get { return get(x => x.EnvironmentLibraryPaths); }
			set { set(x => x.EnvironmentLibraryPaths, value); }
		}

		/// <summary>Search paths for the windows libraries</summary>
		[Description("Search paths for the windows libraries")]
		public List<string> WindowsLibPaths
		{
			get { return get(x => x.WindowsLibPaths); }
			set { set(x => x.WindowsLibPaths, value); }
		}

		/// <summary>The environment variables required by the compiler</summary>
		[Description("The environment variables required by the compiler")]
		public Dictionary<string,string> EnvironmentVars
		{
			get { return get(x => x.EnvironmentVars); }
			set { set(x => x.EnvironmentVars, value); }
		}

		/// <summary>The version of visual studio</summary>
		[Description("The version of visual studio to use")]
		public string VSVersion
		{
			get { return get(x => x.VSDirectory); }
			set { set(x => x.VSDirectory, value); }
		}

		/// <summary>The root install directory of visual studio</summary>
		[Description("The root directory of where Visual Studio is installed")]
		public string VSDirectory
		{
			get { return get(x => x.VSDirectory); }
			set { set(x => x.VSDirectory, value); }
		}

		/// <summary>The Windows Kit (SDK) root directory</summary>
		[Description("The Windows Kit (SDK) root directory")]
		public string WinSDK
		{
			get { return get(x => x.WinSDK); }
			set { set(x => x.WinSDK, value); }
		}

		/// <summary>The full version of the Windows Kit (SDK)</summary>
		[Description("The full version of the Windows Kit (SDK)")]
		public string WinKitVersion
		{
			get { return get(x => x.WinKitVersion); }
			set { set(x => x.WinKitVersion, value); }
		}

		/// <summary>The .NET framework root directory</summary>
		[Description("The .NET framework root directory")]
		public string DotNetFramework
		{
			get { return get(x => x.DotNetFramework); }
			set { set(x => x.DotNetFramework, value); }
		}

		/// <summary>The .NET framework full version</summary>
		[Description("The .NET framework full version")]
		public string DotNetFrameworkVersion
		{
			get { return get(x => x.DotNetFrameworkVersion); }
			set { set(x => x.DotNetFrameworkVersion, value); }
		}

		/// <summary>The .NET framework tools root directory</summary>
		[Description("The .NET framework tools root directory")]
		public string DotNetTools
		{
			get { return get(x => x.DotNetTools); }
			set { set(x => x.DotNetTools, value); }
		}

		/// <summary>Update include paths that used to start with 'old' to 'nue'</summary>
		private void UpdatePaths(string old, string nue)
		{
			// Update environment include paths
			for (var i = 0; i != EnvironmentIncludePaths.Count; ++i)
			{
				if (!EnvironmentIncludePaths[i].StartsWith(old)) continue;
				EnvironmentIncludePaths[i] = nue + EnvironmentIncludePaths[i].Substring(old.Length);
			}

			// Update environment library paths
			for (var i = 0; i != EnvironmentLibraryPaths.Count; ++i)
			{
				if (!EnvironmentLibraryPaths[i].StartsWith(old)) continue;
				EnvironmentLibraryPaths[i] = nue + EnvironmentLibraryPaths[i].Substring(old.Length);
			}

			// Update compiler library paths
			for (var i = 0; i != CompilerLibraryPaths.Count; ++i)
			{
				if (!EnvironmentLibraryPaths[i].StartsWith(old)) continue;
				EnvironmentLibraryPaths[i] = nue + EnvironmentLibraryPaths[i].Substring(old.Length);
			}

			// Update windows library paths
			for (var i = 0; i != WindowsLibPaths.Count; ++i)
			{
				if (!WindowsLibPaths[i].StartsWith(old)) continue;
				WindowsLibPaths[i] = nue + WindowsLibPaths[i].Substring(old.Length);
			}

			// Update the environment variables
			UpdateEnvironmentVars();
		}

		/// <summary>Populate the environment variables from the current settings</summary>
		public void UpdateEnvironmentVars()
		{
			EnvironmentVars["DevEnvDir"         ] = Path_.CombinePath(VSDirectory, "\\Common7\\IDE\\");
			EnvironmentVars["ExtensionSdkDir"   ] = Path_.CombinePath(WinSDK, "\\Extension SDKs");
			EnvironmentVars["Framework40Version"] = "v4.0";
			EnvironmentVars["FrameworkDir"      ] = DotNetFramework + "\\";
			EnvironmentVars["FrameworkDIR32"    ] = DotNetFramework + "\\";
			EnvironmentVars["FrameworkVersion"  ] = DotNetFrameworkVersion;
			EnvironmentVars["FrameworkVersion32"] = DotNetFrameworkVersion;

			EnvironmentVars["INCLUDE"    ] = string.Join(";", EnvironmentIncludePaths);
			EnvironmentVars["LIB"        ] = string.Join(";", EnvironmentLibraryPaths);
			EnvironmentVars["LIBPATH"    ] = string.Join(";", CompilerLibraryPaths);
			EnvironmentVars["NETFXSDKDir"] = Path_.CombinePath(WinSDK + "\\..\\NETFXSDK\\4.6\\");

			EnvironmentVars["UCRTVersion"        ] = WinKitVersion;
			EnvironmentVars["UniversalCRTSdkDir" ] = Path_.CombinePath(WinSDK, "\\");
			EnvironmentVars["VCINSTALLDIR"       ] = Path_.CombinePath(VSDirectory, "\\VC\\");
			EnvironmentVars["VisualStudioVersion"] = VSVersion;
			EnvironmentVars["VSINSTALLDIR"       ] = Path_.CombinePath(VSDirectory, "\\");

			EnvironmentVars["WindowsLibPath"] = string.Join(";", WindowsLibPaths);

			EnvironmentVars["WindowsSdkDir"                ] = Path_.CombinePath(WinSDK, "\\");
			EnvironmentVars["WindowsSDKLibVersion"         ] = VSVersion + "\\";
			EnvironmentVars["WindowsSDKVersion"            ] = VSVersion + "\\";
			EnvironmentVars["WindowsSDK_ExecutablePath_x64"] = Path_.CombinePath(DotNetTools, "\\x64\\");
			EnvironmentVars["WindowsSDK_ExecutablePath_x86"] = Path_.CombinePath(DotNetTools, "\\");
		}

		private class TyConv :GenericTypeConverter<MSVCCompilerSettings> {}
	}
}
