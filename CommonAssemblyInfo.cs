using System.Reflection;
using System.Runtime.InteropServices;

// Only the projects that target net10.0-windows (WINDOWS is defined for them) are Windows-only; the portable projects
// (net10.0, docs/avalonia-port/CROSS-PLATFORM.md) are checked by the platform analyzer (CA1416) instead.
#if WINDOWS
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows7.0")]
#endif

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Gitanas")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Gitanas")]
[assembly: AssemblyProduct("Gitanas")]
[assembly: AssemblyCopyright("Copyright © 2008-2026 Git Extensions Team")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("33.33.33")]
[assembly: AssemblyFileVersion("33.33.33")]
[assembly: AssemblyInformationalVersion("33.33.33")]

// Disable CLS compliance. See https://github.com/gitextensions/gitextensions/issues/4710
[assembly: CLSCompliant(isCompliant: false)]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
