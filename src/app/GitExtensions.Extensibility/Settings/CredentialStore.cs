using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace GitExtensions.Extensibility.Settings;

/// <summary>
///  The secure store of credentials of the system (docs/avalonia-port/CROSS-PLATFORM.md, phase 4): the Credential Manager
///  of Windows, the Keychain of macOS, the Secret Service of Linux (libsecret: GNOME Keyring, KWallet, KeePassXC).
///  Elsewhere, or without a secret service, <see cref="IsAvailable"/> is <see langword="false"/> and nothing is stored.
/// </summary>
public interface ICredentialStore
{
    /// <summary>Whether credentials can be stored (the store exists and answers).</summary>
    bool IsAvailable { get; }

    /// <summary>The credentials stored for <paramref name="target"/>, or <see langword="null"/>.</summary>
    NetworkCredential? Get(string target);

    /// <summary>Stores the credentials for <paramref name="target"/>; whether they were stored.</summary>
    bool Save(string target, NetworkCredential credential);

    /// <summary>Removes the credentials of <paramref name="target"/>; whether there were some.</summary>
    bool Remove(string target);
}

/// <summary>The credential store of the system.</summary>
public static class CredentialStores
{
    public static ICredentialStore Current { get; set; }
        = OperatingSystem.IsWindows() ? new WindowsCredentialStore()
            : OperatingSystem.IsMacOS() ? new MacOSKeychainCredentialStore()
            : OperatingSystem.IsLinux() ? new SecretServiceCredentialStore()
            : new NoCredentialStore();
}

/// <summary>No store: nothing is kept (the systems other than Windows, macOS and Linux).</summary>
internal sealed class NoCredentialStore : ICredentialStore
{
    public bool IsAvailable => false;

    public NetworkCredential? Get(string target) => null;

    public bool Save(string target, NetworkCredential credential) => false;

    public bool Remove(string target) => false;
}

/// <summary>The Credential Manager of Windows (AdysTech.CredentialManager), with the targets prefixed by <c>GitExtensions_</c>.</summary>
internal sealed class WindowsCredentialStore : ICredentialStore
{
    private const string TargetPrefix = "GitExtensions_";

    public bool IsAvailable => true;

    public NetworkCredential? Get(string target) => AdysTech.CredentialManager.CredentialManager.GetCredentials(GetTarget(target));

    public bool Save(string target, NetworkCredential credential)
        => AdysTech.CredentialManager.CredentialManager.SaveCredentials(GetTarget(target), credential) is not null;

    public bool Remove(string target)
        => AdysTech.CredentialManager.CredentialManager.GetCredentials(GetTarget(target)) is not null
            && AdysTech.CredentialManager.CredentialManager.RemoveCredentials(GetTarget(target));

    private static string GetTarget(string rawTarget)
        => string.IsNullOrWhiteSpace(rawTarget) ? throw new ArgumentNullException(nameof(rawTarget)) : $"{TargetPrefix}{rawTarget}";
}

