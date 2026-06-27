// ------------------------------------------------------------------------------
// GuiReplacer - Runtime Texture2D replacement framework for Unity Mono games.
// This file indexes Unity Texture2D objects and optionally dumps them for modders.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GuiReplacer
{
    /// <summary>
    /// Maintains a dictionary of live Texture2D objects keyed by Texture.name.
    /// </summary>
    public sealed class TextureManager
    {
        private static TextureManager _instance;
        private Dictionary<string, Texture2D> _textures;
        private Texture2D[] _allTextures;

        private TextureManager()
        {
            _textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
            _allTextures = new Texture2D[0];
        }

        /// <summary>
        /// Gets the global texture manager instance.
        /// </summary>
        public static TextureManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TextureManager();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Rebuilds the Texture.name lookup table using Resources.FindObjectsOfTypeAll.
        /// </summary>
        public void RebuildIndex()
        {
            StringComparer comparer = Config.Instance.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            _textures = new Dictionary<string, Texture2D>(comparer);

            _allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
            for (int index = 0; index < _allTextures.Length; index++)
            {
                Texture2D texture = _allTextures[index];
                if (texture == null || string.IsNullOrEmpty(texture.name))
                {
                    continue;
                }

                if (_textures.ContainsKey(texture.name))
                {
                    GuiLogger.Instance.Warning("Duplicate Texture Name : " + texture.name + " (InstanceID " + texture.GetInstanceID() + ")");
                    continue;
                }

                _textures.Add(texture.name, texture);
            }

            GuiLogger.Instance.Info("Found " + _textures.Count + " Texture2D");
        }

        /// <summary>
        /// Attempts to find a live Texture2D by Texture.name.
        /// </summary>
        /// <param name="name">The texture name.</param>
        /// <param name="texture">The found texture.</param>
        /// <returns>True when the texture exists.</returns>
        public bool TryGetTexture(string name, out Texture2D texture)
        {
            return _textures.TryGetValue(name, out texture);
        }

        /// <summary>
        /// Dumps all indexed textures to Mods/GUI_Dump and disables EnableDump afterwards.
        /// </summary>
        public void DumpAllTextures()
        {
            try
            {
                string folder = TextureLoader.Instance.DumpPath;
                Directory.CreateDirectory(folder);

                for (int index = 0; index < _allTextures.Length; index++)
                {
                    Texture2D texture = _allTextures[index];
                    if (texture == null || string.IsNullOrEmpty(texture.name))
                    {
                        continue;
                    }

                    Texture2D readable = TextureReplacer.Instance.CreateReadableCopy(texture, texture.width, texture.height);
                    if (readable == null)
                    {
                        continue;
                    }

                    string safeName = GetSafeFileName(texture.name);
                    string path = Path.Combine(folder, safeName + ".png");
                    if (File.Exists(path))
                    {
                        path = Path.Combine(folder, safeName + "_" + texture.GetInstanceID() + ".png");
                    }

                    File.WriteAllBytes(path, ImageConversion.EncodeToPNG(readable));
                    UnityEngine.Object.Destroy(readable);
                }

                GuiLogger.Instance.Info("Texture dump completed: " + folder);
            }
            catch (Exception exception)
            {
                GuiLogger.Instance.Exception("Texture dump failed", exception);
            }
            finally
            {
                Config.Instance.EnableDump = false;
            }
        }

        private string GetSafeFileName(string name)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string safeName = name;
            for (int index = 0; index < invalidChars.Length; index++)
            {
                safeName = safeName.Replace(invalidChars[index], '_');
            }

            return safeName;
        }
    }
}
