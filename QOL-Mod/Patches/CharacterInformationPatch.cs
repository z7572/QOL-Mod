﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QOL
{
    class CharacterInformationPatch
    {
        public static void Patch(Harmony harmonyInstance)
        {
            var startMethod = AccessTools.Method(typeof(CharacterInformation), "Start");
            var startMethodPostfix = new HarmonyMethod(typeof(CharacterInformationPatch).GetMethod(nameof(StartMethodPostfix)));
            harmonyInstance.Patch(startMethod, postfix: startMethodPostfix);
        }

        public static void StartMethodPostfix(CharacterInformation __instance)
        {
            if (!MatchmakingHandler.Instance.IsInsideLobby)
            {
                Color colorWanted = Plugin.configCustomColor.Value != new Color(1, 1, 1) ? Plugin.configCustomColor.Value : Plugin.defaultColors[0];
                MultiplayerManagerPatches.ChangeAllCharacterColors(colorWanted, __instance.gameObject);
            }
        }
    }
}