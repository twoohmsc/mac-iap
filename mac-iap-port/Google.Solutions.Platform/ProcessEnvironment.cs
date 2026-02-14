using System;
using System.Runtime.InteropServices;

namespace Google.Solutions.Platform
{
    public static class ProcessEnvironment
    {
        private static Architecture ToPlatformArchitecture(System.Runtime.InteropServices.Architecture arch)
        {
            switch (arch)
            {
                case System.Runtime.InteropServices.Architecture.X86: return Architecture.X86;
                case System.Runtime.InteropServices.Architecture.X64: return Architecture.X64;
                case System.Runtime.InteropServices.Architecture.Arm64: return Architecture.Arm64;
                default: return Architecture.Unknown;
            }
        }

        public static Architecture NativeArchitecture => ToPlatformArchitecture(RuntimeInformation.OSArchitecture);
        public static Architecture ProcessArchitecture => ToPlatformArchitecture(RuntimeInformation.ProcessArchitecture);
    }
}
