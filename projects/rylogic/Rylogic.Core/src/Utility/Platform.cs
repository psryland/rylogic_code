namespace Rylogic.Utility;

using System;

public class Platform
{
#if NET5_0_OR_GREATER
    /// <summary>Check if the current platform is Windows</summary>
    public static bool IsWindows() => OperatingSystem.IsWindows();
#elif WINDOWS
    public static bool IsWindows() => true;

#else
    /// <summary>Check if the current platform is Windows</summary>
    public static bool IsWindows() =>
        Environment.OSVersion.Platform == PlatformID.Win32NT ||
        Environment.OSVersion.Platform == PlatformID.Win32Windows ||
        Environment.OSVersion.Platform == PlatformID.Win32S;

#endif
}
