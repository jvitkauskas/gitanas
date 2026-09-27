using System.Diagnostics;
using System.Reflection;

namespace GitExtUtils;

/// <summary>
///  The executable, product and data folder of the application, as the WinForms <c>Application</c> gave them (with the
///  same algorithm, from the entry assembly: e.g. the tests keep the data folder of their test host, not the user's).
/// </summary>
public static class ApplicationInfo
{
    private static readonly Lazy<string> _executablePath = new(() => GetExecutablePath(
        Path.GetFullPath(Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0]),
        Assembly.GetEntryAssembly()?.Location,
        File.Exists));
    private static readonly Lazy<FileVersionInfo> _fileVersionInfo = new(() => FileVersionInfo.GetVersionInfo(ExecutablePath));
    private static readonly Lazy<string> _productVersion = new(GetProductVersion);
    private static readonly Lazy<string> _productName = new(GetProductName);
    private static readonly Lazy<string> _companyName = new(GetCompanyName);

    /// <summary>
    ///  The executable of the application: the one of the process, except when the process is the host of .NET (the application
    ///  started as <c>dotnet Gitanas.dll</c>), whose folder is not the one of the application (the portable settings, the
    ///  plugins, the version): then the executable next to the entry assembly (<c>Gitanas</c>, <c>Gitanas.exe</c>),
    ///  else the entry assembly.
    /// </summary>
    internal static string GetExecutablePath(string processPath, string? entryAssemblyPath, Func<string, bool> fileExists)
    {
        if (!Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(entryAssemblyPath))
        {
            return processPath;
        }

        string appHost = Path.ChangeExtension(entryAssemblyPath, OperatingSystem.IsWindows() ? ".exe" : null);
        return fileExists(appHost) ? appHost : entryAssemblyPath;
    }

    /// <summary>The path of the executable of the process (<c>Application.ExecutablePath</c>).</summary>
    public static string ExecutablePath => _executablePath.Value;

    /// <summary>The folder of the executable of the process, without a trailing separator (<c>Application.StartupPath</c>).</summary>
    public static string StartupPath => Path.GetDirectoryName(ExecutablePath)!;

    /// <summary>The informational version of the entry assembly (<c>Application.ProductVersion</c>).</summary>
    public static string ProductVersion => _productVersion.Value;

    /// <summary>The product of the entry assembly (<c>Application.ProductName</c>).</summary>
    public static string ProductName => _productName.Value;

    /// <summary>The company of the entry assembly (<c>Application.CompanyName</c>).</summary>
    public static string CompanyName => _companyName.Value;

    /// <summary>
    ///  The roaming data folder of this version of the application, created if missing: <c>%APPDATA%\company\product\version</c>
    ///  (<c>Application.UserAppDataPath</c>); on Linux in <c>$XDG_CONFIG_HOME</c> (or <c>~/.config</c>), on macOS in
    ///  <c>~/Library/Application Support</c>.
    /// </summary>
    public static string UserAppDataPath
    {
        get
        {
            string path = Path.Join(UserDataRoot, CompanyName, ProductName, ProductVersion);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
    }

    /// <summary>The folder of the data of the user's applications (not checked for existence: it may not exist yet off Windows).</summary>
    private static string UserDataRoot
        => Environment.GetFolderPath(
            OperatingSystem.IsMacOS() ? Environment.SpecialFolder.LocalApplicationData : Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);

    private static string GetProductVersion()
    {
        string? version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return !string.IsNullOrEmpty(version) ? version : _fileVersionInfo.Value.ProductVersion ?? "1.0.0.0";
    }

    private static string GetProductName()
    {
        string? name = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        if (string.IsNullOrEmpty(name))
        {
            name = _fileVersionInfo.Value.ProductName?.Trim();
        }

        if (string.IsNullOrEmpty(name))
        {
            // As WinForms: the namespace of the entry point, after its first part.
            string? ns = Assembly.GetEntryAssembly()?.EntryPoint?.DeclaringType?.Namespace;
            if (!string.IsNullOrEmpty(ns))
            {
                int lastDot = ns.LastIndexOf('.');
                name = lastDot != -1 && lastDot < ns.Length - 1 ? ns[(lastDot + 1)..] : ns;
            }
            else
            {
                name = Assembly.GetEntryAssembly()?.EntryPoint?.DeclaringType?.Name ?? "";
            }
        }

        return name;
    }

    private static string GetCompanyName()
    {
        string? name = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
        if (string.IsNullOrEmpty(name))
        {
            name = _fileVersionInfo.Value.CompanyName?.Trim();
        }

        if (string.IsNullOrEmpty(name))
        {
            // As WinForms: the first part of the namespace of the entry point.
            string? ns = Assembly.GetEntryAssembly()?.EntryPoint?.DeclaringType?.Namespace;
            if (!string.IsNullOrEmpty(ns))
            {
                int firstDot = ns.IndexOf('.');
                name = firstDot != -1 ? ns[..firstDot] : ns;
            }
            else
            {
                name = ProductName;
            }
        }

        return name;
    }
}
