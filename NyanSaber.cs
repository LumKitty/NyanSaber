using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VNyanInterface;
using WatsonWebsocket;

namespace NyanSaber { 
    public class NyanSaber : IVNyanPluginManifest, ITriggerHandler, IButtonClickedHandler {
        public string PluginName { get; } = "NyanSaber";
        public string Version { get; } = "0.2-beta";
        public string Title { get; } = "Nyan Saber";
        public string Author { get; } = "LumKitty";
        public string Website { get; } = "https://lum.uk/";
        private const string SettingsFileName = "NyanSaber.cfg";

        private static WatsonWsClient? client = null;
        private static bool Connecting = false;
        private static bool DisconnectRequested = false;

        // Default settings
        
        private const string DefaultURL = "ws://127.0.0.1:6557/socket";
        private const int DefaultRetryInterval = 1000;
        private const int DefaultMaxRetries = 5;
        private const bool DefaultRetryOnDisconnect = false;

        // User Settings
        private static string URL;
        private static int RetryInterval;
        private static int MaxRetries;
        private static bool RetryOnDisconnect;

        public void InitializePlugin() {
            VNyanInterface.VNyanInterface.VNyanTrigger.registerTriggerListener(this);
            VNyanInterface.VNyanInterface.VNyanUI.registerPluginButton("NyanSaber", this);
            LoadPluginSettings();
        }
        private static void Log(string message) {
            UnityEngine.Debug.Log("[NyanSaber] " + message);
        }
        private static void ErrorHandler(Exception e) {
            Log("ERROR: " + e.ToString());
        }

        private void LoadPluginSettings() {
            try {
                // Get settings in dictionary
                Dictionary<string, string> settings = VNyanInterface.VNyanInterface.VNyanSettings.loadSettings(SettingsFileName);
                bool SettingMissing = false;

                if (settings != null) {
                    // Read string value
                    string tempURL;
                    string tempRetryInterval;
                    string tempMaxRetries;
                    string tempRetryOnDisconnect;

                    if (settings.TryGetValue("URL", out tempURL)) {
                        if (tempURL.Length > 0 && tempURL.Substring(0,5) == "ws://") {
                            URL = tempURL;
                            Log("BSDataPull URL: "+URL);
                        } else {
                            URL = DefaultURL;
                            Log("Invalid URL: "+tempURL+". Using default URL: "+URL);
                            SettingMissing = true;
                        }
                    } else {
                        URL = DefaultURL;
                        Log("Using default URL: " + URL);
                        SettingMissing = true;
                    }
                    if (settings.TryGetValue("RetryInterval", out tempRetryInterval)) {
                        if (int.TryParse(tempRetryInterval, out RetryInterval)) {
                            Log("Retry Interval: " + RetryInterval.ToString() + "ms");
                        } else {
                            RetryInterval = DefaultRetryInterval;
                            Log("Invalid retry interval, using default: " + RetryInterval.ToString() + "ms");
                            SettingMissing = true;
                        }
                    } else {
                        RetryInterval = DefaultRetryInterval;
                        Log("Using default retry interval: " + RetryInterval.ToString() + "ms");
                        SettingMissing = true;
                    }
                    if (settings.TryGetValue("MaxRetries", out tempMaxRetries)) {
                        if (int.TryParse(tempMaxRetries, out MaxRetries)) {
                            Log("Max Retries: " + MaxRetries.ToString());
                        } else {
                            MaxRetries = DefaultMaxRetries;
                            Log("Invalid max retries, using default: " + MaxRetries.ToString());
                            SettingMissing = true;
                        }
                    } else {
                        MaxRetries = DefaultMaxRetries;
                        Log("Using default max retries: " + MaxRetries.ToString());
                        SettingMissing = true;
                    }
                    if (settings.TryGetValue("RetryOnDisconnect", out tempRetryOnDisconnect)) {
                        if (bool.TryParse(tempRetryOnDisconnect, out RetryOnDisconnect)) {
                            Log("Retry on disconnect: " + RetryOnDisconnect.ToString());
                        } else {
                            RetryOnDisconnect = DefaultRetryOnDisconnect;
                            Log("Invalid retry on disconnect, using default: " + RetryOnDisconnect.ToString());
                            SettingMissing = true;
                        }
                    } else {
                        RetryOnDisconnect = DefaultRetryOnDisconnect;
                        Log("Using default retry on disconnect, using default: " + RetryOnDisconnect.ToString());
                        SettingMissing = true;
                    }


                } else {
                    Log("No settings file detected, using defaults");
                    SettingMissing = true;
                    URL = DefaultURL;
                    RetryInterval = DefaultRetryInterval;
                    MaxRetries = DefaultMaxRetries;
                    RetryOnDisconnect = DefaultRetryOnDisconnect;
                }
                if (SettingMissing) {
                    Log("Writing settings file");
                    SavePluginSettings();
                }
            } catch (Exception e) {
                ErrorHandler(e);
            }
        }
        private void SavePluginSettings() {
            Dictionary<string, string> settings = new Dictionary<string, string>();
            settings["URL"] = URL;
            settings["RetryInterval"] = RetryInterval.ToString();
            settings["MaxRetries"] = MaxRetries.ToString();
            settings["RetryOnDisconnect"] = RetryOnDisconnect.ToString();

            VNyanInterface.VNyanInterface.VNyanSettings.saveSettings(SettingsFileName, settings);
        }

