using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VNyanInterface;
using WatsonWebsocket;

namespace NyanSaber {
    public static partial class JTokenExtensions {
        public static bool IsNull(this JToken token) {
            return token == null || token.Type == JTokenType.Null;
        }
    }

    public class NyanSaber : IVNyanPluginManifest, ITriggerHandler, IButtonClickedHandler {
        public string PluginName { get; } = "NyanSaber";
        public string Version { get; } = "0.7-beta";
        public string Title { get; } = "Nyan Saber";
        public string Author { get; } = "LumKitty";
        public string Website { get; } = "https://lum.uk/";
        private const string SettingsFileName = "NyanSaber.cfg";

        private static WatsonWsClient? client = null;
        private static bool Connecting = false;
        private static bool DisconnectRequested = false;
        private static int MaxScore = 0;
        private static int Combo = 0;

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
        private static string[] BlockedEvents = { };
        private static int LogLevel = 1;

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
                    string tempBlockEvents;
                    string tempLogLevel;

                    if (settings.TryGetValue("BlockedEvents", out tempBlockEvents)) {
                        if (tempBlockEvents.Length > 0) {
                            try {
                                BlockedEvents = tempBlockEvents.Split(',');
                            } catch {
                                Log("Could not parse blocked event list: " + tempBlockEvents);
                                SettingMissing = true;
                            }
                            
                        } else {
                            Log("Blocked event list is blank. Enabling all events");
                        }
                    } else {
                        Log("Setting missing. Using default Blocked Events List: ");
                        SettingMissing = true;
                    }
                    if (SettingMissing) {
                        BlockedEvents = new string[] { "_lum_bs_notefullycut", "_lum_bs_notemisseddetails", "_lum_bs_beatmap", "_lum_bs_energychanged" };
                    }
                    Log("Final list of Blocked Events: " + String.Join(",", BlockedEvents));

                    if (settings.TryGetValue("LogLevel", out tempLogLevel)) {
                        if (int.TryParse(tempLogLevel, out LogLevel)) {
                            Log("Log level: " + LogLevel.ToString());
                        } else {
                            Log("Could not read LogLevel setting, setting to 1");
                            LogLevel = 1;
                            SettingMissing = true;
                        }
                    } else {
                        LogLevel = 1;
                        Log("LogLevel setting missing, setting to 1");
                        SettingMissing = true;
                    }

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
            settings["BlockedEvents"] = String.Join(',', BlockedEvents);
            settings["LogLevel"] = LogLevel.ToString();

            VNyanInterface.VNyanInterface.VNyanSettings.saveSettings(SettingsFileName, settings);
        }

