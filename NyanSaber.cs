using System;
using System.Text;
using VNyanInterface;
using WatsonWebsocket;
using Newtonsoft.Json;

namespace NyanSaber { 
    public class NyanSaber : IVNyanPluginManifest, ITriggerHandler, IButtonClickedHandler {
        public string PluginName { get; } = "NyanSaber";
        public string Version { get; } = "0.1-alpha";
        public string Title { get; } = "Nyan Saber";
        public string Author { get; } = "LumKitty";
        public string Website { get; } = "https://lum.uk/";

        private static WatsonWsClient client;
        private static bool BSInLevel = false;
        private static bool BSPaused = false;

        public void InitializePlugin() {
            VNyanInterface.VNyanInterface.VNyanTrigger.registerTriggerListener(this);
            VNyanInterface.VNyanInterface.VNyanUI.registerPluginButton("NyanSaber", this);
            client = new WatsonWsClient(new Uri("ws://127.0.0.1:2946/BSDataPuller/MapData"));
            client.ServerConnected += ServerConnected;
            client.ServerDisconnected += ServerDisconnected;
            client.MessageReceived += MessageReceived;
        }
        private static void Log(string message) {
            UnityEngine.Debug.Log("[NyanSaber] " + message);
        }
        public void triggerCalled(string name, int num1, int num2, int num3, string text1, string text2, string text3) {

        }
        public void pluginButtonClicked() {
            if (!client.Connected) {
                ConnectBS();
            } else {
                DisconnectBS();
            }
        }

        private void ConnectBS() {
            Log("Connecting to websocket");
            client.Start();
        }

        private void DisconnectBS() {
            Log("Disconnecting from websocket");
            client.Stop();
        }

        static async void MessageReceived(object sender, MessageReceivedEventArgs args) {
            string Response = Encoding.UTF8.GetString(args.Data);
            Log("Message from server: " + Response.Substring(0, 40) + "...");
            dynamic Results = JsonConvert.DeserializeObject<dynamic>(Response);
            if (!BSInLevel && (bool)Results.InLevel) {
                // Song started
                BSInLevel = true;
                
                VNyanInterface.VNyanInterface.VNyanTrigger.callTrigger("_lum_bs_songstart", 0, 0, 0, "", "", "");
            }
            if (BSInLevel && !(bool)Results.InLevel) {
                // Song ended
                BSInLevel = false;
                BSPaused = false;
                if ((bool)Results.LevelFinished) {
                    // Song succeeded
                    Log("Song Completed");
                    VNyanInterface.VNyanInterface.VNyanTrigger.callTrigger("_lum_bs_songend", 0, 0, 0, "", "", "");

                } else if ((bool)Results.LevelFailed) {
                    // Song failed
                    Log("Song Failed");
                    VNyanInterface.VNyanInterface.VNyanTrigger.callTrigger("_lum_bs_songfail", 0, 0, 0, "", "", "");
                } else if ((bool)Results.LevelQuit) {
                    // Song quit manually
                    Log("Song Quit");
                    VNyanInterface.VNyanInterface.VNyanTrigger.callTrigger("_lum_bs_songquit", 0, 0, 0, "", "", "");
                } else {
                    // WTF
                    Log("Song exited in an unknown way");
                }
            }
            if (!BSPaused && (bool)Results.LevelPaused && (bool)Results.InLevel) {
                // Song paused
                BSPaused = true;
                Log("Song Paused");
                VNyanInterface.VNyanInterface.VNyanTrigger.callTrigger("_lum_bs_songpause", 0, 0, 0, "", "", "");
            }
            if (BSPaused && !(bool)Results.LevelPaused && (bool)Results.InLevel) {
                // Song unpaused
                BSPaused = false;
                Log("Song Unpaused");
                VNyanInterface.VNyanInterface.VNyanTrigger.callTrigger("_lum_bs_songresume", 0, 0, 0, "", "", "");
            }
        }

        static void ServerConnected(object sender, EventArgs args) {
            Console.WriteLine("Server connected");
        }

        static void ServerDisconnected(object sender, EventArgs args) {
            Console.WriteLine("Server disconnected");
        }


    }
}
