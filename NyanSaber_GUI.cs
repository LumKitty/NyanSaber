using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NyanSaber {
    internal class NyanSaber_GUI : MonoBehaviour {
        internal const string CloseTriggerName = "____bottom_right_gui";
        internal const string CloseTriggerValue = "uk.lum.nyansaber";

        private const int DWidth = 213;
        private const int DHeight = 400;
        private static GameObject objNyanSaber_GUI = new GameObject("NyanSaber_GUI", typeof(NyanSaber_GUI));

        internal static void SetActive(bool Active) { objNyanSaber_GUI.SetActive(Active); }
        internal static void ToggleActive() { objNyanSaber_GUI.SetActive(!objNyanSaber_GUI.activeSelf); }

        void OnEnable() {
            VNyanInterface.VNyanInterface.VNyanTrigger.callTrigger(CloseTriggerName, 0, 0, 0, CloseTriggerValue, "", "");
        }

        void OnDisable() {
            //VRCFTnyan.SavePluginSettings();
        }

        void OnGUI() {
            GUILayout.BeginArea(new Rect(Screen.width - DWidth, Screen.height - DHeight, DWidth, DHeight));
            GUILayout.FlexibleSpace(); // Force bottom alignment

            GUILayout.BeginHorizontal();
            if (NyanSaber.Connected) {
                if (NyanSaber.DisconnectRequested) { 
                    GUILayout.Label("NyanSaber - Connected - Disconnecting");
                } else {
                    GUILayout.Label("NyanSaber - Connected");
                }
            } else {
                if (NyanSaber.Connecting) {
                    GUILayout.Label("NyanSaber - Attempting to connect");
                } else {
                    GUILayout.Label("NyanSaber - Disconnected");
                }
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(" X ")) { SetActive(false); }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Activate")  && !NyanSaber.Connected) { NyanSaber.ConnectBS();    SetActive(false); }
            if (GUILayout.Button("Deactivate") && NyanSaber.Connected) { NyanSaber.DisconnectBS(); SetActive(false); }
            GUILayout.EndHorizontal();

            /*
            GUILayout.BeginHorizontal();
            GUILayout.Label("Track: ");
            EnableEyes = GUILayout.Toggle(VRCFTnyan.EnableEyes, "Eyes");
            EnableMouth = GUILayout.Toggle(VRCFTnyan.EnableMouth, "Mouth");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (VRCFTnyan.EnableEyes != EnableEyes || VRCFTnyan.EnableMouth != EnableMouth) {
                VRCFTnyan.EnableEyes = EnableEyes;
                VRCFTnyan.EnableMouth = EnableMouth;
                VRCFTnyan.FreeUpBlendshapes();
            }
            */

            GUILayout.Space(54); // Padding to avoid conflicting with the Hide UI button
            GUILayout.EndArea();
        }
    }
}