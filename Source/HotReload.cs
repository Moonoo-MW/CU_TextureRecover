// ------------------------------------------------------------------------------
// GuiReplacer - Runtime Texture2D replacement framework for Unity Mono games.
// This file handles user-triggered reloads without scanning every frame.
// ------------------------------------------------------------------------------

using UnityEngine;

namespace GuiReplacer
{
    /// <summary>
    /// Checks for the configured hot reload key and invokes replacement on demand.
    /// </summary>
    public sealed class HotReload
    {
        private static HotReload _instance;

        private HotReload()
        {
        }

        /// <summary>
        /// Gets the global hot reload instance.
        /// </summary>
        public static HotReload Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new HotReload();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Polls F8 once per frame and starts a reload only when pressed.
        /// </summary>
        public void Update()
        {
            if (!Config.Instance.Enable || !Config.Instance.EnableHotReload)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                GuiLogger.Instance.Info("Hot Reload Triggered");
                Plugin.Instance.RunReplacementPipeline();
                if (Overlay.Instance != null)
                {
                    Overlay.Instance.ShowReload("Reload Texture Success");
                }
            }
        }
    }
}
