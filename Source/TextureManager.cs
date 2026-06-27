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
    /// Maintains live Texture2D objects keyed by Texture.name.
    /// </summary>
    public sealed class TextureManager
    {
        private static TextureManager _instance;
        private Dictionary<string, List<Texture2D>> _textures;
        private Texture2D[] _allTextures;

        private TextureManager()
        {
            _textures = new Dictionary<string, List<Texture2D>>(StringComparer.OrdinalIgnoreCase);
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
            _textures = new Dictionary<string, List<Texture2D>>(comparer);

            _allTextures = Resources.FindObjectsOfTypeAll<Texture2D>();
            for (int index = 0; index < _allTextures.Length; index++)
            {
                Texture2D texture = _allTextures[index];
                if (texture == null || string.IsNullOrEmpty(texture.name))
                {
                    continue;
                }

                List<Texture2D> instances;
                if (!_textures.TryGetValue(texture.name, out instances))
                {
                    instances = new List<Texture2D>();
                    _textures.Add(texture.name, instances);
                }

                instances.Add(texture);
            }

            foreach (KeyValuePair<string, List<Texture2D>> pair in _textures)
            {
                if (pair.Value.Count > 1)
                {
                    GuiLogger.Instance.Warning("Duplicate Texture Name : " + pair.Key + " Found " + pair.Value.Count + " instances");
                }
            }

            GuiLogger.Instance.Info("Found " + _allTextures.Length + " Texture2D, indexed " + _textures.Count + " names");
        }

        /// <summary>
        /// Attempts to find all live Texture2D objects by Texture.name.
        /// </summary>
        public bool TryGetTextures(string name, out List<Texture2D> textures)
        {
            return _textures.TryGetValue(name, out textures) && textures.Count > 0;
        }

        /// <summary>
        /// Dumps verbose Texture, Sprite, and Material relationships for debugging displayed UI references.
        /// </summary>
        public void DumpVerboseReferences()
        {
            DumpTextures();
            DumpSprites();
            DumpMaterials();
        }

        /// <summary>
        /// Dumps all indexed textures to GuiReplacer/Cache/GUI_Dump and disables EnableDump afterwards.
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

        private void DumpTextures()
        {
            for (int index = 0; index < _allTextures.Length; index++)
            {
                Texture2D texture = _allTextures[index];
                if (texture != null)
                {
                    GuiLogger.Instance.Info("Texture Name=" + texture.name + " InstanceID=" + texture.GetInstanceID() + " Size=" + texture.width + "x" + texture.height + " Format=" + texture.format + " MipMap=" + texture.mipmapCount + " Readable=" + texture.isReadable);
                }
            }
        }

        private void DumpSprites()
        {
            Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            for (int index = 0; index < sprites.Length; index++)
            {
                Sprite sprite = sprites[index];
                if (sprite != null && sprite.texture != null)
                {
                    Texture2D texture = sprite.texture;
                    GuiLogger.Instance.Info("Sprite " + sprite.name + " -> Texture " + texture.name + " InstanceID=" + texture.GetInstanceID());
                }
            }
        }

        private void DumpMaterials()
        {
            Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
            for (int index = 0; index < materials.Length; index++)
            {
                Material material = materials[index];
                if (material == null || !material.HasProperty("_MainTex"))
                {
                    continue;
                }

                Texture texture = material.mainTexture;
                if (texture != null)
                {
                    GuiLogger.Instance.Info("Material " + material.name + " -> Texture " + texture.name + " InstanceID=" + texture.GetInstanceID());
                }
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
