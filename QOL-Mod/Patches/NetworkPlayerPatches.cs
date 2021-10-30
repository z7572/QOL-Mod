﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
namespace QOL
{
    class NetworkPlayerPatch
    {
        public static void Patch(Harmony harmonyInstance) // NetworkPlayer methods to patch with the harmony instance
        {

            var SyncClientChatMethod = AccessTools.Method(typeof(NetworkPlayer), "SyncClientChat");
            var SyncClientChatMethodPrefix = new HarmonyMethod(typeof(NetworkPlayerPatch).GetMethod(nameof(NetworkPlayerPatch.SyncClientChatMethodPrefix))); // Patches SyncClientChat with prefix method
            harmonyInstance.Patch(SyncClientChatMethod, prefix: SyncClientChatMethodPrefix);
        }

        public static bool SyncClientChatMethodPrefix(ref byte[] data, NetworkPlayer __instance)
        {
            if (!Helper.isTranslating)
            {
                return true;
            }
            TranslateMessage(data, __instance);
            return false;
        }

        public static void TranslateMessage(byte[] data, NetworkPlayer __instance) // Checks if auto-translation is enabled, if so then translate it
        {
            string textToTranslate = Encoding.UTF8.GetString(data);
            Debug.Log("Got message: " + textToTranslate);

            var mHasLocalControl = Traverse.Create(__instance).Field("mHasLocalControl").GetValue(); // TODO: fix this!!
            ChatManager mLocalChatManager = AccessTools.StaticFieldRefAccess<ChatManager>(typeof(NetworkPlayer), "mLocalChatManager");
            Debug.Log("mLocalChatManager : " + mLocalChatManager);
            Debug.Log("mHasLocalControl : " + mHasLocalControl);

            if ((bool)mHasLocalControl)
            {
                __instance.StartCoroutine(Translate.Process("en", textToTranslate, delegate (string s) { mLocalChatManager.Talk(s); }));
                return;
            }
            ChatManager mChatManager = Traverse.Create(__instance).Field("mChatManager").GetValue() as ChatManager;
            __instance.StartCoroutine(Translate.Process("en", textToTranslate, delegate (string s) { mChatManager.Talk(s); }));
        }
    }
}