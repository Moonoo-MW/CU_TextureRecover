// ------------------------------------------------------------------------------
// GuiReplacer - Runtime Texture2D replacement framework for Unity Mono games.
// This file owns all BepInEx configuration entries used by the plugin.
// ------------------------------------------------------------------------------

using BepInEx.Configuration;

namespace GuiReplacer
{
    /// <summary>
    /// Provides strongly typed access to GuiReplacer.cfg settings.
    /// </summary>
    public sealed class Config
    {
        private static Config _instance;

        private ConfigEntry<bool> _enable;
        private ConfigEntry<bool> _enableHotReload;
        private ConfigEntry<bool> _recursiveScan;
        private ConfigEntry<bool> _ignoreCase;
        private ConfigEntry<bool> _allowSizeMismatch;
        private ConfigEntry<bool> _enableLog;
        private ConfigEntry<bool> _enableDump;
        private ConfigEntry<string> _modsFolder;

        private Config()
        {
        }

        /// <summary>
        /// Gets the global configuration instance.
        /// </summary>
        public static Config Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Config();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Gets whether the plugin is enabled.
        /// </summary>
        public bool Enable { get { return _enable.Value; } }

        /// <summary>
        /// Gets whether F8 hot reload is enabled.
        /// </summary>
        public bool EnableHotReload { get { return _enableHotReload.Value; } }

        /// <summary>
        /// Gets whether PNG scanning includes subdirectories.
        /// </summary>
        public bool RecursiveScan { get { return _recursiveScan.Value; } }

        /// <summary>
        /// Gets whether texture-name matching ignores casing.
        /// </summary>
        public bool IgnoreCase { get { return _ignoreCase.Value; } }

        /// <summary>
        /// Gets whether replacement continues when PNG and target dimensions differ.
        /// </summary>
        public bool AllowSizeMismatch { get { return _allowSizeMismatch.Value; } }

        /// <summary>
        /// Gets whether logging is enabled.
        /// </summary>
        public bool EnableLog { get { return _enableLog.Value; } }

        /// <summary>
        /// Gets or sets whether all loaded Texture2D objects should be dumped to PNG files.
        /// </summary>
        public bool EnableDump
        {
            get { return _enableDump.Value; }
            set { _enableDump.Value = value; }
        }

        /// <summary>
        /// Gets the relative Mods folder containing replacement PNG files.
        /// </summary>
        public string ModsFolder { get { return _modsFolder.Value; } }

        /// <summary>
        /// Binds all BepInEx configuration entries and creates GuiReplacer.cfg when needed.
        /// </summary>
        /// <param name="configFile">The BepInEx configuration file.</param>
        public void Initialize(ConfigFile configFile)
        {
            _enable = configFile.Bind("General", "Enable", true, "Enable runtime GUI texture replacement.");
            _enableHotReload = configFile.Bind("General", "EnableHotReload", true, "Press F8 to rescan Mods/GUI and reapply replacements.");
            _recursiveScan = configFile.Bind("General", "RecursiveScan", true, "Scan replacement PNG files recursively.");
            _ignoreCase = configFile.Bind("General", "IgnoreCase", true, "Match PNG names to Texture.name without case sensitivity.");
            _allowSizeMismatch = configFile.Bind("General", "AllowSizeMismatch", false, "Allow replacement when image size differs. The replacement is scaled to the original texture size.");
            _enableLog = configFile.Bind("General", "EnableLog", true, "Enable GuiReplacer logging.");
            _modsFolder = configFile.Bind("General", "ModsFolder", "Mods/GUI", "Path relative to the game root containing replacement PNG files.");
            _enableDump = configFile.Bind("Debug", "EnableDump", false, "Dump all Texture2D objects to Mods/GUI_Dump, then automatically turn this option off.");
            configFile.Save();
        }
    }
}
