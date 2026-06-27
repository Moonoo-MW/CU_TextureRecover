// ------------------------------------------------------------------------------
// GuiReplacer - Runtime Texture2D replacement framework for Unity Mono games.
// This file mutates existing Texture2D instances without replacing object references.
// ------------------------------------------------------------------------------

using System;
using UnityEngine;

namespace GuiReplacer
{
    /// <summary>
    /// Performs in-place Texture2D replacement while preserving existing references.
    /// </summary>
    public sealed class TextureReplacer
    {
        private static TextureReplacer _instance;

        private TextureReplacer()
        {
        }

        /// <summary>
        /// Gets the global texture replacer instance.
        /// </summary>
        public static TextureReplacer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TextureReplacer();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Applies a source PNG texture into an existing target Texture2D.
        /// </summary>
        /// <param name="target">The existing game texture to mutate.</param>
        /// <param name="source">The temporary replacement texture.</param>
        /// <returns>True when the replacement succeeds.</returns>
        public bool Replace(Texture2D target, Texture2D source)
        {
            if (target == null || source == null)
            {
                GuiLogger.Instance.Error("Replace Failed : null texture");
                return false;
            }

            bool sizeMismatch = target.width != source.width || target.height != source.height;
            if (sizeMismatch)
            {
                GuiLogger.Instance.Warning("Texture Size Mismatch : " + target.name + " target=" + target.width + "x" + target.height + " png=" + source.width + "x" + source.height);
                if (!Config.Instance.AllowSizeMismatch)
                {
                    GuiLogger.Instance.Error("Replace Failed : " + target.name + " size mismatch is disabled");
                    return false;
                }
            }

            try
            {
                Texture2D preparedSource = source;
                if (sizeMismatch)
                {
                    preparedSource = CreateReadableCopy(source, target.width, target.height);
                    if (preparedSource == null)
                    {
                        GuiLogger.Instance.Error("Replace Failed : " + target.name + " resize failed");
                        return false;
                    }
                }

                bool copied = TryCopyTexture(target, preparedSource);
                if (!copied)
                {
                    Color32[] pixels = preparedSource.GetPixels32();
                    target.SetPixels32(pixels);
                    target.Apply(false, false);
                }

                if (preparedSource != source)
                {
                    UnityEngine.Object.Destroy(preparedSource);
                }

                GuiLogger.Instance.Info("Replace Success : " + target.name);
                return true;
            }
            catch (Exception exception)
            {
                GuiLogger.Instance.Exception("Replace Failed : " + target.name, exception);
                return false;
            }
        }

        /// <summary>
        /// Creates a readable RGBA32 copy of a texture, optionally scaled to a new size.
        /// </summary>
        /// <param name="source">The source texture.</param>
        /// <param name="width">The output width.</param>
        /// <param name="height">The output height.</param>
        /// <returns>A readable RGBA32 texture, or null when conversion fails.</returns>
        public Texture2D CreateReadableCopy(Texture source, int width, int height)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture renderTexture = null;
            Texture2D readable = null;

            try
            {
                renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                Graphics.Blit(source, renderTexture);
                RenderTexture.active = renderTexture;

                readable = new Texture2D(width, height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readable.Apply(false, false);
                readable.name = source.name;
                return readable;
            }
            catch (Exception exception)
            {
                GuiLogger.Instance.Exception("Create readable texture failed", exception);
                if (readable != null)
                {
                    UnityEngine.Object.Destroy(readable);
                }

                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                if (renderTexture != null)
                {
                    RenderTexture.ReleaseTemporary(renderTexture);
                }
            }
        }

        private bool TryCopyTexture(Texture2D target, Texture2D source)
        {
            try
            {
                Graphics.CopyTexture(source, 0, 0, target, 0, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
