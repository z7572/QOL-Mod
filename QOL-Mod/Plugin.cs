﻿using System;
using BepInEx;
using UnityEngine;
using HarmonyLib;   

namespace QOL
{
    [BepInPlugin("monky.plugins.QOL", "QOL Mod", "1.0.6")]
    [BepInProcess("StickFight.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo("Plugin 'monky.plugins.QOL' is loaded!");
            Logger.LogInfo("Hello from monk :D");
            try
            {
                Harmony harmony = new Harmony("monky.QOL"); // Creates harmony instance with identifier
                Logger.LogInfo("Applying ChatManager patches");
                ChatManagerPatches.Patches(harmony);
                Logger.LogInfo("Applying MatchmakingHandler patches");
                MatchmakingHandlerPatch.Patch(harmony);
            }
            catch (Exception ex)
            {
                Logger.LogError("Exception on applying patches: " + ex.Message);
            }
            try
            {
                new GameObject("GUIHandler").AddComponent<GUIManager>();
                Debug.Log("Added GUIManager!");
            }
            catch (Exception ex)
            {
                Logger.LogError("Exception on starting GUIManager: " + ex.Message);
            }
        }
    }
}
