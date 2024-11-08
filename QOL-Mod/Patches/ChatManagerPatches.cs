using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace QOL {

    public class ChatManagerPatches
    {
        public static void Patches(Harmony harmonyInstance) // ChatManager methods to patch with the harmony __instance
        {
            var awakeMethod = AccessTools.Method(typeof(ChatManager), "Awake");
            var awakeMethodPrefix = new HarmonyMethod(typeof(ChatManagerPatches)
                 .GetMethod(nameof(AwakeMethodPrefix)));
            harmonyInstance.Patch(awakeMethod, prefix: awakeMethodPrefix);

            var startMethod = AccessTools.Method(typeof(ChatManager), "Start");
            var startMethodPostfix = new HarmonyMethod(typeof(ChatManagerPatches)
                .GetMethod(nameof(StartMethodPostfix))); // Patches Start() with prefix method
            harmonyInstance.Patch(startMethod, postfix: startMethodPostfix);

            var updateMethod = AccessTools.Method(typeof(ChatManager), "Update");
            var updateMethodTranspiler = new HarmonyMethod(typeof(ChatManagerPatches)
                .GetMethod(nameof(UpdateMethodTranspiler))); // Patches Update() with transpiler method
            var updateMethodPostfix = new HarmonyMethod(typeof(ChatManagerPatches).GetMethod(nameof(UpdateMethodPostfix)));
            harmonyInstance.Patch(updateMethod, transpiler: updateMethodTranspiler);
            harmonyInstance.Patch(updateMethod, postfix: updateMethodPostfix);

            var stopTypingMethod = AccessTools.Method(typeof(ChatManager), "StopTyping");
            var stopTypingMethodPostfix = new HarmonyMethod(typeof(ChatManagerPatches)
                .GetMethod(nameof(StopTypingMethodPostfix))); // Patches StopTyping() with postfix method
            harmonyInstance.Patch(stopTypingMethod, postfix: stopTypingMethodPostfix);

            var sendChatMessageMethod = AccessTools.Method(typeof(ChatManager), "SendChatMessage");
            var sendChatMessageMethodPrefix = new HarmonyMethod(typeof(ChatManagerPatches)
                .GetMethod(nameof(SendChatMessageMethodPrefix)));
            harmonyInstance.Patch(sendChatMessageMethod, prefix: sendChatMessageMethodPrefix);

            var replaceUnacceptableWordsMethod = AccessTools.Method(typeof(ChatManager), "ReplaceUnacceptableWords");
            var replaceUnacceptableWordsMethodPrefix = new HarmonyMethod(typeof(ChatManagerPatches)
                .GetMethod(nameof(ReplaceUnacceptableWordsMethodPrefix)));
            harmonyInstance.Patch(replaceUnacceptableWordsMethod, prefix: replaceUnacceptableWordsMethodPrefix);

            var talkMethod = AccessTools.Method(typeof(ChatManager), "Talk");
            var talkMethodPostfix = new HarmonyMethod(typeof(ChatManagerPatches).GetMethod(nameof(TalkMethodPostfix)));
            harmonyInstance.Patch(talkMethod, postfix: talkMethodPostfix);
        }

        // Enable chat bubble in all lobbies
        public static bool AwakeMethodPrefix(ChatManager __instance)
        {
            return false;
        }

        // TODO: Remove unneeded parameters and perhaps this entire method
        public static void StartMethodPostfix(ChatManager __instance)
        {
            var playerID = Traverse.Create(__instance)
                .Field("m_NetworkPlayer")
                .GetValue<NetworkPlayer>()
                .NetworkSpawnID;

            // Assigns m_NetworkPlayer value to Helper.localNetworkPlayer if networkPlayer is ours
            Helper.InitValues(__instance, playerID);
        }

        public static void UpdateMethodPostfix(ChatManager __instance)
        {
            var chatFieldInfo = AccessTools.Field(typeof(ChatManager), "chatField");
            TMP_InputField chatField = (TMP_InputField)chatFieldInfo.GetValue(__instance);

            // Enable chat in all lobbies (will not show chat bubble, some of commands work)
            m_NetworkPlayer = Traverse.Create(__instance).Field("m_NetworkPlayer").GetValue<NetworkPlayer>();
            if (true) //m_NetworkPlayer always true
            {
                if (!ChatManager.isTyping && !PauseManager.isPaused)
                {
                    if (Input.GetKeyDown(KeyCode.Slash))
                    {
                        __instance.StartTyping();
                        chatField.DeactivateInputField();
                        chatField.text = "/";
                        chatField.stringPosition = chatField.text.Length;
                        chatField.ActivateInputField();
                    }
                }
                
                // Press (Ctrl + )Tab or scroll wheel to switch commands
                // TODO: fix bug: cmd will skip current cmd (Press Tab will call CheckForArrowKeysAndAutoComplete())
                if ((!Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Tab))
                    || Input.GetAxis("Mouse ScrollWheel") < 0 && ChatManager.isTyping)
                {
                    var txt = chatField.text;
                    if (!txt.StartsWith(Command.CmdPrefix)) return; // Not a command

                    var txtLen = txt.Length;
                    var allCmds = ChatCommands.CmdNames;
                    var strPos = chatField.stringPosition;
                    var matchedCmd = allCmds.FirstOrDefault(cmd => txt.ToLower() == cmd);
                    var nextCmd = "";

                    if (matchedCmd != null) // txt before cursor matches any command
                    {
                        txt = matchedCmd; // Clear possibly parameters

                        var currentCmdIndex = allCmds.FindIndex(cmd => cmd.StartsWith(txt));

                        nextCmd = currentCmdIndex >= 0 && currentCmdIndex < allCmds.Count - 1
                            ? allCmds[currentCmdIndex + 1]
                            : allCmds.First();

                        chatField.DeactivateInputField();
                        chatField.text = nextCmd;
                        chatField.stringPosition = chatField.text.Length;
                        chatField.ActivateInputField();
                    }
                    else // Must be inputting parameters
                    {
                        Debug.Log("Start parameter switch");
                        
                        Command cmd = null;
                        var cmdName = txt.Replace(Command.CmdPrefix, "").Split(' ')[0].ToLower();
                        Debug.Log("cmdName: " + cmdName);

                        if (ChatCommands.CmdDict.ContainsKey(cmdName))
                            cmd = ChatCommands.CmdDict[cmdName];

                        if (cmd != null && cmd.AutoParams != null)
                        {
                            var targetCmdParams = cmd.AutoParams;
                            var paramStartIndex = txt.IndexOf(' ') + 1;
                            var currentParam = txt.Substring(paramStartIndex).Split(' ')[0];
                            Debug.Log("currentParam: " + currentParam);

                            var matchedParam = targetCmdParams.FirstOrDefault(p => p.StartsWith(currentParam));

                            if (matchedParam == null || currentParam != matchedParam || !targetCmdParams.Contains(currentParam)) return; // Skip to complete present parameter

                            var currentParamIndex = targetCmdParams.FindIndex(p => p.StartsWith(currentParam));
                            Debug.Log("currentParamIndex: " + currentParamIndex);

                            var nextParam = currentParamIndex >= 0 && currentParamIndex < targetCmdParams.Count - 1
                                ? targetCmdParams[currentParamIndex + 1]
                                : targetCmdParams.First();
                            Debug.Log("[z7572] nextParam: " + nextParam);

                            chatField.DeactivateInputField();
                            chatField.text = txt.Substring(0, paramStartIndex) + nextParam;
                            chatField.stringPosition = chatField.text.Length;
                            chatField.ActivateInputField();
                        }
                    }
                }
                else if ((Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Tab))
                    || Input.GetAxis("Mouse ScrollWheel") > 0 && ChatManager.isTyping)
                {
                    var txt = chatField.text;
                    if (!txt.StartsWith(Command.CmdPrefix)) return; // Not a command

                    var txtLen = txt.Length;
                    var allCmds = ChatCommands.CmdNames;
                    var strPos = chatField.stringPosition;
                    var matchedCmd = allCmds.FirstOrDefault(cmd => txt.ToLower() == cmd);
                    var prevCmd = "";

                    if (matchedCmd != null) // txt before cursor matches any command
                    {
                        txt = matchedCmd; // Clear possibly parameters

                        var currentCmdIndex = allCmds.FindIndex(cmd => cmd.StartsWith(txt));

                        prevCmd = currentCmdIndex > 0 && currentCmdIndex < allCmds.Count
                            ? allCmds[currentCmdIndex - 1]
                            : allCmds.Last();

                        chatField.DeactivateInputField();
                        chatField.text = prevCmd;
                        chatField.stringPosition = chatField.text.Length;
                        chatField.ActivateInputField();
                    }
                    else // Must be inputting parameters
                    {
                        Command cmd = null;
                        var cmdName = txt.Replace(Command.CmdPrefix, "").Split(' ')[0].ToLower();

                        if (ChatCommands.CmdDict.ContainsKey(cmdName))
                            cmd = ChatCommands.CmdDict[cmdName];

                        if (cmd != null && cmd.AutoParams != null)
                        {
                            var targetCmdParams = cmd.AutoParams;
                            var paramStartIndex = txt.IndexOf(' ') + 1;
                            var currentParam = txt.Substring(paramStartIndex).Split(' ')[0];

                            var matchedParam = targetCmdParams.FirstOrDefault(p => p.StartsWith(currentParam));

                            if (matchedParam == null || currentParam != matchedParam || !targetCmdParams.Contains(currentParam)) return;

                            var currentParamIndex = targetCmdParams.FindIndex(p => p.StartsWith(currentParam));

                            var prevParam = currentParamIndex > 0 && currentParamIndex < targetCmdParams.Count
                                ? targetCmdParams[currentParamIndex - 1]
                                : targetCmdParams.Last();

                            chatField.DeactivateInputField();
                            chatField.text = txt.Substring(0, paramStartIndex) + prevParam;
                            chatField.stringPosition = chatField.text.Length;
                            chatField.ActivateInputField();
                        }
                    }
                }

            }
        }

        // Transpiler patch for Update() of ChatManager; Adds CIL instructions to call CheckForArrowKeys()
        public static IEnumerable<CodeInstruction> UpdateMethodTranspiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator ilGen)
        {
            var stopTypingMethod = AccessTools.Method(typeof(ChatManager), "StopTyping");
            var chatFieldInfo = AccessTools.Field(typeof(ChatManager), "chatField");
            var getKeyDownMethod = AccessTools.Method(typeof(Input), nameof(Input.GetKeyDown), new[] { typeof(KeyCode) });
            var checkForArrowKeysMethod = AccessTools.Method(typeof(ChatManagerPatches), nameof(CheckForArrowKeysAndAutoComplete));
            var instructionList = instructions.ToList(); // Creates list of IL instructions for Update() from enumerable

            for (var i = 0; i < instructionList.Count; i++)
            {
                if (!instructionList[i].Calls(stopTypingMethod) || !instructionList[i - 3].Calls(getKeyDownMethod))
                    continue;

                var jumpToCheckForArrowKeysLabel = ilGen.DefineLabel();

                var instruction0 = instructionList[i - 2];
                instruction0.opcode = OpCodes.Brfalse_S;
                instruction0.operand = jumpToCheckForArrowKeysLabel;
                instruction0.labels.Clear();

                instructionList.InsertRange(i + 1, new[]
                {
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(jumpToCheckForArrowKeysLabel),
                // Gets value of chatField field
                new CodeInstruction(OpCodes.Ldfld, chatFieldInfo),
                // Calls CheckForArrowKeys() with value of chatField
                new CodeInstruction(OpCodes.Call, checkForArrowKeysMethod)
            });

                break;
            }

            // Allow chat in all lobbies
            // Find and remove "if (m_NetworkPlayer.HasLocalControl) {...}"
            var targetInstructions = new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ChatManager), "m_NetworkPlayer")),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(NetworkPlayer), "get_HasLocalControl")),
                new CodeInstruction(OpCodes.Brfalse)
            };
            
            for (var i = 0; i < instructionList.Count - 4; i++)
            {
                if (instructionList[i].opcode == targetInstructions[0].opcode &&
                    instructionList[i + 1].opcode == targetInstructions[1].opcode &&
                    instructionList[i + 1].operand == targetInstructions[1].operand &&
                    instructionList[i + 2].opcode == targetInstructions[2].opcode &&
                    instructionList[i + 2].operand == targetInstructions[2].operand &&
                    instructionList[i + 3].opcode == targetInstructions[3].opcode)
                {
                    instructionList.RemoveRange(i, 4);
                    break;
                }
            }

            return instructionList.AsEnumerable(); // Returns the now modified list of IL instructions
        }

        public static void StopTypingMethodPostfix()
        {
            Debug.Log("ChatManagerPatches.upArrowCounter : " + _upArrowCounter);
            _upArrowCounter = 0; // When player is finished typing, reset the counter for # of up-arrow presses
        }

        public static bool SendChatMessageMethodPrefix(ref string message, ChatManager __instance)
        {
            if (_backupTextList[0] != message && message.Length <= 350) SaveForUpArrow(message);

            if (message.StartsWith(Command.CmdPrefix))
            {
                FindAndRunCommand(message);
                return false;
            }

            if (ChatCommands.CmdDict["uwu"].IsEnabled && !string.IsNullOrEmpty(message) && Helper.localNetworkPlayer.HasLocalControl)
            {
                if (ChatCommands.CmdDict["nuky"].IsEnabled)
                {
                    message = UwUify(message);
                    Helper.RoutineUsed = WaitCoroutine(message);
                    __instance.StartCoroutine(Helper.RoutineUsed);
                    return false;
                }

                Helper.SendPublicOutput(UwUify(message));
                return false;
            }

            if (ChatCommands.CmdDict["nuky"].IsEnabled)
            {
                if (ChatCommands.CmdDict["lowercase"].IsEnabled)
                    message = message.ToLower();

                Helper.RoutineUsed = WaitCoroutine(message);
                __instance.StartCoroutine(Helper.RoutineUsed);
                return false;
            }

            if (ChatCommands.CmdDict["lowercase"].IsEnabled)
            {
                Helper.localNetworkPlayer.OnTalked(message.ToLower());
                return false;
            }

            return true;
        }

        public static bool ReplaceUnacceptableWordsMethodPrefix(ref string message, ref string __result)
        {
            if (ChatCommands.CmdDict["uncensor"].IsEnabled)
            {
                Debug.Log("skipping censorship");
                __result = message;
                return false;
            }

            Debug.Log("censoring message");
            return true;
        }

        // Method which increases duration of a chat message by set amount in config
        public static void TalkMethodPostfix(ref float ___disableChatIn)
        {
            var extraTime = ConfigHandler.GetEntry<float>("MsgDuration");
            if (extraTime > 0) ___disableChatIn += extraTime;
        }

        private static void FindAndRunCommand(string message)
        {
            Debug.Log("User is trying to run a command...");
            var args = message.TrimStart(Command.CmdPrefix).Trim().Split(' '); // Sanitising input

            var targetCommandTyped = args[0];

            if (!ChatCommands.CmdDict.ContainsKey(targetCommandTyped)) // If command is not found
            {
                Helper.SendModOutput("Specified command or it's alias not found. See /help for full list of commands.",
                    Command.LogType.Warning, false);
                return;
            }

            ChatCommands.CmdDict[targetCommandTyped].Execute(args.Skip(1).ToArray()); // Skip first element (original cmd)
        }

        // Checks if the up-arrow or down-arrow key is pressed, if so then
        // set the chatField.text to whichever message the user stops on
        public static void CheckForArrowKeysAndAutoComplete(TMP_InputField chatField)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) && _upArrowCounter < _backupTextList.Count)
            {
                chatField.text = _backupTextList[_upArrowCounter];
                _upArrowCounter++;

                chatField.DeactivateInputField(); // Necessary to properly update carat pos
                chatField.stringPosition = chatField.text.Length;
                chatField.ActivateInputField();

                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) && _upArrowCounter > 0)
            {
                _upArrowCounter--;
                chatField.text = _backupTextList[_upArrowCounter];

                chatField.DeactivateInputField(); // Necessary to properly update carat pos
                chatField.stringPosition = chatField.text.Length;
                chatField.ActivateInputField();

                return;
            }

            const string rTxtFmt = "<#000000BB><u>";
            var txt = chatField.text;
            var txtLen = txt.Length;
            var parsedTxt = chatField.textComponent.GetParsedText();
            // Remove last char of non-richtext str since a random space is added from GetParsedText() 
            parsedTxt = parsedTxt.Remove(parsedTxt.Length - 1);

            if (txtLen > 0 && txt[0] == Command.CmdPrefix)
            {
                // Credit for this easy way of getting the closest matching string from a list
                //https://forum.unity.com/threads/auto-complete-text-field.142181/#post-1741569
                var cmdsMatched = ChatCommands.CmdNames.FindAll(
                    word => word.StartsWith(parsedTxt, StringComparison.InvariantCultureIgnoreCase));

                if (cmdsMatched.Count > 0)
                {
                    var cmdMatch = cmdsMatched[0];
                    var cmdMatchLen = cmdMatch.Length;

                    if (chatField.richText && parsedTxt.Length == cmdMatchLen)
                    {
                        // Check if cmd has been manually fully typed, if so remove its rich text
                        var richTxtStartPos = txt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                        if (richTxtStartPos != -1 && txt.Substring(0, richTxtStartPos) == cmdMatch)
                        {
                            chatField.text = cmdMatch;
                            return;
                        }

                        if (Input.GetKeyDown(KeyCode.Tab))
                        {
                            chatField.DeactivateInputField(); // Necessary to properly update carat pos
                            chatField.text = cmdMatch;
                            chatField.stringPosition = chatField.text.Length;
                            chatField.ActivateInputField();
                        }

                        return;
                    }

                    chatField.richText = true;
                    chatField.text += txtLen <= cmdMatchLen ? rTxtFmt + cmdMatch.Substring(txtLen) : Command.CmdPrefix;
                }
                else if (chatField.richText)
                { // Already a cmd typed
                    var cmdAndParam = parsedTxt.Split(' ');
                    var cmdDetectedIndex = ChatCommands.CmdNames.IndexOf(cmdAndParam[0]);

                    if (cmdDetectedIndex == -1)
                    {
                        var effectStartPos = txt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                        if (effectStartPos == -1)
                            // This will only occur if a cmd is fully typed and then more chars are added after
                            return;

                        chatField.text = txt.Remove(effectStartPos);
                        return;
                    }

                    var cmdMatch = ChatCommands.CmdNames[cmdDetectedIndex];
                    var targetCmd = ChatCommands.CmdDict[cmdMatch.Substring(1)];
                    var targetCmdParams = targetCmd.AutoParams;

                    if (targetCmdParams == null) return; // Cmd may not take any params
                    if (cmdAndParam.Length <= 1 || cmdAndParam[0].Length != cmdMatch.Length) return;

                    // Focusing on auto-completing the parameter now
                    var paramTxt = cmdAndParam![1].Replace(" ", "");
                    var paramTxtLen = paramTxt.Length;

                    //Debug.Log("paramTxt: \"" + paramTxt + "\"");
                    var paramsMatched = targetCmdParams.FindAll(
                            word => word.StartsWith(paramTxt, StringComparison.InvariantCultureIgnoreCase));

                    // Len check is band-aid so spaces don't break it, this will affect dev on nest parameters if it happens
                    if (paramsMatched.Count > 0 && cmdAndParam.Length < 3)
                    {
                        var paramMatch = paramsMatched[0];
                        var paramMatchLen = paramMatch.Length;

                        if (paramTxtLen == paramMatchLen)
                        {
                            var paramRichTxtStartPos = paramTxt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                            if (paramRichTxtStartPos != -1 && paramTxt.Substring(0, paramRichTxtStartPos) == paramMatch)
                            {
                                chatField.text = chatField.text.Remove(txtLen - paramMatchLen - rTxtFmt.Length + 1, 14);
                                return;
                            }

                            if (Input.GetKeyDown(KeyCode.Tab))
                            {   // Auto-completes the suggested parameter. Input field is made immutable so str pos is set correctly
                                chatField.DeactivateInputField();

                                if (ReferenceEquals(targetCmdParams, PlayerUtils.PlayerColorsParams))
                                {   // Change player color to 1 letter variant to encourage shorthand alternative
                                    // Multiply by 2 to get correct shorthand index for color
                                    var colorIndex = Helper.GetIDFromColor(paramMatch) * 2;
                                    paramMatch = PlayerUtils.PlayerColorsParams[colorIndex];
                                }

                                // string.Remove() so we don't rely on the update loop to remove the rich txt leftovers
                                if (txtLen - paramMatchLen - rTxtFmt.Length > 0) // Actually fixs the bug: the second switch don't work
                                {
                                    chatField.text = txt.Remove(txtLen - paramMatchLen - rTxtFmt.Length) + paramMatch; 
                                }
                                chatField.stringPosition = chatField.text.Length;
                                chatField.ActivateInputField();
                            }

                            return;
                        }

                        chatField.text += rTxtFmt + paramMatch.Substring(paramTxtLen);
                        chatField.richText = true;
                    }
                    else if (chatField.richText) // TODO: Implement support for rich text as argument input
                    {
                        var effectStartPos = txt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                        if (effectStartPos == -1) return;

                        chatField.text = txt.Remove(effectStartPos);
                    }
                }
            }
            else if (chatField.richText)
            {
                var effectStartPos = txt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                if (effectStartPos == -1)
                {
                    // Occurs when a cmd is sent, richtext needs to be reset
                    chatField.richText = false;
                    return;
                }
                chatField.text = txt.Remove(effectStartPos);
                chatField.richText = false;
            }
        }

        // Checks if the message should be inserted then inserts it into the 0th index of backup list
        private static void SaveForUpArrow(string backupThisText)
        {
            if (_backupTextList.Count <= 20)
            {
                _backupTextList.Insert(0, backupThisText);
                return;
            }

            _backupTextList.RemoveAt(19);
            _backupTextList.Insert(0, backupThisText);
        }

        private static IEnumerator WaitCoroutine(string msg)
        {
            var msgParts = msg.Split(' ');

            foreach (var text in msgParts)
            {
                Helper.SendPublicOutput(text);
                yield return new WaitForSeconds(0.45f);
            }
        }

        // UwUifies a message if possible, not perfect
        public static string UwUify(string targetText)
        {
            var i = 0;
            var newMessage = new StringBuilder(targetText);
            while (i < newMessage.Length)
            {
                if (!char.IsLetter(newMessage[i]))
                {
                    i++;
                    continue;
                }

                var c = char.ToLower(newMessage[i]);
                var nextC = i < newMessage.Length - 1 ? char.ToLower(newMessage[i + 1]) : '\0';

                switch (c)
                {
                    case 'r' or 'l':
                        newMessage[i] = char.IsUpper(newMessage[i]) ? 'W' : 'w';
                        break;
                    case 't' when nextC == 'h':
                        newMessage[i] = char.IsUpper(newMessage[i]) ? 'D' : 'd';
                        newMessage.Remove(i + 1, 1);
                        break;
                    case 'n' when nextC != ' ' && nextC != 'g' && nextC != 't' && nextC != 'd':
                        newMessage.Insert(i + 1, char.IsUpper(newMessage[i]) ? 'Y' : 'y');
                        break;
                    default:
                        if (Helper.IsVowel(c) && nextC == 't')
                            newMessage.Insert(i + 1, char.IsUpper(newMessage[i]) ? 'W' : 'w');
                        break;
                }
                i++;
            }

            return newMessage.ToString();
        }

        public static bool m_NetworkPlayer;

        private static int _upArrowCounter; // Holds how many times the up-arrow key is pressed while typing
                                            //private static bool _startedTypingParam;

        // List to contain previous messages sent by us (up to 20)
        private static List<string> _backupTextList = new(21)
        {
            "" // has an empty string so that the list isn't null when attempting to perform on it
        };
    }
}