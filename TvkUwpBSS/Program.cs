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

namespace TvkUwpBSS
{

    internal class Program
    {
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
        static string FluxusPath = "";
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
        static void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            try
            {
                if (e.NewEvent.Properties["ProcessName"].Value.ToString() == "Windows10Universal.exe")
                {
                    Thread.Sleep(5000);
                    string id = e.NewEvent.Properties["ProcessId"].Value.ToString();
                    Process process = Process.GetProcessById(int.Parse(id));
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
                    DllInject.Inject(process, FluxusPath);
                    Thread.Sleep(2000);
                }
            }
            catch (Exception)
            {

            }
        }
    
        static long GetTime()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        const string RAMPORT = "1412";
        const string RAMPASS = "sucvatruabithieunang";
        public static HttpClient HttpClient = new HttpClient();
        static Dictionary<string, int> FailJoin = new Dictionary<string, int>();
        static string JoinGame(string accname, string placeid, string jobid)
        {
            string url = "http://localhost:" + RAMPORT + "/LaunchAccount?Password=" + RAMPASS + "&Account=" + accname + "&PlaceId=" + placeid;
            if (!string.IsNullOrEmpty(jobid))
            {
                url += "&JobId=" + jobid;
            }
            return (HttpClient.GetAsync(url).Result.Content.ReadAsStringAsync().Result);
        }
        static string GetFluxusPath()
        {
            string[] folders = Directory.GetDirectories(@"C:\Program Files (x86)", "*", SearchOption.TopDirectoryOnly);
            foreach (var item in folders)
            {
                FileInfo fileInfo = new FileInfo(item);
                if (fileInfo.Name.Length == 64)
                {
                    if (File.Exists(item + "\\" + fileInfo.Name + ".dll"))
                    {
                        return item + "\\" + fileInfo.Name + ".dll";
                    }
                }
            }
            return "";
        }
        static void Main(string[] args)
        {
            AccountManger = Environment.CurrentDirectory;
            if (!File.Exists(Path.Combine(AccountManger, "RAMSettings.ini")))
            {
                Console.WriteLine("K ph la folder account manager huhu");
                Console.ReadKey();
                return;
            }
            Dictionary<string, string> Settings = new Dictionary<string, string>() {
                ["WebServerPort"] = RAMPORT,
                ["Password"] = RAMPASS,
                ["AllowLaunchAccount"] = "true",
                ["EveryRequestRequiresPassword"] = "true",
                ["AllowExternalConnections"] = "true",
                ["RelaunchDelay"] = "3600",
                ["LauncherDelayNumber"] = "3600",
                ["UsePresence"] = "true",
                ["LauncherDelay"] = "3600",
                ["EnableWebServer"] = "true",
                ["DevMode"] = "true"
            };
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
                        Console.WriteLine(splited[0] + ","+ splited[1]);
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
                File.WriteAllText(Path.Combine(AccountManger, "RAMSettings.ini"),OutputIni);
            }
            if (Killed)
            {
                Thread.Sleep(1000);
            }
            if (Process.GetProcessesByName("Roblox Account Manager").Length == 0)
            {
                Process.Start(Path.Combine(AccountManger, "Roblox Account Manager.exe"));
            }
            
            FluxusPath = GetFluxusPath();
            if (string.IsNullOrEmpty(FluxusPath))
            {
                Console.WriteLine("Fluxus path not found");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Rolox Account Manager Supporter - made by tvk1308");
            Console.Title = "Rolox Account Manager Supporter - tvk1308";
            WaitForProcess();
            string[] folders = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+@"\Packages", "*", SearchOption.TopDirectoryOnly);
            Dictionary<string, long> AccTimer = new Dictionary<string, long>();
            string InitScript = File.ReadAllText("Account Manager Supporter.lua");
            long lastJoin = 0;
            while (true)
            {
                try
                {
                    //if (GetTime() - lastDelete > 3600 * 2)
                    //{
                    //    Console.WriteLine("Deleting Cache");
                    //    Thread.Sleep(5000);
                    //    lastDelete = GetTime();
                    //    foreach (var item in Process.GetProcessesByName("Windows10Universal"))
                    //    {
                    //        item.Kill();
                    //    }
                    //    Thread.Sleep(1000);

                    //    foreach (var item in folders)
                    //    {
                    //        if (item.Contains("ROBLOXCORPORATION.ROBLOX"))
                    //        {
                    //            try
                    //            {
                    //                Directory.Delete(item + "\\LocalState", true);
                    //            }
                    //            catch (Exception e)
                    //            {
                    //            }
                    //        }
                    //    }
                    //    Console.WriteLine("Deleted");

                    //}
                    Thread.Sleep(1000);
                    foreach (Process process in Process.GetProcessesByName("cmd"))
                    {
                        process.Kill();
                    }
                    
                    dynamic accs = JsonConvert.DeserializeObject(File.ReadAllText(AccountManger + "\\AccountControlData.json"));
                    foreach (var item in folders)
                    {
                        if (item.Contains("ROBLOXCORPORATION.ROBLOX"))
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
                                                            if (GetTime() - AccTimer[accname] > 30 && GetTime() - lastJoin > 2)
                                                            {
                                                                long placeid = item2.PlaceId;
                                                                string jobid = item2.JobId;
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
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                
            }
        }
    }
}