        private static void CallVNyan(string TriggerName, int int1, int int2, int int3, string text1, string text2, string text3) {
            Log("Trigger: " + TriggerName.PadRight(20, ' ') + "|" + int1.ToString().PadRight(5, ' ') + "|" + int2.ToString().PadRight(5, ' ') + "|" + int3.ToString().PadRight(5, ' ')
               + "|" + text1 + "|" + text2 + "|" + text3);
            VNyanInterface.VNyanInterface.VNyanTrigger.callTrigger(TriggerName, int1, int2, int3, text1, text2, text3);
        }
        public void triggerCalled(string name, int num1, int num2, int num3, string text1, string text2, string text3) {
            if (name.Length > 8) {
                name = name.ToLower();
                if (name.Substring(0, 8) == "_lum_bs_") {
                    switch (name.ToLower().Substring(7)) {
                        case "_connect":
                            if (text1 != "" && text1.Substring(0, 5) == "ws://") { URL = text1; }
                            if (num1 > 0) { MaxRetries = num1; }
                            if (num2 > 0) { RetryInterval = num2; }
                            if (num3 != 0) { RetryOnDisconnect = (num3 > 0); }
                            Task.Run(() => ConnectBS());
                            break;
                        case "_disconnect":
                            Task.Run(() => DisconnectBS());
                            break;
                    }
                }
            }
        }
        public void pluginButtonClicked() {
            if (client == null) {
                Task.Run(() => ConnectBS());
            } else {
                Task.Run(() => DisconnectBS());
            }
        }

