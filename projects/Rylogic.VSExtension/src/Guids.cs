// Guids.cs
// MUST match guids.h
using System;

namespace Rylogic.VSExtension
{
    static class GuidList
    {
        public const string guidRylogic_VSExtensionPkgString = "1d697591-233a-4a5b-bf85-2fccc769dfe3";
        public const string guidRylogic_VSExtensionCmdSetString = "7154f9e3-6324-4957-8da6-2dfa88ccf58a";

        public static readonly Guid guidRylogic_VSExtensionCmdSet = new Guid(guidRylogic_VSExtensionCmdSetString);
    };
}