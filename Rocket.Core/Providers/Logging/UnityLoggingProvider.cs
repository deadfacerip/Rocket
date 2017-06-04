﻿using System;
using Rocket.API.Providers.Logging;
using Debug = UnityEngine.Debug;

namespace Rocket.Core.Providers.Logging
{
    public class UnityLoggingProvider: IRocketLoggingProvider
    {
        public bool EchoNativeOutput { get; } = false;

        public void Unload(bool isReload = false)
        {
            
        }

        public void Load(bool isReload = false)
        {

        }

        public void Log(LogLevel level, object message, Exception exception = null, ConsoleColor? color = null)
        {
            object msg = (message ?? "") + (exception?.ToString() ?? "");

            if (message == null)
            {
                msg = exception;
            }

            msg = $"[{level}] {msg}";
            
            switch (level)
            {
                case LogLevel.INFO:
                case LogLevel.DEBUG:
                    Debug.Log(msg);
                    break;

                case LogLevel.WARN:
                    Debug.LogWarning(msg);
                    break;
                case LogLevel.FATAL:
                case LogLevel.ERROR:
                    Debug.LogError(msg);
                    break;
            }
        }

        public void Log(LogLevel level, Exception exception, ConsoleColor? color = null)
        {
            Log(level, null, exception, color);
        }

        public void LogMessage(LogLevel level, object message, ConsoleColor? color = null)
        {
            Log(level, message, null, color);
        }
    }
}