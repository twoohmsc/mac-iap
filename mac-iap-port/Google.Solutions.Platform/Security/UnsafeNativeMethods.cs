using System;
using System.Runtime.InteropServices;

namespace Google.Solutions.Platform.Security
{
    internal static class UnsafeNativeMethods
    {
        private const string SecurityLib = "/System/Library/Frameworks/Security.framework/Security";

        [DllImport(SecurityLib)]
        public static extern int SecItemAdd(IntPtr attributes, out IntPtr result);

        [DllImport(SecurityLib)]
        public static extern int SecItemCopyMatching(IntPtr query, out IntPtr result);

        [DllImport(SecurityLib)]
        public static extern int SecItemDelete(IntPtr query);

        [DllImport(SecurityLib)]
        public static extern int SecItemUpdate(IntPtr query, IntPtr attributesToUpdate);

        // keys
        public static IntPtr kSecClass;
        public static IntPtr kSecClassGenericPassword;
        public static IntPtr kSecAttrAccount;
        public static IntPtr kSecAttrService;
        public static IntPtr kSecAttrLabel;
        public static IntPtr kSecAttrDescription;
        public static IntPtr kSecAttrComment;
        public static IntPtr kSecValueData;
        public static IntPtr kSecReturnData;
        public static IntPtr kSecMatchLimit;
        public static IntPtr kSecMatchLimitOne;
        public static IntPtr kSecReturnAttributes;
        public static IntPtr kSecReturnRef;

        public static IntPtr kSecAttrAccessible;
        public static IntPtr kSecAttrAccessibleWhenUnlocked;

        // kSecClassKey
        public static IntPtr kSecClassKey;
        public static IntPtr kSecAttrKeyType;
        public static IntPtr kSecAttrKeyTypeRSA;
        public static IntPtr kSecAttrKeySizeInBits;
        public static IntPtr kSecAttrIsPermanent;
        public static IntPtr kSecAttrApplicationTag; // private tag
        public static IntPtr kSecPrivateKeyAttrs;
        public static IntPtr kSecPublicKeyAttrs;

        static UnsafeNativeMethods() 
        {
            LoadConstants();
        }

        private static void LoadConstants()
        {
            var handle = NativeLibrary.Load(SecurityLib);

            IntPtr Load(string name)
            {
                var ptr = NativeLibrary.GetExport(handle, name);
                return Marshal.ReadIntPtr(ptr);
            }

            kSecClass = Load("kSecClass");
            kSecClassGenericPassword = Load("kSecClassGenericPassword");
            kSecAttrAccount = Load("kSecAttrAccount");
            kSecAttrService = Load("kSecAttrService");
            kSecAttrLabel = Load("kSecAttrLabel");
            kSecAttrDescription = Load("kSecAttrDescription");
            kSecAttrComment = Load("kSecAttrComment");
            kSecValueData = Load("kSecValueData");
            kSecReturnData = Load("kSecReturnData");
            kSecMatchLimit = Load("kSecMatchLimit");
            kSecMatchLimitOne = Load("kSecMatchLimitOne");
            kSecReturnAttributes = Load("kSecReturnAttributes");
            kSecReturnRef = Load("kSecReturnRef");

            kSecAttrAccessible = Load("kSecAttrAccessible");
            kSecAttrAccessibleWhenUnlocked = Load("kSecAttrAccessibleWhenUnlocked");

            kSecClassKey = Load("kSecClassKey");
            kSecAttrKeyType = Load("kSecAttrKeyType");
            kSecAttrKeyTypeRSA = Load("kSecAttrKeyTypeRSA");
            kSecAttrKeySizeInBits = Load("kSecAttrKeySizeInBits");
            kSecAttrIsPermanent = Load("kSecAttrIsPermanent");
            kSecAttrApplicationTag = Load("kSecAttrApplicationTag");
            kSecPrivateKeyAttrs = Load("kSecPrivateKeyAttrs");
            kSecPublicKeyAttrs = Load("kSecPublicKeyAttrs");
        }
    }
}
