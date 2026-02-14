using System;
using System.Runtime.InteropServices;

namespace Google.Solutions.Platform.Interop
{
    internal static class CoreFoundation
    {
        private const string CoreFoundationLib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        [DllImport(CoreFoundationLib)]
        public static extern void CFRelease(IntPtr cf);

        [DllImport(CoreFoundationLib)]
        public static extern void CFRetain(IntPtr cf);

        [DllImport(CoreFoundationLib, CharSet = CharSet.Ansi)]
        public static extern IntPtr CFStringCreateWithCString(IntPtr alloc, string cStr, uint encoding);

        [DllImport(CoreFoundationLib)]
        public static extern int CFStringGetLength(IntPtr theString);

        [DllImport(CoreFoundationLib)]
        public static extern bool CFStringGetCString(IntPtr theString, byte[] buffer, int bufferSize, uint encoding);

        public const uint kCFStringEncodingUTF8 = 0x08000100;

        public static IntPtr ToCFString(string s)
        {
            if (s == null) return IntPtr.Zero;
            return CFStringCreateWithCString(IntPtr.Zero, s, kCFStringEncodingUTF8);
        }

        public static string? FromCFString(IntPtr cfString)
        {
            if (cfString == IntPtr.Zero) return null;

            int length = CFStringGetLength(cfString);
            int bufferSize = (length + 1) * 3; // Max UTF-8 size per char is 3? 4 actually.
            byte[] buffer = new byte[bufferSize];

            if (CFStringGetCString(cfString, buffer, bufferSize, kCFStringEncodingUTF8))
            {
                // Find null terminator
                int nullIndex = Array.IndexOf(buffer, (byte)0);
                if (nullIndex >= 0)
                {
                    return System.Text.Encoding.UTF8.GetString(buffer, 0, nullIndex);
                }
                return System.Text.Encoding.UTF8.GetString(buffer);
            }
            return null;
        }
        
        // Dictionary
        [DllImport(CoreFoundationLib)]
        public static extern IntPtr CFDictionaryCreate(
            IntPtr allocator,
            IntPtr[] keys,
            IntPtr[] values,
            int numValues,
            IntPtr keyCallBacks,
            IntPtr valueCallBacks);

        public static IntPtr kCFTypeDictionaryKeyCallBacks;
        public static IntPtr kCFTypeDictionaryValueCallBacks;
        
        // Boolean
        public static IntPtr kCFBooleanTrue;
        public static IntPtr kCFBooleanFalse;
        
        static CoreFoundation()
        {
            var handle = NativeLibrary.Load(CoreFoundationLib);
            
            IntPtr Load(string name)
            {
                var ptr = NativeLibrary.GetExport(handle, name);
                return Marshal.ReadIntPtr(ptr); // Constants are pointers to data
            }
            
            kCFTypeDictionaryKeyCallBacks = NativeLibrary.GetExport(handle, "kCFTypeDictionaryKeyCallBacks"); // This is a struct, not a pointer to a pointer? verification needed.
            kCFTypeDictionaryValueCallBacks = NativeLibrary.GetExport(handle, "kCFTypeDictionaryValueCallBacks");

            kCFBooleanTrue = Load("kCFBooleanTrue");
            kCFBooleanFalse = Load("kCFBooleanFalse");
        }

        // Data
        [DllImport(CoreFoundationLib)]
        public static extern IntPtr CFDataCreate(IntPtr allocator, byte[] bytes, int length);
        
        [DllImport(CoreFoundationLib)]
        public static extern IntPtr CFDataGetBytePtr(IntPtr theData);
        
        [DllImport(CoreFoundationLib)]
        public static extern int CFDataGetLength(IntPtr theData);
        
        public static byte[]? FromCFData(IntPtr cfData)
        {
            if (cfData == IntPtr.Zero) return null;
            
            int length = CFDataGetLength(cfData);
            IntPtr ptr = CFDataGetBytePtr(cfData);
            
            byte[] buffer = new byte[length];
            Marshal.Copy(ptr, buffer, 0, length);
            return buffer;
        }

        // Error
        [DllImport(CoreFoundationLib)]
        public static extern IntPtr CFErrorCopyDescription(IntPtr err);
    }
}
