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
        public bool Replace(Texture2D target, Texture2D source)
        {
            if (target == null || source == null)
            {
                GuiLogger.Instance.Error("Replace Failed : null texture");
                return false;
            }

            LogTextureDetails("Replace Begin", target);
            if (!target.isReadable)
            {
                GuiLogger.Instance.Warning("Target texture is not readable before replacement. CPU overwrite will attempt Resize/Reinitialize path. Name=" + target.name + " InstanceID=" + target.GetInstanceID());
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

            Texture2D preparedSource = source;
            try
            {
                if (sizeMismatch)
                {
                    preparedSource = CreateReadableCopy(source, target.width, target.height);
                    if (preparedSource == null)
                    {
                        GuiLogger.Instance.Error("Replace Failed : " + target.name + " resize failed");
                        return false;
                    }
                }

                Color32[] replacementPixels = preparedSource.GetPixels32();
                if (!TryCpuReplace(target, replacementPixels, preparedSource.width, preparedSource.height))
                {
                    return false;
                }

                if (!VerifyPixels(target, replacementPixels))
                {
                    GuiLogger.Instance.Error("Replace Failed : " + target.name + " Pixel verification failed.");
                    LogTextureDetails("Replace Failed", target);
                    return false;
                }

                LogTextureDetails("Replace Success", target);
                return true;
            }
            catch (Exception exception)
            {
                GuiLogger.Instance.Exception("Replace Failed : " + target.name, exception);
                return false;
            }
            finally
            {
                if (preparedSource != source && preparedSource != null)
                {
                    UnityEngine.Object.Destroy(preparedSource);
                }
            }
        }

        /// <summary>
        /// Creates a readable RGBA32 copy of a texture, optionally scaled to a new size.
        /// </summary>
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

        private bool TryCpuReplace(Texture2D target, Color32[] pixels, int width, int height)
        {
            try
            {
                if (target.width != width || target.height != height || !target.isReadable || !IsCpuWritableFormat(target.format))
                {
                    if (!target.Resize(width, height, TextureFormat.RGBA32, target.mipmapCount > 1))
                    {
                        GuiLogger.Instance.Error("Replace Failed : " + target.name + " Resize/Reinitialize returned false");
                        return false;
                    }
                }

                target.SetPixels32(pixels);
                target.Apply(false, false);
                return true;
            }
            catch (Exception exception)
            {
                GuiLogger.Instance.Exception("CPU texture replacement failed: " + target.name, exception);
                return false;
            }
        }

        private bool VerifyPixels(Texture2D target, Color32[] expectedPixels)
        {
            try
            {
                Color32[] actualPixels = target.GetPixels32();
                if (actualPixels == null || actualPixels.Length != expectedPixels.Length)
                {
                    GuiLogger.Instance.Error("Pixel verification failed: pixel count mismatch target=" + (actualPixels == null ? 0 : actualPixels.Length) + " expected=" + expectedPixels.Length);
                    return false;
                }

                int[] samples = new int[] { 0, expectedPixels.Length / 4, expectedPixels.Length / 2, (expectedPixels.Length * 3) / 4, expectedPixels.Length - 1 };
                for (int index = 0; index < samples.Length; index++)
                {
                    int sampleIndex = samples[index];
                    if (!actualPixels[sampleIndex].Equals(expectedPixels[sampleIndex]))
                    {
                        GuiLogger.Instance.Error("Pixel verification failed at " + sampleIndex + " expected=" + expectedPixels[sampleIndex] + " actual=" + actualPixels[sampleIndex]);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                GuiLogger.Instance.Exception("Pixel verification failed for " + target.name, exception);
                return false;
            }
        }

        private bool IsCpuWritableFormat(TextureFormat format)
        {
            return format == TextureFormat.Alpha8 ||
                   format == TextureFormat.ARGB4444 ||
                   format == TextureFormat.RGB24 ||
                   format == TextureFormat.RGBA32 ||
                   format == TextureFormat.ARGB32 ||
                   format == TextureFormat.RGB565 ||
                   format == TextureFormat.RGBA4444 ||
                   format == TextureFormat.BGRA32;
        }

        private void LogTextureDetails(string prefix, Texture2D texture)
        {
            GuiLogger.Instance.Info(prefix + " : " + texture.name + " Name=" + texture.name + " InstanceID=" + texture.GetInstanceID() + " Size=" + texture.width + "x" + texture.height + " Format=" + texture.format + " MipMap=" + texture.mipmapCount + " Readable=" + texture.isReadable);
        }
    }
}