/// <summary>
///  The Secret Service of freedesktop.org through libsecret: one item per target (attribute <c>target</c> of the schema
///  <c>org.gitextensions.Credentials</c>), whose secret is the user name and the password on two lines.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed partial class SecretServiceCredentialStore : ICredentialStore
{
    private const string LibSecret = "libsecret-1.so.0";
    private const string LibGLib = "libglib-2.0.so.0";
    private const string SchemaName = "org.gitextensions.Credentials";
    private const string TargetAttribute = "target";

    private static readonly Lazy<IntPtr> _schema = new(CreateSchema);
    private readonly Lazy<bool> _isAvailable;

    public SecretServiceCredentialStore()
    {
        _isAvailable = new(CheckAvailable);
    }

    public bool IsAvailable => _isAvailable.Value;

    private static bool CheckAvailable()
    {
        // Whether a secret service answers on the session bus, without unlocking anything (a lookup would ask to
        // unlock the keyring). Guarded by a timeout: D-Bus may start a service that does not answer.
        Task<bool> check = Task.Run(() =>
        {
            try
            {
                IntPtr service = secret_service_get_sync(0 /* SECRET_SERVICE_NONE */, IntPtr.Zero, out IntPtr error);
                if (service == IntPtr.Zero || !CheckError(error))
                {
                    return false;
                }

                g_object_unref(service);
                return true;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                Trace.WriteLine($"No libsecret: the credentials are not stored. {ex.Message}");
                return false;
            }
        });

#pragma warning disable VSTHRD002 // A native call with a timeout.
        return check.Wait(TimeSpan.FromSeconds(10)) && check.Result;
#pragma warning restore VSTHRD002
    }

    public NetworkCredential? Get(string target)
    {
        if (!IsAvailable || !TryLookup(target, out string? secret) || secret is null)
        {
            return null;
        }

        int newLine = secret.IndexOf('\n');
        return newLine < 0 ? new NetworkCredential(secret, "") : new NetworkCredential(secret[..newLine], secret[(newLine + 1)..]);
    }

    public bool Save(string target, NetworkCredential credential)
    {
        if (!IsAvailable)
        {
            return false;
        }

        return WithAttributes(target, attributes =>
        {
            int stored = secret_password_storev_sync(_schema.Value, attributes, collection: null, $"Gitanas: {target}", $"{credential.UserName}\n{credential.Password}", IntPtr.Zero, out IntPtr error);
            return stored != 0 && CheckError(error);
        });
    }

    public bool Remove(string target)
    {
        if (!IsAvailable)
        {
            return false;
        }

        return WithAttributes(target, attributes =>
        {
            int removed = secret_password_clearv_sync(_schema.Value, attributes, IntPtr.Zero, out IntPtr error);
            return removed != 0 && CheckError(error);
        });
    }

    private static bool TryLookup(string target, [MaybeNullWhen(false)] out string? secret)
    {
        string? found = null;
        bool answered = WithAttributes(target, attributes =>
        {
            IntPtr password = secret_password_lookupv_sync(_schema.Value, attributes, IntPtr.Zero, out IntPtr error);
            if (!CheckError(error))
            {
                return false;
            }

            if (password != IntPtr.Zero)
            {
                found = Marshal.PtrToStringUTF8(password);
                secret_password_free(password);
            }

            return true;
        });
        secret = found;
        return answered;
    }

    /// <summary>Runs <paramref name="action"/> with a <c>GHashTable</c> of the attributes of <paramref name="target"/>.</summary>
    private static bool WithAttributes(string target, Func<IntPtr, bool> action)
    {
        IntPtr glib = NativeLibrary.Load(LibGLib);
        IntPtr table = g_hash_table_new(NativeLibrary.GetExport(glib, "g_str_hash"), NativeLibrary.GetExport(glib, "g_str_equal"));
        IntPtr key = Marshal.StringToCoTaskMemUTF8(TargetAttribute);
        IntPtr value = Marshal.StringToCoTaskMemUTF8(target);
        try
        {
            g_hash_table_insert(table, key, value);
            return action(table);
        }
        finally
        {
            g_hash_table_unref(table);
            Marshal.FreeCoTaskMem(key);
            Marshal.FreeCoTaskMem(value);
        }
    }

    /// <summary>Whether there was no error (a <c>GError</c> is traced and freed).</summary>
    private static bool CheckError(IntPtr error)
    {
        if (error == IntPtr.Zero)
        {
            return true;
        }

        // GError: GQuark domain, gint code, gchar* message.
        Trace.WriteLine($"Secret Service: {Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(error, 8))}");
        g_error_free(error);
        return false;
    }

    /// <summary>The <c>SecretSchema</c> of the credentials (kept for the life of the process).</summary>
    private static IntPtr CreateSchema()
    {
        SecretSchema schema = new()
        {
            Name = Marshal.StringToCoTaskMemUTF8(SchemaName),
            Flags = 0, // SECRET_SCHEMA_NONE
            Attributes = new SecretSchemaAttribute[32],
        };
        schema.Attributes[0] = new SecretSchemaAttribute { Name = Marshal.StringToCoTaskMemUTF8(TargetAttribute), Type = 0 }; // SECRET_SCHEMA_ATTRIBUTE_STRING
        IntPtr pointer = Marshal.AllocHGlobal(Marshal.SizeOf<SecretSchema>());
        Marshal.StructureToPtr(schema, pointer, fDeleteOld: false);
        return pointer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SecretSchemaAttribute
    {
        public IntPtr Name;
        public int Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SecretSchema
    {
        public IntPtr Name;
        public int Flags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public SecretSchemaAttribute[] Attributes;
        public int Reserved;
        public IntPtr Reserved1;
        public IntPtr Reserved2;
        public IntPtr Reserved3;
        public IntPtr Reserved4;
        public IntPtr Reserved5;
        public IntPtr Reserved6;
        public IntPtr Reserved7;
    }

    [LibraryImport(LibSecret, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int secret_password_storev_sync(IntPtr schema, IntPtr attributes, string? collection, string label, string password, IntPtr cancellable, out IntPtr error);

    [LibraryImport(LibSecret)]
    private static partial IntPtr secret_service_get_sync(int flags, IntPtr cancellable, out IntPtr error);

    [LibraryImport("libgobject-2.0.so.0")]
    private static partial void g_object_unref(IntPtr instance);

    [LibraryImport(LibSecret)]
    private static partial IntPtr secret_password_lookupv_sync(IntPtr schema, IntPtr attributes, IntPtr cancellable, out IntPtr error);

    [LibraryImport(LibSecret)]
    private static partial int secret_password_clearv_sync(IntPtr schema, IntPtr attributes, IntPtr cancellable, out IntPtr error);

    [LibraryImport(LibSecret)]
    private static partial void secret_password_free(IntPtr password);

    [LibraryImport(LibGLib)]
    private static partial IntPtr g_hash_table_new(IntPtr hashFunc, IntPtr keyEqualFunc);

    [LibraryImport(LibGLib)]
    private static partial int g_hash_table_insert(IntPtr hashTable, IntPtr key, IntPtr value);

    [LibraryImport(LibGLib)]
    private static partial void g_hash_table_unref(IntPtr hashTable);

    [LibraryImport(LibGLib)]
    private static partial void g_error_free(IntPtr error);
}

/// <summary>
///  The login Keychain of macOS (the generic passwords of the Security framework, as Git Credential Manager): one item per
///  target, whose service is the target prefixed by <c>GitExtensions_</c> (the name of the Windows target), whose account
///  is the user name and whose password is the password. Only this application reads its items without asking the user.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed unsafe partial class MacOSKeychainCredentialStore : ICredentialStore
{
    private const string SecurityFramework = "/System/Library/Frameworks/Security.framework/Security";
    private const string CoreFoundationFramework = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string ServicePrefix = "GitExtensions_";
    private const int ErrSecItemNotFound = -25300;
    private const uint AccountAttribute = 0x61636374; // kSecAccountItemAttr, 'acct'
    private const uint StringFormat = 0; // CSSM_DB_ATTRIBUTE_FORMAT_STRING

    public bool IsAvailable => true;

    public NetworkCredential? Get(string target)
    {
        byte[] service = GetService(target);
        uint length = 0;
        void* data = null;
        IntPtr item = IntPtr.Zero;
        int status;
        fixed (byte* serviceName = service)
        {
            status = SecKeychainFindGenericPassword(IntPtr.Zero, (uint)service.Length, serviceName, 0, null, &length, &data, &item);
        }

        if (!Succeeded(status))
        {
            return null;
        }

        try
        {
            return new NetworkCredential(GetAccount(item) ?? "", Encoding.UTF8.GetString((byte*)data, (int)length));
        }
        finally
        {
            SecKeychainItemFreeContent(null, data);
            CFRelease(item);
        }
    }

    public bool Save(string target, NetworkCredential credential)
    {
        byte[] service = GetService(target);
        byte[] account = Encoding.UTF8.GetBytes(credential.UserName);
        byte[] password = Encoding.UTF8.GetBytes(credential.Password);
        IntPtr item = FindItem(service);
        fixed (byte* serviceName = service, accountName = account, passwordData = password)
        {
            if (item == IntPtr.Zero)
            {
                IntPtr added = IntPtr.Zero;
                int status = SecKeychainAddGenericPassword(IntPtr.Zero, (uint)service.Length, serviceName, (uint)account.Length, accountName, (uint)password.Length, passwordData, &added);
                if (added != IntPtr.Zero)
                {
                    CFRelease(added);
                }

                return Succeeded(status);
            }

            try
            {
                // The user name may have changed: the account is rewritten with the password.
                SecKeychainAttribute attribute = new() { Tag = AccountAttribute, Length = (uint)account.Length, Data = accountName };
                SecKeychainAttributeList attributes = new() { Count = 1, Attributes = &attribute };
                return Succeeded(SecKeychainItemModifyAttributesAndData(item, &attributes, (uint)password.Length, passwordData));
            }
            finally
            {
                CFRelease(item);
            }
        }
    }

    public bool Remove(string target)
    {
        IntPtr item = FindItem(GetService(target));
        if (item == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return Succeeded(SecKeychainItemDelete(item));
        }
        finally
        {
            CFRelease(item);
        }
    }

    /// <summary>The item of a service (to be released with <c>CFRelease</c>), or <see cref="IntPtr.Zero"/>; its password is not read.</summary>
    private static IntPtr FindItem(byte[] service)
    {
        IntPtr item = IntPtr.Zero;
        int status;
        fixed (byte* serviceName = service)
        {
            status = SecKeychainFindGenericPassword(IntPtr.Zero, (uint)service.Length, serviceName, 0, null, null, null, &item);
        }

        return Succeeded(status) ? item : IntPtr.Zero;
    }

    /// <summary>The account (the user name) of an item.</summary>
    private static string? GetAccount(IntPtr item)
    {
        uint tag = AccountAttribute;
        uint format = StringFormat;
        SecKeychainAttributeInfo info = new() { Count = 1, Tags = &tag, Formats = &format };
        SecKeychainAttributeList* attributes = null;
        if (!Succeeded(SecKeychainItemCopyAttributesAndData(item, &info, null, &attributes, null, null)) || attributes is null)
        {
            return null;
        }

        try
        {
            return attributes->Count > 0 && attributes->Attributes[0].Data is not null
                ? Encoding.UTF8.GetString((byte*)attributes->Attributes[0].Data, (int)attributes->Attributes[0].Length)
                : null;
        }
        finally
        {
            SecKeychainItemFreeAttributesAndData(attributes, null);
        }
    }

    private static byte[] GetService(string target)
        => string.IsNullOrWhiteSpace(target) ? throw new ArgumentNullException(nameof(target)) : Encoding.UTF8.GetBytes($"{ServicePrefix}{target}");

    /// <summary>Whether a call succeeded; the failures other than a missing item are traced.</summary>
    private static bool Succeeded(int status)
    {
        if (status != 0 && status != ErrSecItemNotFound)
        {
            Trace.WriteLine($"Keychain: error {status}");
        }

        return status == 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SecKeychainAttribute
    {
        public uint Tag;
        public uint Length;
        public void* Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SecKeychainAttributeList
    {
        public uint Count;
        public SecKeychainAttribute* Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SecKeychainAttributeInfo
    {
        public uint Count;
        public uint* Tags;
        public uint* Formats;
    }

    // The SecKeychain functions are deprecated in favor of SecItem, but they are still supported and need no CFDictionary.
    [LibraryImport(SecurityFramework)]
    private static partial int SecKeychainFindGenericPassword(IntPtr keychainOrArray, uint serviceNameLength, byte* serviceName, uint accountNameLength, byte* accountName, uint* passwordLength, void** passwordData, IntPtr* itemRef);

    [LibraryImport(SecurityFramework)]
    private static partial int SecKeychainAddGenericPassword(IntPtr keychain, uint serviceNameLength, byte* serviceName, uint accountNameLength, byte* accountName, uint passwordLength, void* passwordData, IntPtr* itemRef);

    [LibraryImport(SecurityFramework)]
    private static partial int SecKeychainItemModifyAttributesAndData(IntPtr itemRef, SecKeychainAttributeList* attrList, uint length, void* data);

    [LibraryImport(SecurityFramework)]
    private static partial int SecKeychainItemCopyAttributesAndData(IntPtr itemRef, SecKeychainAttributeInfo* info, uint* itemClass, SecKeychainAttributeList** attrList, uint* length, void** outData);

    [LibraryImport(SecurityFramework)]
    private static partial int SecKeychainItemFreeAttributesAndData(SecKeychainAttributeList* attrList, void* data);

    [LibraryImport(SecurityFramework)]
    private static partial int SecKeychainItemFreeContent(SecKeychainAttributeList* attrList, void* data);

    [LibraryImport(SecurityFramework)]
    private static partial int SecKeychainItemDelete(IntPtr itemRef);

    [LibraryImport(CoreFoundationFramework)]
    private static partial void CFRelease(IntPtr cf);
}
