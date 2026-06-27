// ------------------------------------------------------------------------------
// GuiReplacer - Runtime Texture2D replacement framework for Unity Mono games.
// This file is the BepInEx entry point and orchestrates the replacement pipeline.
// ------------------------------------------------------------------------------

using BepInEx;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiReplacer
{
    /// <summary>
    /// BepInEx plugin entry point for runtime GUI texture replacement.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        /// <summary>
        /// The stable BepInEx plugin GUID.
        /// </summary>
        public const string PluginGuid = "com.moonoo.guireplacer";

        /// <summary>
        /// The display name shown by BepInEx.
        /// </summary>
        public const string PluginName = "GuiReplacer";

        /// <summary>
        /// The plugin version.
        /// </summary>
        public const string PluginVersion = "1.0.0";

        private static Plugin _instance;

        /// <summary>
        /// Gets the active plugin instance.
        /// </summary>
        public static Plugin Instance { get { return _instance; } }

        private void Awake()
        {
            _instance = this;
            Config.Instance.Initialize(Config);
            GuiLogger.Instance.Initialize(Logger);
            GuiLogger.Instance.Info("GuiReplacer Loaded");

            if (!Config.Instance.Enable)
            {
                GuiLogger.Instance.Info("GuiReplacer disabled by config");
                return;
            }

            RunReplacementPipeline();

            if (Config.Instance.EnableDump)
            {
                TextureManager.Instance.DumpAllTextures();
                Config.Save();
            }
        }

        private void Update()
        {
            HotReload.Instance.Update();
        }

        /// <summary>
        /// Rebuilds texture lookup data, scans replacement PNG files, and applies all matches.
        /// </summary>
        public void RunReplacementPipeline()
        {
            try
            {
                TextureManager.Instance.RebuildIndex();
                List<string> pngFiles = TextureLoader.Instance.ScanPngFiles();

                for (int index = 0; index < pngFiles.Count; index++)
                {
                    ApplySingleFile(pngFiles[index]);
                }
            }
            catch (Exception exception)
            {
                GuiLogger.Instance.Exception("Replacement pipeline failed", exception);
            }
        }

        private void ApplySingleFile(string filePath)
        {
            string key = TextureLoader.Instance.GetTextureKey(filePath);
            Texture2D target;
            if (!TextureManager.Instance.TryGetTexture(key, out target))
            {
                GuiLogger.Instance.Warning("Texture Not Found : " + key);
                GuiLogger.Instance.Warning("Missing Texture : " + key);
                return;
            }

            Texture2D source = TextureLoader.Instance.LoadPng(filePath);
            if (source == null)
            {
                GuiLogger.Instance.Error("Replace Failed : " + key + " PNG load failed");
                return;
            }

            try
            {
                TextureReplacer.Instance.Replace(target, source);
            }
            finally
            {
                UnityEngine.Object.Destroy(source);
            }
        }
    }
}