        private static void CallVNyan(string TriggerName, int int1, int int2, int int3, string text1, string text2, string text3) {
            if (LogLevel >= 2) {
                Log("Trigger: " + TriggerName.PadRight(20, ' ') + "|" + int1.ToString().PadRight(5, ' ') + "|" + int2.ToString().PadRight(5, ' ') + "|" + int3.ToString().PadRight(5, ' ')
                   + "|" + text1 + "|" + text2 + "|" + text3);
            } else if (LogLevel == 1) {
                Log("Trigger: " + TriggerName);
            }
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
                            if (LogLevel >= 3) { Log("Connect function exiting"); }
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
                if (LogLevel >= 4) {
                    Log("Colours: " + Colors.ToString());
                    Log(Colors[0].ToString());
                    Log(Colors[1].ToString());
                    Log(Colors[2].ToString());
                }
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

        private static int JObjectToInt(ref JObject Values, string Key) {
            JValue temp = (JValue)Values[Key];
            if (temp.Type != JTokenType.Null) {
                int result;
                int.TryParse(temp.ToString(), out result);
                return result;
            } else {
                return -1;
            }

        }

        private static string JSONtoVNyan(ref JObject InputJSON) {
            JObject Results = new JObject();
            foreach (JProperty Key in InputJSON.OfType<JProperty>()) {
                Results.Add(new JProperty(Key.Name.ToLower(), Key.Value.ToString()));
            }
            return Results.ToString();
        }
        private static string JSONtoVNyan(JObject InputJSON) {
            return JSONtoVNyan(ref InputJSON);
        }



        private static void LogHeader(ref string TriggerName, ref JObject Results) {
            if (LogLevel >= 2) { 
                Log("**************************************************************************");
                Log(TriggerName);
            }
            
            if (LogLevel >= 3) {
                RemoveSongCover(ref Results);
                Log(Results.ToString()); 
            }
        }

        private static void RemoveSongCover(ref JObject Results) {
            try { 
                if (Results.ContainsKey("status")) {
                    if (LogLevel >= 4) { Log("Status key found"); }
                    JObject Status = (JObject)Results["status"];
                    if (Status.ContainsKey("beatmap") && (!Status["beatmap"].IsNull())) {
                        if (LogLevel >= 4) { Log("Beatmap key found"); }
                        JObject Beatmap = (JObject)Status["beatmap"];
                        if (Beatmap.ContainsKey("songCover")) {
                            if (LogLevel >= 4) { Log("SongCover key found, removing it"); }
                            Beatmap.Remove("songCover");
                        }
                    }
                }
            } catch (Exception e) {
                ErrorHandler(e);
            }
        }

        private static void BSMenuEvent(string TriggerName, ref JObject Results) {
            if (!BlockedEvents.Contains(TriggerName)) {
                LogHeader(ref TriggerName, ref Results);
                CallVNyan(TriggerName, 0, 0, 0, "", "", "");
            }
        }

        private static void BSPerformanceEvent(string TriggerName, ref JObject Results) {
            if (!BlockedEvents.Contains(TriggerName)) {
                //if (TriggerName != "_lum_bs_scorechanged") {
                LogHeader(ref TriggerName, ref Results);
                //}
                JObject Status = (JObject)Results["status"];
                string PerformanceResult;
                string NoteResult;
                int NoteScore = 0;
                int Combo = 0;
                int MissCount = 0;
                string Rank = "";
                if (Status.ContainsKey("performance")) {
                    JObject Performance = (JObject)Status["performance"];
                    int.TryParse(Performance["combo"].ToString(), out Combo);
                    Rank = Performance["rank"].ToString();
                    PerformanceResult = new JObject(
                        new JProperty("rawscore", Performance["rawScore"].ToString()),
                        new JProperty("score", Performance["score"].ToString()),
                        new JProperty("currentmaxscore", Performance["currentMaxScore"].ToString()),
                        new JProperty("rank", Rank),
                        new JProperty("relativescore", Performance["relativeScore"].ToString()),
                        new JProperty("passednotes", Performance["passedNotes"].ToString()),
                        new JProperty("hitnotes", Performance["hitNotes"].ToString()),
                        new JProperty("missednotes", Performance["missedNotes"].ToString()),
                        new JProperty("passedbombs", Performance["passedBombs"].ToString()),
                        new JProperty("hitbombs", Performance["hitBombs"].ToString()),
                        new JProperty("combo", Combo.ToString()),
                        new JProperty("maxcombo", Performance["maxCombo"].ToString()),
                        new JProperty("multiplier", Performance["multiplier"].ToString()),
                        new JProperty("multiplierprogress", Performance["multiplierProgress"].ToString()),
                        new JProperty("batteryenergy", JObjectToInt(ref Performance, "batteryEnergy").ToString()),
                        new JProperty("currentsongtime", Performance["currentSongTime"].ToString()),
                        new JProperty("softfailed", Performance["softFailed"].ToString())
                    ).ToString();
                } else {
                    PerformanceResult = "";
                }
                if (Results.ContainsKey("noteCut")) {
                    JObject NoteCut = (JObject)Results["noteCut"];
                    JArray SaberDir = (JArray)NoteCut["saberDir"];
                    JArray CutPoint = (JArray)NoteCut["cutPoint"];
                    JArray CutNormal = (JArray)NoteCut["cutNormal"];
                    int InitialScore = JObjectToInt(ref NoteCut, "initialScore");
                    int FinalScore = JObjectToInt(ref NoteCut, "finalScore");
                    int CutDistanceScore = JObjectToInt(ref NoteCut, "cutDistanceScore");
                    JObject NoteResultJSON = new JObject(
                        new JProperty("noteid", NoteCut["noteID"].ToString()),
                        new JProperty("notetype", NoteCut["noteType"].ToString()),
                        new JProperty("notecutdirection", NoteCut["noteCutDirection"].ToString()),
                        new JProperty("noteline", NoteCut["noteLine"].ToString()),
                        new JProperty("notelayer", NoteCut["noteLayer"].ToString()),
                        new JProperty("speedok", NoteCut["speedOK"].ToString()),
                        new JProperty("wascuttoosoon", NoteCut["wasCutTooSoon"].ToString()),
                        new JProperty("initialscore", InitialScore.ToString()),
                        new JProperty("finalscore", FinalScore.ToString()),
                        new JProperty("cutdistancescore", CutDistanceScore.ToString()),
                        new JProperty("multiplier", NoteCut["multiplier"].ToString()),
                        new JProperty("saberspeed", NoteCut["saberSpeed"].ToString()),
                        new JProperty("saberdirx", SaberDir[0].ToString()),
                        new JProperty("saberdiry", SaberDir[1].ToString()),
                        new JProperty("saberdirz", SaberDir[2].ToString()),
                        new JProperty("sabertype", NoteCut["saberType"].ToString()),
                        new JProperty("swingrating", NoteCut["swingRating"].ToString()),
                        new JProperty("timedeviation", NoteCut["timeDeviation"].ToString()),
                        new JProperty("cutdirectiondeviation", NoteCut["cutDirectionDeviation"].ToString()),
                        new JProperty("cutpointx", CutPoint[0].ToString()),
                        new JProperty("cutpointy", CutPoint[1].ToString()),
                        new JProperty("cutpointz", CutPoint[2].ToString()),
                        new JProperty("cutnormalx", CutNormal[0].ToString()),
                        new JProperty("cutnormaly", CutNormal[1].ToString()),
                        new JProperty("cutnormalz", CutNormal[2].ToString()),
                        new JProperty("cutdistancetocenter", NoteCut["cutDistanceToCenter"].ToString()),
                        new JProperty("timetonextbasicnote", NoteCut["timeToNextBasicNote"].ToString())
                    );
                    if (FinalScore > 0) {
                        NoteScore = FinalScore;
                    } else {
                        NoteScore = InitialScore;
                    }
                    NoteResult = NoteResultJSON.ToString();
                } else {
                    NoteResult = "";
                }
                CallVNyan(TriggerName, Combo, MissCount, NoteScore, Rank, PerformanceResult, NoteResult);
            }
        }

        private static void BSScoreEvent(string TriggerName, ref JObject Results) {
            if (!BlockedEvents.Contains(TriggerName)) {
                LogHeader(ref TriggerName, ref Results);
                JObject Performance = (JObject)((JObject)Results["status"])["performance"];
                int TempCombo = 0;
                int TempMaxScore = 0;
                if (int.TryParse(Performance["combo"].ToString(), out TempCombo) &&
                  int.TryParse(Performance["currentMaxScore"].ToString(), out TempMaxScore)) {
                    if (TempCombo != Combo || TempMaxScore != MaxScore) {
                        Combo = TempCombo;
                        MaxScore = TempMaxScore;
                        BSPerformanceEvent("_lum_bs_scorechanged", ref Results);
                    } else {
                        if (LogLevel >= 3) { Log("Filtered duplicate Score event"); }
                    }
                }
            }
        }

        private static void BSLogOnly(string EventName, ref JObject Results) {
            if (LogLevel >= 3) { Log("**************************************************************************"); }
            if (LogLevel >= 2) { Log("LOGONLY: " + EventName); }
            if (LogLevel >= 3) { Log(Results.ToString()); }
        }

        private static void BeatMapEvent(string TriggerName, ref JObject Results) {
            if (!BlockedEvents.Contains("_lum_bs_beatmap")) {
                int EventVersion = 0;
                int Type = 0;
                int GroupID = 0;
                string EventJSON = "";
                string PrevJSON = "";
                string NextJSON = "";

                LogHeader(ref TriggerName, ref Results);
                if (Results.ContainsKey("beatmapEvent")) {
                    JObject Event = (JObject)Results["beatmapEvent"];
                    if (Event.ContainsKey("version")) {
                        string Temp = Event["version"].ToString();
                        int.TryParse(Temp.Substring(0, Temp.IndexOf('.')), out EventVersion);
                    }
                    if (Event.ContainsKey("type")) { int.TryParse(Event["type"].ToString(), out Type); }
                    if (Event.ContainsKey("groupId")) { int.TryParse(Event["groupId"].ToString(), out GroupID); }
                    if (Event.ContainsKey("previousSameTypeEventData")) {
                        PrevJSON = JSONtoVNyan((JObject)Event["previousSameTypeEventData"]);
                        Event.Remove("previousSameTypeEventData");
                    }
                    if (Event.ContainsKey("nextSameTypeEventData")) {
                        NextJSON = JSONtoVNyan((JObject)Event["nextSameTypeEventData"]);
                        Event.Remove("nextSameTypeEventData");
                    }
                    EventJSON = JSONtoVNyan(ref Event);
                    CallVNyan(TriggerName, EventVersion, Type, GroupID, EventJSON, PrevJSON, NextJSON);
                } else {
                    if (LogLevel >=3) { Log("Skipping useless Beatmap event with no data"); }
                }
            }
        }

        private static void BSSongEvent(string TriggerName, ref JObject Results) {
            if (!BlockedEvents.Contains(TriggerName)) {
                LogHeader(ref TriggerName, ref Results);
                JObject Status = (JObject)Results["status"];
                if (Status.ContainsKey("beatmap")) {
                    if (LogLevel >= 3) { Log("Song info found"); }
                    JObject Song = (JObject)Status["beatmap"];
                    if (Song.ContainsKey("songCover")) { Song["songCover"] = ""; }
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
                        new JProperty("sabera", BSColorToHex((JArray)TempColors["saberA"])),
                        new JProperty("saberb", BSColorToHex((JArray)TempColors["saberB"])),
                        new JProperty("obstacle", BSColorToHex((JArray)TempColors["obstacle"])),
                        new JProperty("environment0", BSColorToHex((JArray)TempColors["environment0"])),
                        new JProperty("environment1", BSColorToHex((JArray)TempColors["environment1"])),
                        new JProperty("environment0boost", BSColorToHex((JArray)TempColors["environment0Boost"])),
                        new JProperty("environment1boost", BSColorToHex((JArray)TempColors["environment1Boost"]))
                    );

                    int Seconds = (int)Song["length"] / 1000;
                    int Minutes = (int)Math.Floor((decimal)(Seconds / 60));
                    Seconds = Seconds - (Minutes * 60);

                    JObject SongInfo = new JObject(
                        new JProperty("songname", Song["songName"]),
                        new JProperty("songsubname", Song["songSubName"]),
                        new JProperty("songauthorname", Song["songAuthorName"]),
                        new JProperty("levelauthornames", BSNamesToList((JArray)Song["levelAuthorNamesArray"])),
                        new JProperty("lighternames", BSNamesToList((JArray)Song["lighterNamesArray"])),
                        new JProperty("length", Song["length"]),
                        new JProperty("lengthtext", Minutes + ":" + Seconds.ToString().PadLeft(2, '0')),
                        new JProperty("environmentname", Song["environmentName"]),
                        new JProperty("difficulty", Song["difficulty"]),
                        new JProperty("notejumpspeed", Song["noteJumpSpeed"])
                    );

                    CallVNyan(TriggerName, SongDifficulty, BPM, SongLength, SongName, Colors.ToString(), SongInfo.ToString());


                    //Log("Song Started: " + SongName + "(Difficulty: " + Difficulty + ", BPM: " + BPM + ", Duration: " + Duration + " seconds)");
                } else {
                    if (LogLevel >= 3) { Log("Song info not found"); }
                    CallVNyan(TriggerName, 0, 0, 0, "", "", "");
                }
            }
        }

