using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BlastDomainUserPwd
{
    class win32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct NETRESOURCE

        {
            public int dwScope;

            public int dwType;

            public int dwDisplayType;

            public int dwUsage;

            [MarshalAs(UnmanagedType.LPWStr)]

            public string lpLocalName;

            [MarshalAs(UnmanagedType.LPWStr)]

            public string lpRemoteName;

            [MarshalAs(UnmanagedType.LPWStr)]

            public string lpComment;

            [MarshalAs(UnmanagedType.LPWStr)]

            public string lpProvider;

        }

        [DllImport("mpr.dll", CharSet = CharSet.Auto)]
        public static extern int WNetAddConnection2(ref NETRESOURCE netResource,string password, string username, uint flags);
        [DllImport("mpr.dll", CharSet = CharSet.Auto)]
        public static extern int WNetCancelConnection2(string sLocalName, uint iFlags, bool iForce);

    }
}
