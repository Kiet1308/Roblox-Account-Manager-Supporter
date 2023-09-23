using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AccountManagerSupporter
{

    internal class Program
    {
        const string VERSION = "v1.1";
        static string AccountManger = "";
        static void WaitForProcess()
        {
            ManagementEventWatcher startWatch = new ManagementEventWatcher(
              new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            startWatch.EventArrived
                                += new EventArrivedEventHandler(OnProcessStarted);
            startWatch.Start();
        }
        static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random();
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                int index = random.Next(chars.Length);
                stringBuilder.Append(chars[index]);
            }

            return stringBuilder.ToString();
        }
        static string GetRobloxAccByProcess(Process process)
        {
            try
            {
                return new FileInfo(process.MainModule.FileName.Substring(0, process.MainModule.FileName.Length - "Windows10Universal.exe".Length - 1)).Name;
            }
            catch (Exception)
            {

                return "";
            }
        }

        private static object processLock = new object(); 
        static string ExecuteCommand(string command)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "cmd.exe";
            processStartInfo.Arguments = command;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;
            Process process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();
            string text = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return text;
        }
        static bool IsRobloxTabInGame(Process process) 
        {
            string Command = "/C netstat -ano | find \"" + process.Id.ToString()+"\"";
            string output = ExecuteCommand(Command);
            return output.Contains("UDP");
        }
        static void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            if (!Commands["AutoInject"])
            {
                return;
            }
            lock (processLock)
            {
                try
                {
                    if (e.NewEvent.Properties["ProcessName"].Value.ToString() == "Windows10Universal.exe")
                    {
                        Thread.Sleep(1000);
                        
                        string id = e.NewEvent.Properties["ProcessId"].Value.ToString();

                        Process process = Process.GetProcessById(int.Parse(id));

                        string FluxusPath = GetFluxusPath(process);
                        Thread thread = new Thread(() => {
                            Thread.Sleep(1000);
                            string AccountName = GetRobloxAccByProcess(process);
                            if (!FailJoin.ContainsKey(AccountName))
                            {
                                FailJoin.Add(AccountName, 0);
                            }
                            else
                            {
                                FailJoin[AccountName] = 0;
                            }
                            Thread.Sleep(1000);

                            if (!string.IsNullOrEmpty(FluxusPath))
                            {
                                DllInject.Inject(process, FluxusPath);
                            }
                            Thread.Sleep(2000);
                        });
                        thread.IsBackground = true;
                        thread.Start();
                    }
                }
                catch (Exception)
                {

                }
            }
        }
        static async void HttpServer()
        {
            // Mảng chứa địa chỉ Http lắng nghe
            // http =  giao thức http, * = ip bất kỳ, 8080 = cổng lắng nghe
            string[] prefixes = new string[] { "http://localhost:4953/" };

            HttpListener listener = new HttpListener();

            if (!HttpListener.IsSupported) throw new Exception("Hệ thống hỗ trợ HttpListener.");

            if (prefixes == null || prefixes.Length == 0) throw new ArgumentException("prefixes");

            foreach (string s in prefixes)
            {
                listener.Prefixes.Add(s);
            }

            Console.WriteLine("Server start ...");

            // Http bắt đầu lắng nghe truy vấn gửi đến
            listener.Start();

            // Vòng lặp chấp nhận và xử lý các client kết nối
            do
            {
                // Chấp nhận khi có cliet kết nối đế
                HttpListenerContext context = await listener.GetContextAsync();
                await Console.Out.WriteLineAsync(context.Request.Url.LocalPath);
                // ....
                // Xử lý context - đọc  thông tin request,  ghi thông tin response
                // ... ví dụ như sau:parameter 
                if (context.Request.Url.LocalPath == "/JoinServer")
                {
                   // JoinGame(context.Request.Url.Query);
                }
                var response = context.Response;                                        // lấy HttpListenerResponse
                var outputstream = response.OutputStream;                               // lấy Stream lưu nội dung gửi cho client

                context.Response.Headers.Add("content-type", "text/html");              // thiết lập respone header
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes("Hello world!");     // dữ liệu content
                response.ContentLength64 = buffer.Length;
                await outputstream.WriteAsync(buffer, 0, buffer.Length);                  // viết content ra stream
                outputstream.Close();                                                   // Đóng stream (gửi về cho cliet)

            }
            while (listener.IsListening);
        }
        static long GetTime()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        const string RAMPORT = "1412";
        const string RAMPASS = "sucvatruabithieunang";

        public static HttpClient HttpClient = new HttpClient();
        static Dictionary<string, int> FailJoin = new Dictionary<string, int>();
        static string HttpGet(string url) {
            return (HttpClient.GetAsync(url).Result.Content.ReadAsStringAsync().Result);
        }
        static string JoinGame(string accname, string placeid, string jobid)
        {
            string url = "http://localhost:" + RAMPORT + "/LaunchAccount?Password=" + RAMPASS + "&Account=" + accname + "&PlaceId=" + placeid;
            if (!string.IsNullOrEmpty(jobid))
            {
                url += "&JobId=" + jobid;
            }
            return (HttpClient.GetAsync(url).Result.Content.ReadAsStringAsync().Result);
        }
        static Dictionary<string,string> CachedHash = new Dictionary<string,string>();
        static string GetFluxusPath(Process process)
        {
            string FluxusDlls = "C:\\Program Files (x86)";

            string file;
            if (CachedHash.ContainsKey(process.MainModule.FileName))
            {
                file = CachedHash[process.MainModule.FileName];
            }
            else
            {
                file = CalculateSHA384(process.MainModule.FileName);
                CachedHash.Add(process.MainModule.FileName, file);
            }
            if (File.Exists(Path.Combine(FluxusDlls,file+".dll")))
            {
                return Path.Combine(FluxusDlls, file + ".dll");
            }
            string Url = "https://flux.li/windows/external/get_dll_hash.php?hash="+ file;
            string DownloadUrl = HttpGet(Url);
            if (!string.IsNullOrEmpty(DownloadUrl))
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(DownloadUrl, file+".dll");
                    Thread.Sleep(100);
                    var fs = File.GetAccessControl(file + ".dll");
                    fs.SetAccessRuleProtection(false, false);
                    File.Move(file + ".dll", Path.Combine(FluxusDlls, file + ".dll"));
                    File.SetAccessControl(Path.Combine(FluxusDlls, file + ".dll"), fs);
                    Thread.Sleep(100);
                    return Path.Combine(FluxusDlls, file + ".dll");
                }
            }
            return "";
        }
        static string CalculateSHA384(string filePath)
        {
            using (var sha384 = SHA384.Create())
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] hashBytes = sha384.ComputeHash(fileStream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
        }
        static string[] GetRobloxAccsAppData() 
        {
            List<string> folder = new List<string>();
            string[] folders = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages", "*", SearchOption.TopDirectoryOnly);
            foreach (var item in folders)
            {
                if (item.Contains("ROBLOXCORPORATION.ROBLOX"))
                {
                    folder.Add(item);
                }
            }
            return folder.ToArray();
        }
        static Dictionary<string, bool> LoadConfig(Dictionary<string, bool> Config, string filename)
        {
            Dictionary<string, bool> ReturnedConfig;
            try
            {
                ReturnedConfig = JsonConvert.DeserializeObject<Dictionary<string, bool>>(File.ReadAllText(filename));
            }
            catch (Exception)
            {
                return Config;
            }
            if (ReturnedConfig == null)
            {
                return Config;
            }
            foreach (var item in Config)
            {
                if (!ReturnedConfig.ContainsKey(item.Key))
                {
                    ReturnedConfig.Add(item.Key, item.Value);
                }
            }
            return ReturnedConfig;
        }
        static void SaveConfig(Dictionary<string, bool> Config, string filename)
        {
            File.WriteAllText(filename, JsonConvert.SerializeObject(Config, Formatting.Indented));
        }
        static void DisplayHelp(Dictionary<string, bool> Commands)
        {
            Console.WriteLine("--------------------------------------------------------------");
            Console.WriteLine("|                     Command-Line Help                       |");
            Console.WriteLine("--------------------------------------------------------------");
            Console.WriteLine("Usage: [command] [value]\n");

            Console.WriteLine("Commands:");
            Console.WriteLine("  - status      : Display the current status of all commands.");
            Console.WriteLine("  - [command]   : Set the value of a specific command.");
            Console.WriteLine("  - UpdateAllRobloxVersionToOriginal : Set all Roblox instances to the original version.");
            Console.WriteLine("  - donate : Get Donate Info.\n");
            Console.WriteLine("Recommended Settings for Minimizing Roblox Crashes:");
            Console.WriteLine("  - AutoInject     : true");
            Console.WriteLine("  - InjectMainTab  : true");
            //Console.WriteLine("  - Use custom Join function: AccountManagerSupporter.JoinServer(UserName, PlaceId, JobId)");
            Console.WriteLine("  - Use newest Roblox Version");
            Console.WriteLine("  - Disable auto inject in Fluxus.\n");

            Console.WriteLine("  - Set Account Manager Settings:");
            Console.WriteLine("    - Enable Web Server: Enable");
            Console.WriteLine("    - Every Request Requires Password: Enable");
            Console.WriteLine("    - Port: 1412");
            Console.WriteLine("    - Allow LaunchAccountMethod: Enable");
            Console.WriteLine("    - WebServer Password: sucvatruabithieunang\n");
            Console.WriteLine("Available Commands:");
            foreach (var item in Commands)
            {
                Console.WriteLine("  - " + item.Key.PadRight(35) + " : [true | false]");
            }
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  - status               : Display the status of all commands.");
            Console.WriteLine("  - AutoDeleteRobloxCache true : Set the 'AutoDeleteRobloxCache' command to 'true'.");
            Console.WriteLine("  - AutoDeleteRobloxCache false : Set the 'AutoDeleteRobloxCache' command to 'false'.");
            Console.WriteLine("  - UpdateAllRobloxVersionToOriginal : Set all Roblox instances to the original version.\n");
            Console.WriteLine("Notes:");
            Console.WriteLine("  - Use 'status' to view the current state of all available commands.");
            Console.WriteLine("  - To change the state of a command, specify the command name and 'true' or 'false'.");
            Console.WriteLine("  - Invalid commands or values will result in error messages.");
            Console.WriteLine("  - Dont use this tool with Nexus");
            Console.WriteLine("  - Changes are automatically saved to the configuration file.\n");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Important: Disable 'Auto Attach' in Fluxus settings when enabling 'Auto Inject' in this tool.\n");
            Console.ResetColor();
        }
        static Dictionary<string, bool> Commands = new Dictionary<string, bool>()
        {
            ["AutoDeleteRobloxCache"] = false,
            ["AutoInject"] = true,
            ["InjectMainTab"] = false,
        };
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        private const int WM_CLOSE = 0x0010;

        [DllImport("user32", CharSet = CharSet.Unicode)]
        private static extern
         IntPtr SendMessage(
                 IntPtr handle,
                 int Msg,
                 IntPtr wParam,
                 IntPtr lParam
          );
        static void OpenAccountTab(string AccountFolder)
        {
            Process.Start("explorer.exe", "shell:appsFolder\\"+ AccountFolder+"!App");
        }
        static void Main(string[] args)
        {
            string Varriable = "";
            AccountManger = Environment.CurrentDirectory;
            Console.Title = "Roblox Account Manager Supporter - tvk1308 " + VERSION;
            if (!File.Exists(Path.Combine(AccountManger, "RAMSettings.ini")))
            {
                Console.WriteLine("Please put this program in account manager folder");
                Console.ReadKey();
                return;
            }
            Dictionary<string, string> Settings = new Dictionary<string, string>() {
                ["RelaunchDelay"] = "3600",
                ["LauncherDelayNumber"] = "3600",
                ["UsePresence"] = "true",
                ["LauncherDelay"] = "3600",
            };
            Console.WriteLine("Roblox Account Manager Supporter - made by tvk1308");
            Console.WriteLine("This tool is designed for use with Fluxus exploit only");
            Console.WriteLine("Type help to see all command and best setting for no crash");
            Thread thread = new Thread(() => {
                //HttpServer();
            });
            thread.IsBackground = true;
            thread.Start();
            WaitForProcess();
            string[] folders = GetRobloxAccsAppData();
            Dictionary<string, long> AccTimer = new Dictionary<string, long>();
            string InitScript = File.ReadAllText("Account Manager Supporter.lua");
            long lastJoin = 0;
            long LastGetAcc = 0;
            long LastCheckINI = 0;
            long LastDelete = 0;
            long LastCheckMainTab = 0;
            string ConfigFileName = "AccountManagerSupporterConfig.json";
            
            Commands = LoadConfig(Commands, ConfigFileName);
            Thread GetCommand = new Thread(() => { 
                while (true)
                {
                    string InputCommand = Console.ReadLine();
                    string[] Splited = InputCommand.Split(' ');
                    string Command = Splited[0];
                    switch (Command)
                    {
                        case "help":
                            DisplayHelp(Commands);
                            break;
                        case "status":
                            foreach (var item in Commands)
                            {
                                Console.WriteLine(item.Key + " " + item.Value);
                            }
                            break;
                        case "donate":
                            Console.WriteLine("Momo: 0921798186");
                            Console.WriteLine("MB BANK: 0921798186 TRINH VUONG KIET");
                            Console.WriteLine("Paypal: kiettrinhvuong@gmail.com");
                            Console.WriteLine("Thesieure: trinhkietvuong@gmail.com");
                            break;
                        case "UpdateAllRobloxVersionToOriginal":
                            string OriginalPath = "";
                            foreach (var item in Directory.GetDirectories("C:\\Program Files\\WindowsApps"))
                            {
                                if (item.Contains("ROBLOXCORPORATION.ROBLOX") && File.Exists(Path.Combine(item, "Windows10Universal.exe")))
                                {
                                    OriginalPath = item;
                                }
                            }
                            if (!string.IsNullOrEmpty(OriginalPath))
                            {
                                Console.WriteLine(Path.Combine(AccountManger, "UWP_Instances", "Windows10Universal.exe"));
                                foreach (var item in Directory.GetDirectories(Path.Combine(AccountManger, "UWP_Instances")))
                                {
                                    if (File.Exists(Path.Combine(item, "Windows10Universal.exe")))
                                    {
                                        File.Copy(Path.Combine(item, "Windows10Universal.exe"), Path.Combine(AccountManger, "UWP_Instances", "Windows10Universal.exe"), true);
                                        Console.WriteLine("Updated: " + (new FileInfo(item)).Name);
                                    }
                                }
                                Console.WriteLine("Updated all instance");
                            }
                            else
                            {
                                Console.WriteLine("Could not find original version");
                            }
                            break;
                        default:
                            if (Commands.ContainsKey(Command))
                            {
                                if (Splited.Length < 2 || !(Splited[1] == "true" || Splited[1] == "false"))
                                {
                                    Console.WriteLine("Invalid Value");
                                }
                                else
                                {
                                    bool Val = bool.Parse(Splited[1]);
                                    Commands[Splited[0]] = Val;
                                    Console.WriteLine("Setted " + Splited[0] + ": " + Splited[1]);
                                    SaveConfig(Commands, ConfigFileName);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Invalid Command");
                            }
                            break;
                    }                
                }
            });
            GetCommand.IsBackground = true;
            GetCommand.Start();
            while (true)
            {
                try
                {
                    if (GetTime() - LastCheckMainTab > 30)
                    {
                        foreach (var item in Process.GetProcessesByName("Windows10Universal"))
                        {
                            if (DateTime.Now - item.StartTime > TimeSpan.FromSeconds(60))
                            {
                                if (!IsRobloxTabInGame(item))
                                {
                                    item.Kill();
                                }
                            }
                        }
                        LastCheckMainTab = GetTime();
                    }
                }
                catch (Exception)
                {

                   
                }
                try
                {
                    var handle = FindWindow(null, "Fluxus");
                    if (handle != IntPtr.Zero)
                    {
                        SendMessage(handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                catch (Exception)
                {

                    throw;
                }
                try
                {
                    if (GetTime()- LastCheckINI > 20)
                    {
                        string INI = File.ReadAllText(Path.Combine(AccountManger, "RAMSettings.ini"));
                        string OutputIni = "";
                        bool Changed = false;
                        foreach (string text in INI.Split('\n'))
                        {
                            string outputtext = text;
                            if (!text.Contains(("!")))
                            {
                                string[] splited = text.Split('=');
                                if (splited.Length == 2)
                                {
                                    splited[1] = splited[1].Replace(((char)(13)).ToString(), "");
                                    if (Settings.ContainsKey(splited[0]) && Settings[splited[0]] != splited[1])
                                    {
                                        outputtext = splited[0] + "=" + Settings[splited[0]];
                                        Changed = true;
                                    }
                                }
                            }
                            OutputIni = OutputIni + outputtext + "\n";
                        }
                        bool Killed = false;
                        if (Changed)
                        {
                            foreach (var proc in Process.GetProcessesByName("Roblox Account Manager"))
                            {
                                proc.Kill();
                                Killed = true;
                            }
                            Thread.Sleep(1000);
                            File.WriteAllText(Path.Combine(AccountManger, "RAMSettings.ini"), OutputIni);
                        }
                        if (Killed)
                        {
                            Thread.Sleep(1000);
                        }
                        LastCheckINI = GetTime();
                    }
                    if (Process.GetProcessesByName("Roblox Account Manager").Length == 0)
                    {
                        Process.Start(Path.Combine(AccountManger, "Roblox Account Manager.exe"));
                        Thread.Sleep(1000);
                    }
                    if (GetTime()- LastGetAcc > 10)
                    {
                        folders = GetRobloxAccsAppData();
                       
                        LastGetAcc = GetTime();
                    }
                    if (GetTime() - LastDelete > 3600 * 2)
                    {
                        if (Commands["AutoDeleteRobloxCache"])
                        {
                            Console.WriteLine("Deleting Cache");
                            Thread.Sleep(5000);
                            LastDelete = GetTime();
                            foreach (var item in Process.GetProcessesByName("Windows10Universal"))
                            {
                                item.Kill();
                            }
                            Thread.Sleep(1000);

                            foreach (var item in folders)
                            {
                                if (item.Contains("ROBLOXCORPORATION.ROBLOX"))
                                {
                                    try
                                    {
                                        Directory.Delete(item + "\\LocalState", true);
                                    }
                                    catch (Exception e)
                                    {
                                    }
                                }
                            }
                            Console.WriteLine("Deleted");
                        }
                    }
                    Thread.Sleep(1000);
                    foreach (Process process in Process.GetProcessesByName("cmd"))
                    {
                        process.Kill();
                    }
                    if (File.Exists(AccountManger + "\\AccountControlData.json"))
                    {
                        dynamic accs = JsonConvert.DeserializeObject(File.ReadAllText(AccountManger + "\\AccountControlData.json"));
                        foreach (var item in folders)
                        {
                            if (true)
                            {
                                string[] split = item.Split('.');
                                if (split.Length == 3)
                                {
                                    string accname = item.Split('.')[2].Split('_')[0];
                                    accname = accname.Replace("-", "_");
                                    if (Directory.Exists(AccountManger + "\\UWP_Instances" + @"\" + accname))
                                    {
                                        try
                                        {
                                            if (!Directory.Exists((item + "\\AC")))
                                            {
                                                Directory.CreateDirectory((item + "\\AC"));
                                            }
                                            if (!Directory.Exists((item + "\\AC\\autoexec")))
                                            {
                                                Directory.CreateDirectory((item + "\\AC\\autoexec"));
                                            }
                                            if (File.Exists(item + "\\AC\\autoexec\\Nexus.lua"))
                                            {
                                                File.Delete(item + "\\AC\\autoexec\\Nexus.lua");
                                            }
                                            string path = item + "\\AC\\autoexec\\Account Manager Supporter.lua";
                                            File.WriteAllText(path, InitScript);
                                        }
                                        catch (Exception e)
                                        {
                                        }
                                        try
                                        {
                                            string pathh = item + "\\AC\\workspace\\CurrentTime.txt";
                                            File.WriteAllText(pathh, GetTime().ToString());
                                        }
                                        catch (Exception e)
                                        {
                                        }
                                        foreach (dynamic item2 in accs)
                                        {
                                            if (item2.Username == accname)
                                            {
                                                try
                                                {
                                                    if (!Directory.Exists((item + "\\AC")))
                                                    {
                                                        Directory.CreateDirectory((item + "\\AC"));
                                                    }
                                                    if (!Directory.Exists((item + "\\AC\\workspace")))
                                                    {
                                                        Directory.CreateDirectory((item + "\\AC\\workspace"));
                                                    }
                                                    string pathh = item + "\\AC\\workspace\\Auto Execute.txt";
                                                    string AutoExecute = item2.AutoExecute;
                                                    File.WriteAllText(pathh, AutoExecute);
                                                }
                                                catch (Exception e)
                                                {

                                                }
                                            }
                                            if (item2.Username == accname && item2.AutoRelaunch == true)
                                            {
                                                if (!AccTimer.ContainsKey(accname))
                                                {
                                                    AccTimer.Add(accname, GetTime());
                                                }
                                                else
                                                {
                                                    string path = item + "\\AC\\workspace\\Account Manager Supporter.txt";
                                                    try
                                                    {
                                                        if (File.Exists(path))
                                                        {
                                                            long LastUpdate;
                                                            if (long.TryParse(File.ReadAllText(path), out LastUpdate))
                                                            {
                                                                AccTimer[accname] = LastUpdate;
                                                                if (FindWindow(null, "Roblox " + accname) == IntPtr.Zero || GetTime() - AccTimer[accname] > 40 && GetTime() - lastJoin > 2)
                                                                {
                                                                    long placeid = item2.PlaceId;
                                                                    string jobid = item2.JobId;
                                                                    if (FindWindow(null, "Roblox " + accname) == IntPtr.Zero && Commands["InjectMainTab"])
                                                                    {
                                                                        FileInfo fileInfo = new FileInfo(item);
                                                                        OpenAccountTab(fileInfo.Name);
                                                                        Thread.Sleep(5000);
                                                                    }
                                                                    if (JoinGame(accname, placeid.ToString(), jobid).Contains("Launched"))
                                                                    {
                                                                        lastJoin = GetTime();
                                                                    }
                                                                    AccTimer[accname] = GetTime() + 10;
                                                                    File.WriteAllText(path, AccTimer[accname].ToString());
                                                                    if (FailJoin.ContainsKey(accname))
                                                                    {
                                                                        if (FailJoin[accname] > 3)
                                                                        {
                                                                            foreach (var proc in Process.GetProcessesByName("Windows10Universal"))
                                                                            {
                                                                                if (GetRobloxAccByProcess(proc) == accname)
                                                                                {
                                                                                    proc.Kill();
                                                                                    FailJoin[accname] = 0;
                                                                                    AccTimer[accname] = 0;
                                                                                }
                                                                            }
                                                                        }
                                                                        FailJoin[accname]++;
                                                                    }
                                                                    else
                                                                    {
                                                                        FailJoin.Add(accname, 0);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            File.WriteAllText(path, "0");
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                
            }
        }
    }
}