        private static async void ConnectBS() {
            try {
                if (!Connecting && client == null) {
                    Connecting = true;
                    DisconnectRequested = false;
                    Log("Connecting to websocket");
                    client = new WatsonWsClient(new Uri(URL));
                    client.ServerConnected += ServerConnected;
                    client.ServerDisconnected += ServerDisconnected;
                    client.MessageReceived += MessageReceived;
                    client.Start();
                    int Retries = 0;
                    Thread.Sleep(RetryInterval);
                    while (!client.Connected && Retries < MaxRetries && !DisconnectRequested) {
                        Retries++;
                        Log("Failed to connect, retrying " + Retries.ToString() + "/" + MaxRetries.ToString());
                        client = new WatsonWsClient(new Uri(URL));
                        client.ServerConnected += ServerConnected;
                        client.ServerDisconnected += ServerDisconnected;
                        client.MessageReceived += MessageReceived;
                        client.Start();
                        Thread.Sleep(RetryInterval);
                    }
                    if (client.Connected) {
                        if (DisconnectRequested) {
                            Log("Connected, but user requested a disconnect");
                            client.ServerDisconnected -= ServerDisconnected;
                            client.Stop();
                            client = null;
                            VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat("_lum_bs_connected", 0);
                            CallVNyan("_lum_bs_disconnected", 0, 0, 0, "", "", "");
                        } else {
                            Log("Connect function exiting");
                        }
                    } else {
                        if (DisconnectRequested) {
                            Log("Connection attempt aborted by request");
                            CallVNyan("_lum_bs_connectaborted", 0, 0, 0, "", "", "");
                        } else {
                            Log("Failed to connect to Beat Saber on: " + URL);
                            CallVNyan("_lum_bs_connectfailed", 0, 0, 0, "", "", "");
                        }
                        client = null;
                    }
                    Connecting = false;
                } else {
                    Log("Attempted to connect while already connected");
                }
            } catch (Exception e) {
                ErrorHandler(e);
            }
        }

        private static async void DisconnectBS() {
            if (Connecting) {
                DisconnectRequested = true;
                Log("Requesting disconnect");
            } else if (client != null) {
                Log("Disconnecting from websocket");
                client.ServerDisconnected -= ServerDisconnected;
                client.Stop();
                client = null;
                VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat("_lum_bs_connected", 0);
                CallVNyan("_lum_bs_disconnected", 0, 0, 0, "", "", "");
            } else {
                Log("Attempted to disconnect while not connected");
            }
        }

        private static string JoinJArray(JArray values) {
            string result = "";
            foreach (JToken value in values) {
                result += value.ToString() + ", ";
            }
            if (result.Length > 2) {
                return result.Substring(0, result.Length - 2);
            } else {
                return "";
            }
        }
        private static string ConvertNullString(object value) {
            if (value == null) {
                return "";
            } else {
                return value.ToString();
            }
        }

        private static string BSColorToHex(JArray Colors) {
            if (Colors != null) {
                Log("Colours: " + Colors.ToString());
                Log(Colors[0].ToString());
                Log(Colors[1].ToString());
                Log(Colors[2].ToString());
                return ((int)Colors[0]).ToString("X2") + ((int)Colors[1]).ToString("X2") + ((int)Colors[2]).ToString("X2");
            } else {
                return "";
            }
        }

        private static string BSNamesToList(JArray Names) {
            string result = "";
            if (Names.Count > 0) {
                foreach (JToken Name in Names) {
                    result += (String)Name + ", ";
                }
                return result.Substring(0, result.Length - 2);
            } else {
                return "";
            }
        }

        private static void BSMenuEvent(string TriggerName, JObject Results) {
            CallVNyan(TriggerName, 0, 0, 0, "", "", "");
        }

