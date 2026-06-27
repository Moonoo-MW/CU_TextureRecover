// ------------------------------------------------------------------------------
// GuiReplacer - Runtime Texture2D replacement framework for Unity Mono games.
// This file is the BepInEx entry point and orchestrates the replacement pipeline.
// ------------------------------------------------------------------------------

using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private Coroutine _scheduledPipeline;

        /// <summary>
        /// Gets the active plugin instance.
        /// </summary>
        public static Plugin Instance { get { return _instance; } }

        private void Awake()
        {
            _instance = this;
            GuiReplacer.Config.Instance.Initialize(Config);
            GuiLogger.Instance.Initialize(Logger);
            GuiLogger.Instance.Info("GuiReplacer Loaded");

            if (!GuiReplacer.Config.Instance.Enable)
            {
                GuiLogger.Instance.Info("GuiReplacer disabled by config");
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            ScheduleReplacementPipeline("plugin awake", GuiReplacer.Config.Instance.InitialScanDelaySeconds);

            if (GuiReplacer.Config.Instance.EnableDump)
            {
                TextureManager.Instance.DumpAllTextures();
                Config.Save();
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            HotReload.Instance.Update();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!GuiReplacer.Config.Instance.Enable)
            {
                return;
            }

            GuiLogger.Instance.Info("Scene loaded: " + scene.name + " (" + mode + ")");
            ScheduleReplacementPipeline("scene loaded: " + scene.name, GuiReplacer.Config.Instance.SceneScanDelaySeconds);
        }

        /// <summary>
        /// Schedules a delayed replacement pass so scene UI assets have time to load.
        /// </summary>
        /// <param name="reason">The reason for scheduling.</param>
        /// <param name="delaySeconds">The delay in seconds.</param>
        public void ScheduleReplacementPipeline(string reason, float delaySeconds)
        {
            if (_scheduledPipeline != null)
            {
                StopCoroutine(_scheduledPipeline);
            }

            _scheduledPipeline = StartCoroutine(RunReplacementPipelineDelayed(reason, delaySeconds));
        }

        private IEnumerator RunReplacementPipelineDelayed(string reason, float delaySeconds)
        {
            if (delaySeconds > 0f)
            {
                GuiLogger.Instance.Info("Replacement pipeline scheduled after " + delaySeconds.ToString("0.###") + "s: " + reason);
                yield return new WaitForSeconds(delaySeconds);
            }

            _scheduledPipeline = null;
            RunReplacementPipeline();
        }

        /// <summary>
        /// Rebuilds texture lookup data, scans replacement PNG files, and applies all matches.
        /// </summary>
        public void RunReplacementPipeline()
        {
            try
            {
                TextureManager.Instance.RebuildIndex();
                if (GuiReplacer.Config.Instance.EnableVerboseLog)
                {
                    TextureManager.Instance.DumpVerboseReferences();
                }

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
            List<Texture2D> targets;
            if (!TextureManager.Instance.TryGetTextures(key, out targets))
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
                int successCount = 0;
                GuiLogger.Instance.Info(key + " Found " + targets.Count + " instances");
                for (int index = 0; index < targets.Count; index++)
                {
                    if (TextureReplacer.Instance.Replace(targets[index], source))
                    {
                        successCount++;
                    }
                }

                GuiLogger.Instance.Info(key + " Replace " + successCount + "/" + targets.Count);
            }
            finally
            {
                UnityEngine.Object.Destroy(source);
            }
        }
    }
}
