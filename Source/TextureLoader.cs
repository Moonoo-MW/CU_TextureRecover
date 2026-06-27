// ------------------------------------------------------------------------------
// GuiReplacer - Runtime Texture2D replacement framework for Unity Mono games.
// This file scans replacement PNG files and loads them into temporary textures.
// ------------------------------------------------------------------------------

using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GuiReplacer
{
    /// <summary>
    /// Scans and loads PNG files from the configured plugin folder.
    /// </summary>
    public sealed class TextureLoader
    {
        private static TextureLoader _instance;

        private TextureLoader()
        {
        }

        /// <summary>
        /// Gets the global texture loader instance.
        /// </summary>
        public static TextureLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TextureLoader();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Gets the absolute path to the configured replacement folder.
        /// </summary>
        public string ModsGuiPath
        {
            get { return Path.Combine(Paths.PluginPath, Config.Instance.ModsFolder.Replace('/', Path.DirectorySeparatorChar)); }
        }

        /// <summary>
        /// Gets the absolute path to the debug texture dump folder.
        /// </summary>
        public string DumpPath
        {
            get { return Path.Combine(Paths.PluginPath, "GuiReplacer", "Cache", "GUI_Dump"); }
        }


        /// <summary>
        /// Creates the plugin-owned GuiReplacer resource directories.
        /// </summary>
        public void EnsurePluginDirectories()
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(Paths.PluginPath, "GuiReplacer"));
                Directory.CreateDirectory(ModsGuiPath);
                Directory.CreateDirectory(Path.Combine(Paths.PluginPath, "GuiReplacer", "Cache"));
                Directory.CreateDirectory(Path.Combine(Paths.PluginPath, "GuiReplacer", "Config"));
            }
            catch (Exception exception)
            {
                GuiLogger.Instance.Exception("Directory initialization failed", exception);
            }
        }

        /// <summary>
        /// Recursively or non-recursively scans configured GuiReplacer/GUI for PNG files.
        /// </summary>
        /// <returns>A list of PNG file paths.</returns>
        public List<string> ScanPngFiles()
        {
            List<string> files = new List<string>();
            string folder = ModsGuiPath;

            try
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                SearchOption option = Config.Instance.RecursiveScan ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                string[] discoveredFiles = Directory.GetFiles(folder, "*.png", option);
                for (int index = 0; index < discoveredFiles.Length; index++)
                {
                    files.Add(discoveredFiles[index]);
                }
            }
            catch (Exception exception)
            {
                GuiLogger.Instance.Exception("PNG scan failed", exception);
            }

            GuiLogger.Instance.Info("Found " + files.Count + " PNG");
            return files;
        }

        /// <summary>
        /// Loads a PNG file into a temporary readable RGBA32 Texture2D.
        /// </summary>
        /// <param name="filePath">The PNG path.</param>
        /// <returns>A loaded temporary texture, or null when loading fails.</returns>
        public Texture2D LoadPng(string filePath)
        {
            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.name = Path.GetFileNameWithoutExtension(filePath);

                if (!ImageConversion.LoadImage(texture, data, false))
                {
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                return texture;
            }
            catch (Exception exception)
            {
                GuiLogger.Instance.Exception("Load PNG failed: " + filePath, exception);
                return null;
            }
        }

        /// <summary>
        /// Returns the texture key implied by a PNG filename.
        /// </summary>
        /// <param name="filePath">The PNG path.</param>
        /// <returns>The filename without extension.</returns>
        public string GetTextureKey(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }
    }
}
