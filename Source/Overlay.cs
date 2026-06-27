// ------------------------------------------------------------------------------
// GuiReplacer - Runtime Texture2D replacement framework for Unity Mono games.
// This file draws the non-interactive startup and hot-reload overlay with IMGUI.
// ------------------------------------------------------------------------------

using UnityEngine;

namespace GuiReplacer
{
    /// <summary>
    /// Draws a small, non-interactive IMGUI overlay with fade animation.
    /// </summary>
    public sealed class Overlay : MonoBehaviour
    {
        private const float DefaultWidth = 260f;
        private const float DefaultHeight = 40f;
        private const float Margin = 20f;
        private const float ReloadDurationSeconds = 2f;
        private const float DefaultFadeOutSeconds = 0.5f;

        private static Overlay _instance;

        private string _message;
        private float _showStartedAt;
        private float _holdDuration;
        private float _fadeInDuration;
        private float _fadeOutDuration;
        private bool _visible;
        private bool _initialized;
        private GUIStyle _labelStyle;
        private Texture2D _backgroundTexture;

        /// <summary>
        /// Gets the active overlay instance.
        /// </summary>
        public static Overlay Instance { get { return _instance; } }

        /// <summary>
        /// Attaches the overlay component to the plugin GameObject when enabled.
        /// </summary>
        /// <param name="host">The BepInEx plugin GameObject.</param>
        public static void Initialize(GameObject host)
        {
            if (!Config.Instance.EnableOverlay || host == null)
            {
                return;
            }

            if (_instance == null)
            {
                _instance = host.GetComponent<Overlay>();
                if (_instance == null)
                {
                    _instance = host.AddComponent<Overlay>();
                }
            }

            _instance.InitializeResources();
            GuiLogger.Instance.Info("Overlay Initialized");
        }

        /// <summary>
        /// Shows a startup notification using configured timing.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public void Show(string message)
        {
            Show(message, Config.Instance.OverlayDuration, Config.Instance.OverlayFadeTime, DefaultFadeOutSeconds);
        }

        /// <summary>
        /// Shows a reload notification for two seconds using the configured fade-in time.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public void ShowReload(string message)
        {
            Show(message, ReloadDurationSeconds, Config.Instance.OverlayFadeTime, DefaultFadeOutSeconds);
        }

        /// <summary>
        /// Shows the overlay with explicit animation timing.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="holdDuration">Seconds to remain fully visible.</param>
        /// <param name="fadeInDuration">Seconds to fade in.</param>
        /// <param name="fadeOutDuration">Seconds to fade out.</param>
        public void Show(string message, float holdDuration, float fadeInDuration, float fadeOutDuration)
        {
            if (!Config.Instance.EnableOverlay)
            {
                return;
            }

            InitializeResources();
            _message = message;
            _holdDuration = Mathf.Max(0f, holdDuration);
            _fadeInDuration = Mathf.Max(0.01f, fadeInDuration);
            _fadeOutDuration = Mathf.Max(0.01f, fadeOutDuration);
            _showStartedAt = Time.realtimeSinceStartup;
            _visible = true;
            GuiLogger.Instance.Info("Overlay Show");
        }

        private void OnGUI()
        {
            if (!Config.Instance.EnableOverlay || !_visible)
            {
                return;
            }

            if (Event.current != null && Event.current.type != EventType.Repaint)
            {
                return;
            }

            float alpha = GetAlpha(Time.realtimeSinceStartup - _showStartedAt);
            if (alpha <= 0f)
            {
                Hide();
                return;
            }

            InitializeResources();
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.depth = -1000;
            Rect rect = new Rect(Margin, Margin, DefaultWidth, DefaultHeight);
            GUI.DrawTexture(rect, _backgroundTexture, ScaleMode.StretchToFill, true);
            GUI.Label(rect, _message, _labelStyle);
            GUI.color = previousColor;
        }

        private void InitializeResources()
        {
            if (_initialized)
            {
                return;
            }

            _backgroundTexture = CreateRoundedBackground(16, 16, 6, new Color(0f, 0f, 0f, 0.65f));
            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.alignment = TextAnchor.MiddleCenter;
            _labelStyle.normal.textColor = Color.white;
            _labelStyle.fontSize = 15;
            _labelStyle.richText = false;
            _initialized = true;
        }

        private float GetAlpha(float elapsed)
        {
            if (elapsed < _fadeInDuration)
            {
                return Mathf.Clamp01(elapsed / _fadeInDuration);
            }

            float fadeOutStart = _fadeInDuration + _holdDuration;
            if (elapsed < fadeOutStart)
            {
                return 1f;
            }

            float fadeOutElapsed = elapsed - fadeOutStart;
            if (fadeOutElapsed < _fadeOutDuration)
            {
                return 1f - Mathf.Clamp01(fadeOutElapsed / _fadeOutDuration);
            }

            return 0f;
        }

        private void Hide()
        {
            if (!_visible)
            {
                return;
            }

            _visible = false;
            GuiLogger.Instance.Info("Overlay Hide");
        }

        private static Texture2D CreateRoundedBackground(int width, int height, int radius, Color color)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool corner = IsTransparentCorner(x, y, width, height, radius);
                    texture.SetPixel(x, y, corner ? clear : color);
                }
            }

            texture.Apply(false, true);
            return texture;
        }

        private static bool IsTransparentCorner(int x, int y, int width, int height, int radius)
        {
            int left = radius;
            int right = width - radius - 1;
            int bottom = radius;
            int top = height - radius - 1;
            int centerX = x < left ? left : (x > right ? right : x);
            int centerY = y < bottom ? bottom : (y > top ? top : y);
            int deltaX = x - centerX;
            int deltaY = y - centerY;
            return deltaX * deltaX + deltaY * deltaY > radius * radius;
        }
    }
}
