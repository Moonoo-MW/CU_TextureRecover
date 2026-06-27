// ------------------------------------------------------------------------------
// GuiReplacer - Runtime Texture2D replacement framework for Unity Mono games.
// This file wraps BepInEx logging so the rest of the project does not log directly.
// ------------------------------------------------------------------------------

using BepInEx.Logging;
using System;

namespace GuiReplacer
{
    /// <summary>
    /// Centralized logging helper for GuiReplacer.
    /// </summary>
    public sealed class GuiLogger
    {
        private static GuiLogger _instance;
        private ManualLogSource _source;

        private GuiLogger()
        {
        }

        /// <summary>
        /// Gets the global logger instance.
        /// </summary>
        public static GuiLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GuiLogger();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Initializes the logger with BepInEx's plugin log source.
        /// </summary>
        /// <param name="source">The BepInEx log source.</param>
        public void Initialize(ManualLogSource source)
        {
            _source = source;
        }

        /// <summary>
        /// Writes an informational message when logging is enabled.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public void Info(string message)
        {
            if (CanLog())
            {
                _source.LogInfo(message);
            }
        }

        /// <summary>
        /// Writes a warning message when logging is enabled.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public void Warning(string message)
        {
            if (CanLog())
            {
                _source.LogWarning(message);
            }
        }

        /// <summary>
        /// Writes an error message when logging is enabled.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public void Error(string message)
        {
            if (CanLog())
            {
                _source.LogError(message);
            }
        }

        /// <summary>
        /// Writes an exception when logging is enabled.
        /// </summary>
        /// <param name="context">The operation being performed.</param>
        /// <param name="exception">The exception to write.</param>
        public void Exception(string context, Exception exception)
        {
            if (CanLog())
            {
                _source.LogError(context + ": " + exception);
            }
        }

        private bool CanLog()
        {
            return _source != null && Config.Instance.EnableLog;
        }
    }
}
