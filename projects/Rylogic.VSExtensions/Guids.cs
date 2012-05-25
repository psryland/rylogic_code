// Guids.cs
// MUST match guids.h
using System;

namespace RylogicLimited.Rylogic_VSExtensions
{
    static class GuidList
    {
        public const string guidRylogic_VSExtensionsPkgString = "8ff65aee-8ab9-4cf1-8696-cbc83b886e71";
        public const string guidRylogic_VSExtensionsCmdSetString = "de41e5b5-45c6-4b24-b38d-0086045a4ebc";
        public const string guidToolWindowPersistanceString = "1ee9a9af-fd73-4ee5-a872-c8d19e4f5eac";

        public static readonly Guid guidRylogic_VSExtensionsCmdSet = new Guid(guidRylogic_VSExtensionsCmdSetString);
    };
}