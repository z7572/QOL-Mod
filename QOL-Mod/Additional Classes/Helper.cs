using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Steamworks;
using SimpleJSON;
using HarmonyLib;
using TMPro;

namespace QOL
{
    public class Helper
    {
        // Returns the steamID of the specified spawnID
        public static CSteamID GetSteamID(ushort targetID) => clientData[targetID].ClientID;

        // Returns the corresponding spawnID from the specified color
        public static ushort GetIDFromColor(string targetSpawnColor) => targetSpawnColor
            switch { 
            "yellow" or "y" => 0, 
            "blue" or "b" => 1,
            "red" or "r" => 2,
            "green" or "g" => 3,
                _ => ushort.MaxValue
            };

        // Returns the corresponding color from the specified spawnID
        public static string GetColorFromID(ushort x) => x switch { 1 => "Blue", 2 => "Red", 3 => "Green", _ => "Yellow" };

        // Returns the targeted player based on the specified spawnID
        public static NetworkPlayer GetNetworkPlayer(ushort targetID) => clientData[targetID].PlayerObject.GetComponent<NetworkPlayer>();

        // Gets the steam profile name of the specified steamID
        public static string GetPlayerName(CSteamID passedClientID) => SteamFriends.GetFriendPersonaName(passedClientID);

        // Actually sticks the "join game" link together (url prefix + appID + LobbyID + SteamID)
        public static string GetJoinGameLink() => $"steam://joinlobby/674940/{lobbyID}/{localPlayerSteamID}";

        public static void ToggleLobbyVisibility(bool open)
        {
            if (MatchmakingHandler.Instance.IsHost)
            {
                MethodInfo ChangeLobbyTypeMethod = typeof(MatchmakingHandler).GetMethod("ChangeLobbyType", BindingFlags.NonPublic | BindingFlags.Instance);

                if (open)
                {
                    ChangeLobbyTypeMethod.Invoke(MatchmakingHandler.Instance, new object[] { ELobbyType.k_ELobbyTypePublic });
                    localNetworkPlayer.OnTalked("Lobby made public!");
                }
                else
                {
                    ChangeLobbyTypeMethod.Invoke(MatchmakingHandler.Instance, new object[] { ELobbyType.k_ELobbyTypePrivate });
                    localNetworkPlayer.OnTalked("Lobby made private!");
                }
                
                return;
            }
            
            localNetworkPlayer.OnTalked("Need to be host!");
        }

        public static void InitValues(ChatManager __instance, ushort playerID) // Assigns the networkPlayer as the local one if it matches our steamID (also if text should be rich or not)
        {
            if (playerID != GameManager.Instance.mMultiplayerManager.LocalPlayerIndex) return;

            clientData = GameManager.Instance.mMultiplayerManager.ConnectedClients;
            mutedPlayers.Clear();

            byte localID = GameManager.Instance.mMultiplayerManager.LocalPlayerIndex;
            localNetworkPlayer = clientData[localID].PlayerObject.GetComponent<NetworkPlayer>();
            localChat = clientData[localID].PlayerObject.GetComponentInChildren<ChatManager>();

            Debug.Log("Assigned the localNetworkPlayer!: " + localNetworkPlayer.NetworkSpawnID);

            tmpText = Traverse.Create(__instance).Field("text").GetValue() as TextMeshPro;
            tmpText.richText = Plugin.configRichText.Value;

            if (Plugin.configFixCrown.Value)
            {
                WinCounterUI counter = UnityEngine.Object.FindObjectOfType<WinCounterUI>();
                foreach (var crownCount in counter.GetComponentsInChildren<TextMeshProUGUI>(true)) crownCount.enableAutoSizing = true;
            }

            if (NameResize)
            {
                TextMeshProUGUI[] playerNames = Traverse.Create(UnityEngine.Object.FindObjectOfType<OnlinePlayerUI>()).Field("mPlayerTexts").GetValue<TextMeshProUGUI[]>();

                foreach (var name in playerNames)
                {
                    name.fontSizeMin = 45;
                    name.fontSizeMax = 45;
                    name.overflowMode = TextOverflowModes.Overflow;
                }
            }

            GameObject rbHand = new ("RainbowHandler");
            rbHand.AddComponent<RainbowManager>().enabled = false;
            rainbowEnabled = false;

            if (Plugin.configAlwaysRainbow.Value) ToggleRainbow();
            
            OptionsHolder.AvailableFramerates = new [] { 48, 60, 69, 72, 75, 120, 144, 240, 0 };
        }

