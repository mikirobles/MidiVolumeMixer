using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MidiVolumeMixer.Utils
{
    public class ConfigManager
    {
        private const string ConfigFilePath = "config.json";

        public VolumePreset LoadConfig()
        {
            if (!File.Exists(ConfigFilePath))
            {
                return new VolumePreset();
            }

            var json = File.ReadAllText(ConfigFilePath);
            return JsonConvert.DeserializeObject<VolumePreset>(json);
        }

        public void SaveConfig(VolumePreset preset)
        {
            var json = JsonConvert.SerializeObject(preset, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }
    }

    public class VolumePreset
    {
        public Dictionary<string, int> ApplicationVolumes { get; set; } = new Dictionary<string, int>();
    }
}