        private static void BSSongEvent(string TriggerName, JObject Results) {
            Log(TriggerName);
            JObject Status = (JObject)Results["status"];
            if (Status.ContainsKey("beatmap")) {
                Log("Song info found");
                JObject Song = (JObject)Status["beatmap"];
                if (Song.ContainsKey("songCover")) { Song["songCover"] = ""; }
                Log(Song.ToString());
                int BPM;
                Int32.TryParse((string)Song["songBPM"], out BPM);
                int SongDifficulty;
                int SongLength;
                string Lighters;
                string FriendlyDuration;

                Int32.TryParse((string)Song["length"], out SongLength);

                switch (((string)Song["difficulty"]).ToLower()) {
                    case "easy":
                        SongDifficulty = 1;
                        break;
                    case "normal":
                        SongDifficulty = 2;
                        break;
                    case "hard":
                        SongDifficulty = 3;
                        break;
                    case "expert":
                        SongDifficulty = 4;
                        break;
                    case "expertplus":
                        SongDifficulty = 5;
                        break;
                    default:
                        SongDifficulty = 0;
                        break;
                }

                string Difficulty = (string)Song["difficulty"];
                string SongName = (string)Song["songAuthorName"] + " - " + (string)Song["songName"];
                string SongSubName = (string)Song["songSubName"];
                if ((SongSubName != null) && (SongSubName.Length > 0)) {
                    SongName += " (" + SongSubName + ")";
                }
                JObject TempColors = (JObject)Song["color"];

                JObject Colors = new JObject(
                    new JProperty("left", BSColorToHex((JArray)TempColors["saberA"])),
                    new JProperty("right", BSColorToHex((JArray)TempColors["saberB"])),
                    new JProperty("obstacles", BSColorToHex((JArray)TempColors["obstacle"])),
                    new JProperty("environment0", BSColorToHex((JArray)TempColors["environment0"])),
                    new JProperty("environment1", BSColorToHex((JArray)TempColors["environment1"])),
                    new JProperty("environment0boost", BSColorToHex((JArray)TempColors["environment0Boost"])),
                    new JProperty("environment1boost", BSColorToHex((JArray)TempColors["environment1Boost"]))
                );

                int Seconds = (int)Song["length"] / 1000;
                int Minutes = Seconds / 60;
                Seconds = Seconds - Minutes;

                JObject SongInfo = new JObject(
                    new JProperty("songname", Song["songName"]),
                    new JProperty("songsubname", Song["songSubName"]),
                    new JProperty("songauthor", Song["songAuthorName"]),
                    new JProperty("mappers", BSNamesToList((JArray)Song["levelAuthorNamesArray"])),
                    new JProperty("lighters", BSNamesToList((JArray)Song["lighterNamesArray"])),
                    new JProperty("duration", Song["length"]),
                    new JProperty("durationtext", Minutes + ":" + Seconds),
                    new JProperty("environment", Song["environmentName"]),
                    new JProperty("difficulty", Song["difficulty"]),
                    new JProperty("njs", Song["noteJumpSpeed"])
                );

                CallVNyan(TriggerName, SongDifficulty, BPM, SongLength, SongName, Colors.ToString(), SongInfo.ToString());


                //Log("Song Started: " + SongName + "(Difficulty: " + Difficulty + ", BPM: " + BPM + ", Duration: " + Duration + " seconds)");
            } else {
                Log("Song info not found");
                CallVNyan(TriggerName, 0, 0, 0, "", "", "");
            }
        }

        private static async void MessageReceived(object sender, MessageReceivedEventArgs args) {
            string Response = Encoding.UTF8.GetString(args.Data);
            //Log("Message from server: " + Response.Substring(0, 40) + "...");

            JObject Results = JObject.Parse(Response);

            Log("Event: " + (string)Results["event"]);

            switch ((string)Results["event"]) {
                case "hello":

                    break;
                case "songStart":
                    BSSongEvent("_lum_bs_songstart", Results);
                    break;
                case "finished":
                    BSSongEvent("_lum_bs_songpass", Results);
                    break;
                case "failed":
                    BSSongEvent("_lum_bs_songfail", Results);
                    break;
                case "menu":
                    BSMenuEvent("_lum_bs_menu", Results);
                    break;
                case "pause":
                    BSSongEvent("_lum_bs_songpause", Results);
                    break;
                case "resume":
                    BSSongEvent("_lum_bs_songunpause", Results);
                    break;
            }
        }

        static async void ServerConnected(object sender, EventArgs args) {
            Log("Server connected");
            VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat("_lum_bs_connected", 1);
            CallVNyan("_lum_bs_connected",0,0,0,"","","");
        }

        static async void ServerDisconnected(object sender, EventArgs args) {
            Log("Server disconnected");
            client = null;
            VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat("_lum_bs_connected", 0);
            CallVNyan("_lum_bs_disconnected", 1, 0, 0, "", "", "");
            if (RetryOnDisconnect) {
                ConnectBS();
            }
        }
    }
}
