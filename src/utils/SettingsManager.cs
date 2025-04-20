using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MidiVolumeMixer.Utils
{
    // Create a separate settings data class for serialization
    public class SettingsData
    {
        public bool StartWithWindows { get; set; }
        public bool MinimizeToTray { get; set; }
        public bool StartMinimized { get; set; }
        public bool UseDarkMode { get; set; }
    }

    public class SettingsManager
    {
        private const string DEFAULT_SETTINGS_FILE = "app_settings.json";
        private static readonly string SettingsFilePath;

        // Singleton instance
        private static SettingsManager _instance;

        // Settings data
        private SettingsData _data;

        // Application settings properties
        public bool StartWithWindows 
        { 
            get => _data.StartWithWindows; 
            set => _data.StartWithWindows = value; 
        }
        
        public bool MinimizeToTray 
        { 
            get => _data.MinimizeToTray; 
            set => _data.MinimizeToTray = value; 
        }
        
        public bool StartMinimized 
        { 
            get => _data.StartMinimized; 
            set => _data.StartMinimized = value; 
        }
        
        public bool UseDarkMode 
        { 
            get => _data.UseDarkMode; 
            set => _data.UseDarkMode = value; 
        }

        static SettingsManager()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataFolder, "MidiVolumeMixer");
            
            // Create the directory if it doesn't exist
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            SettingsFilePath = Path.Combine(appFolder, DEFAULT_SETTINGS_FILE);
            Logger.Log($"Settings file path initialized: {SettingsFilePath}");
        }

        private SettingsManager()
        {
            // Initialize with default settings
            _data = new SettingsData
            {
                StartWithWindows = false,
                MinimizeToTray = false,
                StartMinimized = false,
                UseDarkMode = false
            };
            Logger.Log("SettingsManager initialized with default values");
        }

        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Logger.Log("Creating SettingsManager instance");
                    _instance = Load();
                }
                return _instance;
            }
        }

        public void Save()
        {
            try
            {
                Logger.Log("Saving settings...");
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                // Serialize the settings data object, not the manager
                string json = JsonSerializer.Serialize(_data, options);
                File.WriteAllText(SettingsFilePath, json);
                
                Logger.Log($"Settings saved to: {SettingsFilePath}");
                Logger.Log($"Settings content: {json}");
            }
            catch (Exception ex)
            {
                Logger.LogException("Save settings", ex);
            }
        }

        private static SettingsManager Load()
        {
            var manager = new SettingsManager();
            
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    Logger.Log($"Loading settings from: {SettingsFilePath}");
                    string json = File.ReadAllText(SettingsFilePath);
                    
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Logger.Log("Settings file exists but is empty");
                        return manager;
                    }
                    
                    Logger.Log($"Settings file content: {json}");
                    var loadedData = JsonSerializer.Deserialize<SettingsData>(json);
                    
                    if (loadedData != null)
                    {
                        // Replace the default data with loaded data
                        manager._data = loadedData;
                        Logger.Log($"Settings loaded successfully: StartWithWindows={loadedData.StartWithWindows}, MinimizeToTray={loadedData.MinimizeToTray}, StartMinimized={loadedData.StartMinimized}, UseDarkMode={loadedData.UseDarkMode}");
                    }
                    else
                    {
                        Logger.Log("Settings deserialized to null");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException("Load settings", ex);
                }
            }
            else
            {
                Logger.Log($"Settings file does not exist: {SettingsFilePath}");
                Logger.Log("Using default settings");
            }
            
            return manager;
        }

        // Helper to configure auto-start with Windows
        public void ConfigureAutoStart(bool enable)
        {
            try
            {
                Logger.Log($"Configuring auto-start: {enable}");
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            // Get the path to the executable
                            string execPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            key.SetValue("MidiVolumeMixer", $"\"{execPath}\"");
                            Logger.Log($"Application set to start with Windows using path: {execPath}");
                        }
                        else
                        {
                            key.DeleteValue("MidiVolumeMixer", false);
                            Logger.Log("Application removed from startup");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("ConfigureAutoStart", ex);
            }
        }
    }
}