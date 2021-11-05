﻿using System;
using BepInEx;
using UnityEngine;
using HarmonyLib;   
using System.Reflection;

namespace QOL
{
    [BepInPlugin("monky.plugins.QOL", "QOL Mod", "1.0.8")]
    [BepInProcess("StickFight.exe")]    
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo("Plugin 'monky.plugins.QOL' is loaded! [v1.0.8]");
            Logger.LogInfo("Hello from monk :D");
            try
            {
                Harmony harmony = new Harmony("monky.QOL"); // Creates harmony instance with identifier
                Logger.LogInfo("Applying ChatManager patches");
                ChatManagerPatches.Patches(harmony);
                Logger.LogInfo("Applying MatchmakingHandler patch");
                MatchmakingHandlerPatch.Patch(harmony);
                Logger.LogInfo("Applying MultiplayerManager patches");
                MultiplayerManagerPatches.Patches(harmony);
                Logger.LogInfo("Applying NetworkPlayer patch");
                NetworkPlayerPatch.Patch(harmony);
                Logger.LogInfo("Applying Controller patch");
                ControllerPatch.Patch(harmony);
            }
            catch (Exception ex)
            {
                Logger.LogError("Exception on applying patches: " + ex.InnerException);
            }
        }
    }
}