        private static async void MessageReceived(object sender, MessageReceivedEventArgs args) {
            try {
                string Response = Encoding.UTF8.GetString(args.Data);
                if (LogLevel >= 5) { Log("Message from server: " + Response.Substring(0, 40) + "..."); }

                JObject Results = JObject.Parse(Response);

                switch ((string)Results["event"]) {
                    case "hello":
                        break;
                    case "noteSpawned":
                        break;
                    case "softFailed":
                        BSPerformanceEvent("_lum_bs_softfailed", ref Results);
                        break;
                    case "energyChanged":
                        BSPerformanceEvent("_lum_bs_energychanged", ref Results);
                        break;
                    case "beatmapEvent":
                        BeatMapEvent("_lum_bs_beatmap", ref Results);
                        break;
                    case "songStart":
                        BSSongEvent("_lum_bs_songstart", ref Results);
                        break;
                    case "finished":
                        BSSongEvent("_lum_bs_songpass", ref Results);
                        break;
                    case "failed":
                        BSSongEvent("_lum_bs_songfail", ref Results);
                        break;
                    case "menu":
                        BSMenuEvent("_lum_bs_menu", ref Results);
                        break;
                    case "pause":
                        BSSongEvent("_lum_bs_songpause", ref Results);
                        break;
                    case "resume":
                        BSSongEvent("_lum_bs_songunpause", ref Results);
                        break;
                    case "noteCut":
                        BSPerformanceEvent("_lum_bs_notecut", ref Results);
                        break;
                    case "noteFullyCut":
                        BSPerformanceEvent("_lum_bs_notefullycut", ref Results);
                        break;
                    case "noteMissed":
                        if (Results.ContainsKey("noteCut")) {
                            BSPerformanceEvent("_lum_bs_notemisseddetails", ref Results);
                        } else {
                            BSPerformanceEvent("_lum_bs_notemissed", ref Results);
                        }
                        break;
                    case "bombCut":
                        BSPerformanceEvent("_lum_bs_bombcut", ref Results);
                        break;
                    case "bombMissed":
                        BSPerformanceEvent("_lum_bs_bombmissed", ref Results);
                        break;
                    case "obstacleEnter":
                        BSPerformanceEvent("_lum_bs_obstacleenter", ref Results);
                        break;
                    case "obstacleExit":
                        BSPerformanceEvent("_lum_bs_obstacleexit", ref Results);
                        break;
                    case "scoreChanged":
                        BSScoreEvent("_lum_bs_scorechanged", ref Results);
                        break;
                    default:
                        Log("Unknown event type: " + (string)Results["event"]);
                        if (LogLevel >= 3) { Log(Results.ToString()); }
                        break;
                }
            } catch (Exception e) {
                ErrorHandler(e);
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
