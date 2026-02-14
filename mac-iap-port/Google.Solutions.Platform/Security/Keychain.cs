using System;
using System.Runtime.InteropServices;
using Google.Solutions.Platform.Interop;

namespace Google.Solutions.Platform.Security
{
    public class Keychain
    {
        public void AddGenericPassword(string service, string account, byte[] password)
        {
            var keys = new IntPtr[]
            {
                UnsafeNativeMethods.kSecClass,
                UnsafeNativeMethods.kSecAttrService,
                UnsafeNativeMethods.kSecAttrAccount,
                UnsafeNativeMethods.kSecValueData,
                UnsafeNativeMethods.kSecAttrAccessible
            };

            var serviceRef = CoreFoundation.ToCFString(service);
            var accountRef = CoreFoundation.ToCFString(account);
            var passwordRef = CoreFoundation.CFDataCreate(IntPtr.Zero, password, password.Length);
            
            try
            {
                var values = new IntPtr[]
                {
                    UnsafeNativeMethods.kSecClassGenericPassword,
                    serviceRef,
                    accountRef,
                    passwordRef,
                    UnsafeNativeMethods.kSecAttrAccessibleWhenUnlocked
                };

                var attributes = CoreFoundation.CFDictionaryCreate(
                    IntPtr.Zero,
                    keys,
                    values,
                    keys.Length,
                    CoreFoundation.kCFTypeDictionaryKeyCallBacks,
                    CoreFoundation.kCFTypeDictionaryValueCallBacks);

                if (attributes == IntPtr.Zero)
                {
                    throw new OutOfMemoryException("Failed to create CFDictionary");
                }

                try
                {
                    var status = UnsafeNativeMethods.SecItemAdd(attributes, out var result);
                    if (status != 0)
                    {
                        throw new Exception($"SecItemAdd failed with status {status}");
                    }
                }
                finally
                {
                    CoreFoundation.CFRelease(attributes);
                }
            }
            finally
            {
                if (serviceRef != IntPtr.Zero) CoreFoundation.CFRelease(serviceRef);
                if (accountRef != IntPtr.Zero) CoreFoundation.CFRelease(accountRef);
                if (passwordRef != IntPtr.Zero) CoreFoundation.CFRelease(passwordRef);
            }
        }

        public byte[]? FindGenericPassword(string service, string account)
        {
            var keys = new IntPtr[]
            {
                UnsafeNativeMethods.kSecClass,
                UnsafeNativeMethods.kSecAttrService,
                UnsafeNativeMethods.kSecAttrAccount,
                UnsafeNativeMethods.kSecReturnData,
                UnsafeNativeMethods.kSecMatchLimit
            };

            var serviceRef = CoreFoundation.ToCFString(service);
            var accountRef = CoreFoundation.ToCFString(account);
            
            try
            {
                var values = new IntPtr[]
                {
                    UnsafeNativeMethods.kSecClassGenericPassword,
                    serviceRef,
                    accountRef,
                    CoreFoundation.kCFBooleanTrue,
                    UnsafeNativeMethods.kSecMatchLimitOne
                };

                var query = CoreFoundation.CFDictionaryCreate(
                    IntPtr.Zero,
                    keys,
                    values,
                    keys.Length,
                    CoreFoundation.kCFTypeDictionaryKeyCallBacks,
                    CoreFoundation.kCFTypeDictionaryValueCallBacks);

                try
                {
                    var status = UnsafeNativeMethods.SecItemCopyMatching(query, out var result);
                    if (status == -25300) // errSecItemNotFound
                    {
                        return null;
                    }
                    else if (status != 0)
                    {
                        throw new Exception($"SecItemCopyMatching failed with status {status}");
                    }

                    try
                    {
                        return CoreFoundation.FromCFData(result);
                    }
                    finally
                    {
                        CoreFoundation.CFRelease(result);
                    }
                }
                finally
                {
                    CoreFoundation.CFRelease(query);
                }
            }
            finally
            {
                 if (serviceRef != IntPtr.Zero) CoreFoundation.CFRelease(serviceRef);
                 if (accountRef != IntPtr.Zero) CoreFoundation.CFRelease(accountRef);
            }
        }

        public void DeleteGenericPassword(string service, string account)
        {
            var keys = new IntPtr[]
            {
                UnsafeNativeMethods.kSecClass,
                UnsafeNativeMethods.kSecAttrService,
                UnsafeNativeMethods.kSecAttrAccount
            };

            var serviceRef = CoreFoundation.ToCFString(service);
            var accountRef = CoreFoundation.ToCFString(account);
            
            try
            {
                var values = new IntPtr[]
                {
                    UnsafeNativeMethods.kSecClassGenericPassword,
                    serviceRef,
                    accountRef
                };

                var query = CoreFoundation.CFDictionaryCreate(
                    IntPtr.Zero,
                    keys,
                    values,
                    keys.Length,
                    CoreFoundation.kCFTypeDictionaryKeyCallBacks,
                    CoreFoundation.kCFTypeDictionaryValueCallBacks);

                try
                {
                    var status = UnsafeNativeMethods.SecItemDelete(query);
                    if (status != 0 && status != -25300) // Ignore ItemNotFound
                    {
                         throw new Exception($"SecItemDelete failed with status {status}");
                    }
                }
                finally
                {
                    CoreFoundation.CFRelease(query);
                }
            }
            finally
            {
                 if (serviceRef != IntPtr.Zero) CoreFoundation.CFRelease(serviceRef);
                 if (accountRef != IntPtr.Zero) CoreFoundation.CFRelease(accountRef);
            }
        }
    }
}