        public static void ToggleWinstreak()
        {
            if (!winStreakEnabled)
            {
                winStreakEnabled = true;
                GameManager.Instance.winText.fontSize = Plugin.configWinStreakFontsize.Value;
                return;
            }

            winStreakEnabled = false;
        }

        public static void ToggleRainbow()
        {
            if (!rainbowEnabled)
            {
                Debug.Log("trying to start RainBowHandler");
                UnityEngine.Object.FindObjectOfType<RainbowManager>().enabled = true;
                rainbowEnabled = true;
                return;
            }

            UnityEngine.Object.FindObjectOfType<RainbowManager>().enabled = false;
            rainbowEnabled = false;
        }

        public static string GetTargetStatValue(CharacterStats stats, string targetStat)
        {
            foreach (var stat in typeof(CharacterStats).GetFields())
                if (stat.Name.ToLower() == targetStat)
                    return stat.GetValue(stats).ToString();

            return "No value";
        }

        public static void SendLocalMsg(string msg) => GameManager.Instance.StartCoroutine(SendClientSideMsg("<#006400>", msg));

        public static IEnumerator SendClientSideMsg(string logLevel, string msg)
        {
            bool origRichTextValue = tmpText.richText;
            float chatMsgDuration = 1.5f + msg.Length * 0.075f; // Time that chat msg will last till closing animation

            tmpText.richText = true;
            localChat.Talk(logLevel + msg);

            yield return new WaitForSeconds(chatMsgDuration + 3f); // Add 3 grace seconds to original msg duration so rich text doesn't stop during closing animation
            tmpText.richText = origRichTextValue;
        }

        // Fancy bit-manipulation of a char's ASCII values to check whether it's a vowel or not
        public static bool IsVowel(char c) => (0x208222 >> (c & 0x1f) & 1) != 0;

        public static CSteamID lobbyID; // The ID of the current lobby
        
        public static readonly CSteamID localPlayerSteamID = SteamUser.GetSteamID(); // The steamID of the local user (ours)
        public static NetworkPlayer localNetworkPlayer; // The networkPlayer of the local user (ours)
        public static List<ushort> mutedPlayers = new(4);

        public static bool isTranslating = Plugin.configTranslation.Value; // True if auto-translations are enabled, false by default
        public static bool autoGG = Plugin.configAutoGG.Value; // True if auto gg on death is enabled, false by default
        public static bool uwuifyText; // True if uwufiy text is enabled, false by default
        public static bool winStreakEnabled = Plugin.configWinStreakLog.Value;
        public static bool chatCensorshipBypass = Plugin.configchatCensorshipBypass.Value; // True if chat censoring is bypassed, false by default
        public static Color customPlayerColor = Plugin.configCustomColor.Value;
        public static bool IsCustomName = !string.IsNullOrEmpty(Plugin.configCustomName.Value);
        public static bool NameResize = Plugin.configNoResize.Value;
        public static bool nukChat;
        public static bool onlyLower;
        public static bool HPWinner = Plugin.configHPWinner.Value;
        public static bool rainbowEnabled;
        public static IEnumerator routineUsed;

        public static ConnectedClientData[] clientData;
        public static ChatManager localChat;

        public static TextMeshPro tmpText;
        public static int winStreak = 0;
    }
}
