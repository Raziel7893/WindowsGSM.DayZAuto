using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.Functions;
using System.Text;

namespace WindowsGSM.Plugins
{
    //https://www.reddit.com/r/dayz/comments/afad51/automatically_update_and_sync_your_steam_workshop/
    public class DayZAuto : SteamCMDAgent
    {       
        // - Plugin Details
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.DayZAuto", // WindowsGSM.XXXX
            author = "raziel7893",
            description = "WindowsGSM plugin for supporting DayZ Dedicated Server with automatic ModUpdates",
            version = "1.0.0",
            url = "https://github.com/raziel7893/WindowsGSM.DayZAuto", // Github repository link (Best practice)
            color = "#34c9eb" // Color Hex
        };

        private readonly Functions.ServerConfig _serverData;
        public string Error, Notice;

        // - Settings properties for SteamCMD installer
        public override bool loginAnonymous => false;
        public override string AppId => "223350"; // Game server appId Steam
        public override string StartPath => "DayZServer_x64.exe"; // Game server start path


        public string FullName = "DayZ Dedicated Server with ModAutoUpdate";
        public bool AllowsEmbedConsole = true;
        public int PortIncrements = 1;
        public dynamic QueryMethod = new GameServer.Query.A2S();

        public string Port = "2302";
        public string QueryPort = "27016";
        public string Defaultmap = "DayZOffline.chernarusplus";
        public string Maxplayers = "60";
        public string Additional = "-config=serverDZ.cfg -doLogs -adminLog -netLog";

        public DayZAuto(Functions.ServerConfig serverData) : base(serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            //Download serverDZ.cfg
            string configPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "serverDZ.cfg");
            if (await Functions.Github.DownloadGameServerConfig(configPath, FullName))
            {
                string configText = File.ReadAllText(configPath);
                configText = configText.Replace("{{hostname}}", _serverData.ServerName);
                configText = configText.Replace("{{maxplayers}}", Maxplayers);
                File.WriteAllText(configPath, configText);
            }
        }


        public async Task<Process> Start()
        {
            // Use DZSALModServer.exe if the exe exist, otherwise use original
            string dzsaPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "DZSALModServer.exe");
            if (File.Exists(dzsaPath))
            {
                StartPath = "DZSALModServer.exe";
            }
            else
            {
                string serverPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
                if (!File.Exists(serverPath))
                {
                    Error = $"{StartPath} not found ({serverPath})";
                    return null;
                }
            }

            string configPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "serverDZ.cfg");
            if (!File.Exists(configPath))
            {
                Notice = $"{Path.GetFileName(configPath)} not found ({configPath})";
            }

            string param = $" {_serverData.ServerParam}";
            param += string.IsNullOrEmpty(_serverData.ServerIP) ? string.Empty : $" -ip={_serverData.ServerIP}";
            param += string.IsNullOrEmpty(_serverData.ServerPort) ? string.Empty : $" -port={_serverData.ServerPort}";

            string modPath = Functions.ServerPath.GetServersConfigs(_serverData.ServerID, "Modlist.txt");
            if (File.Exists(modPath))
            {
                var lines = File.ReadAllLines(modPath);
                var modParam = UpdateMods(lines);

                if (!string.IsNullOrWhiteSpace(modParam))
                {
                    param += $" \"-mod={modParam}\"";
                }
            }

            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath),
                    Arguments = param,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };
            p.Start();

            return p;
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                p.Kill();
            });
        }

        private string UpdateMods(string[] modList)
        {
            var modParam = "";
            var modIds = new string[16];
            var modNames = new string[16];
            int index = 0;
            foreach (string line in modList)
            {
                var splits = line.Split(',');
                if (splits.Length != 2)
                    continue;

                modIds[index] = splits[0];
                modNames[index] = splits[1];

                modParam += $"{splits[1].Replace("@", "").Replace(",", "").Trim()};";
            }
            DownloadMods(modIds);

            return modParam;
        }

        private void DownloadMods(string[] modIds)
        {        
            string _exeFile = "steamcmd.exe";
            string _installPath = ServerPath.GetBin("steamcmd");
        
            string PluginsPath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "/Plugins/");
            string exePath = Path.Combine(_installPath, _exeFile);

            if (!File.Exists(exePath))
            {
                Error = $"SteamCMD not available, break up";
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"{GetLogin()} +force_install_dir \"{PluginsPath}\"");
            foreach (var modId in modIds)
            {
                if(! string.IsNullOrEmpty(modId))
                    sb.Append($" +workshop_download_item 221100 {modId}");
            }

            sb.Append($" +quit");
            Process p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = _installPath,
                    FileName = exePath,
                    Arguments = sb.ToString(),
                    WindowStyle = ProcessWindowStyle.Minimized,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };
            p.Start();
            p.WaitForExit();

            return;
        }

        private string GetLogin()
        {
            string _installPath = ServerPath.GetBin("steamcmd");
            string _userDataPath = Path.Combine(_installPath, "userData.txt");

            string steamUser = null, steamPass = null;

            if (File.Exists(_userDataPath))
            {
                string[] lines = File.ReadAllLines(_userDataPath);

                foreach (string line in lines)
                {
                    if (line[0] == '/' && line[1] == '/')
                    {
                        continue;
                    }

                    string[] keyvalue = line.Split(new char[] { '=' }, 2);
                    if (keyvalue[0] == "steamUser")
                    {
                        steamUser = keyvalue[1].Trim('\"');
                    }
                    else if (keyvalue[0] == "steamPass")
                    {
                        steamPass = keyvalue[1].Trim('\"');
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(steamUser) || string.IsNullOrWhiteSpace(steamPass))
            {
                Error = "Can not receive Userlogin!";
                return null;
            }

            return $" +login \"{steamUser}\" \"{steamPass}\"";
        }
    }
}