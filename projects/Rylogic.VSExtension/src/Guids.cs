// Guids.cs
// MUST match guids.h
using System;

namespace Rylogic.VSExtension
{
    static class GuidList
    {
        public const string guidRylogic_VSExtensionPkgString = "DF402917-6013-40CA-A4C6-E1640DA86B90";
        public const string guidRylogic_VSExtensionCmdSetString = "E695E21D-48BB-4B3E-B442-DF64253991A5";

        public static readonly Guid guidRylogic_VSExtensionCmdSet = new Guid(guidRylogic_VSExtensionCmdSetString);
    };
}