using System;
using System.Runtime.InteropServices;

namespace Google.Solutions.Ssh.Native
{
    public enum LIBSSH2_CHANNEL_EXTENDED_DATA : Int32
    {
        NORMAL = 0,
        IGNORE = 1,
        MERGE = 2
    }

    public enum LIBSSH2_SFTP_ATTR : uint
    {
        SIZE = 0x00000001,
        UIDGID = 0x00000002,
        PERMISSIONS = 0x00000004,
        ACMODTIME = 0x00000008,
        EXTENDED = 0x80000000,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LIBSSH2_SFTP_ATTRIBUTES
    {
        /// <summary>
        /// If flags & ATTR_* bit is set, then the value in this struct is
        /// meaningful. Otherwise it should be ignored.
        /// </summary>
        public LIBSSH2_SFTP_ATTR flags;
        internal ulong filesize;
        internal uint uid;
        internal uint gid;
        internal FilePermissions permissions;
        internal uint atime;
        internal uint mtime;
    }
}
