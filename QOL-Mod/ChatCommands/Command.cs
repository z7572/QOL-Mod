﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QOL {

    public class Command
    {
        public bool IsPublic
        {
            get => _isPublic;
            set
            {
                if (AlwaysPublic || AlwaysPrivate)
                {
                    Debug.LogWarning("Cannot modify cmd visibility once it has been set always public/private!");
                    return;
                }

                _isPublic = value;
            }
        }

        public string Name { get; }
        public List<string> Aliases { get; } = new();
        // TODO: Implement auto-suggested parameters property
        //public List<string> AutoParams { get; }
        public object AutoParams { get; private set; }
        public bool IsToggle { get; private set; }
        public bool IsEnabled { get; set; }

        public static char CmdPrefix = ConfigHandler.GetEntry<string>("CommandPrefix").Length == 1
            ? ConfigHandler.GetEntry<string>("CommandPrefix")[0]
            : '/';

        private readonly Action<string[], Command> _runCmdAction; // Use Action as method will never return anything
        private readonly int _minExpectedArgs; // Minimal # of args required for cmd to function
        private bool _isPublic;
        public bool AlwaysPublic;
        public bool AlwaysPrivate;

        private static string _currentOutputMsg;
        private static LogType _currentLogType; // Any mod msg will be of type "success" by default

        public Command(string name, Action<string[], Command> cmdMethod, int minNumExpectedArgs, bool defaultPrivate,
            object autoParameters = null)
        {
            Name = CmdPrefix + name;
            _runCmdAction = cmdMethod;
            _minExpectedArgs = minNumExpectedArgs;

            // Compatible with old structure List<string> autocompletion
            if (autoParameters is List<string> simpleAutoParams)
            {
                AutoParams = simpleAutoParams;
            }
            else if (autoParameters is List<List<string>> AutoParamsByIndex)
            {
                AutoParams = AutoParamsByIndex;
                    //.SelectMany((paramList, index) => paramList.Select(param => new KeyValuePair<string, int>(param, index)))
                    //.ToDictionary(pair => pair.Key, pair => pair.Value); // Dictionary<string, int>: {param, index}
            }
            else if (autoParameters is Dictionary<string, object> AutoParamsByName)
            {
                AutoParams = AutoParamsByName;
            }
            else
            {
                AutoParams = null;
            }

            IsPublic = !defaultPrivate;
        }

        // Private as there has been no cases where this type of visibility was necessary and the cmd was not a toggle
        private void SetAlwaysPrivate()
        {
            if (AlwaysPublic)
            {
                Debug.LogWarning("Cmd is already always public, cannot modify this!");
                return;
            }

            AlwaysPrivate = true;
            IsPublic = false;
        }

        public Command SetAlwaysPublic()
        {
            if (AlwaysPrivate)
            {
                Debug.LogWarning("Cmd is already always private, cannot modify this!");
                return this;
            }

            AlwaysPublic = true;
            IsPublic = true;
            return this;
        }

        public Command MarkAsToggle()
        {
            IsToggle = true;
            return this;
        }

        public void SetOutputMsg(string msg) => _currentOutputMsg = msg;
        public void SetLogType(LogType type) => _currentLogType = type;
        public void Toggle() => IsEnabled = !IsEnabled;

        public void Execute(params string[] args)
        {
            if (args.Length < _minExpectedArgs)
            {
                _currentLogType = LogType.Warning;
                _currentOutputMsg = "Invalid # of arguments specified. See /help for more info.";
                Helper.SendModOutput(_currentOutputMsg, _currentLogType, false);

                _currentLogType = LogType.Success;
                _currentOutputMsg = ""; // In case next cmd has no output 
                return;
            }

            try
            {
                _runCmdAction(args, this);
            }
            catch (Exception e)
            {
                Debug.LogError("Exception occured when running command: " + e);

                // _currentOutputMsg = "Something went wrong! DM Monky#4600 if bug.";
                _currentOutputMsg = e.Message;
                Helper.SendModOutput(_currentOutputMsg, LogType.Warning, false);
                _currentOutputMsg = "";
                throw;
            }

            if (string.IsNullOrEmpty(_currentOutputMsg)) // Some cmds may not have any output at all
                return;

            if (_currentLogType == LogType.Warning) // All warning msg's should be client-side
            {
                Helper.SendModOutput(_currentOutputMsg, LogType.Warning, false);
                _currentLogType = LogType.Success;
                _currentOutputMsg = "";
                return;
            }

            Helper.SendModOutput(_currentOutputMsg, LogType.Success, !IsToggle && IsPublic, !IsToggle || IsEnabled);
            _currentLogType = LogType.Success;
            _currentOutputMsg = "";
        }

        public enum LogType
        {
            Success,
            Warning
        }
    }
}