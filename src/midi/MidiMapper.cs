using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using MidiVolumeMixer.Audio;

namespace MidiVolumeMixer.Midi
{
    public class MidiMapper
    {
        private Dictionary<Guid, MidiMapping> _mappingsById;
        private Dictionary<int, Guid> _noteToMappingIdLookup;
        private readonly VolumeController _volumeController;
        private const string DEFAULT_SAVE_FILE = "midi_mappings.json";
        private int _selectedMidiDeviceIndex = 0;
        
        public int SelectedMidiDeviceIndex 
        {
            get => _selectedMidiDeviceIndex;
            set => _selectedMidiDeviceIndex = value;
        }

        // Event to notify when a MIDI-based volume change occurs
        public event EventHandler VolumeChanged;

        public MidiMapper(VolumeController volumeController)
        {
            _volumeController = volumeController;
            _mappingsById = new Dictionary<Guid, MidiMapping>();
            _noteToMappingIdLookup = new Dictionary<int, Guid>();
            
            // Try to load existing mappings first
            if (!LoadMappings())
            {
                // If no mappings could be loaded, initialize default mappings
                InitializeDefaultMappings();
            }
        }
        
        private void InitializeDefaultMappings()
        {
            // Initialize default mappings
            var mapping1 = new MidiMapping
            {
                Id = Guid.NewGuid(),
                MidiNote = 36,
                Settings = new VolumeSetting[] {
                    new VolumeSetting("cs2.exe", 20),
                    new VolumeSetting("msedge.exe", 100)
                }
            };
            
            var mapping2 = new MidiMapping
            {
                Id = Guid.NewGuid(),
                MidiNote = 37,
                Settings = new VolumeSetting[] {
                    new VolumeSetting("cs2.exe", 100),
                    new VolumeSetting("msedge.exe", 100)
                }
            };
            
            _mappingsById.Add(mapping1.Id, mapping1);
            _mappingsById.Add(mapping2.Id, mapping2);
            
            _noteToMappingIdLookup.Add(mapping1.MidiNote, mapping1.Id);
            _noteToMappingIdLookup.Add(mapping2.MidiNote, mapping2.Id);
        }

        public void MapMidiEventToVolume(MidiEvent midiEvent)
        {
            // Check if the event is a NoteOnEvent (typically for pads)
            if (midiEvent is NoteOnEvent noteOnEvent && noteOnEvent.Velocity > 0)
            {
                int noteNumber = noteOnEvent.NoteNumber;
                Console.WriteLine($"MIDI Note On: {noteNumber}, Velocity: {noteOnEvent.Velocity}");
                
                MapMidiInput(noteNumber);
            }
            // Could also handle ControlChangeEvent for knobs/sliders
            else if (midiEvent is ControlChangeEvent ccEvent)
            {
                Console.WriteLine($"MIDI CC: {ccEvent.ControlNumber}, Value: {ccEvent.ControlValue}");
                // Handle controller events like knobs or sliders if needed
            }
        }

        public void MapMidiInput(int midiNote)
        {
            if (_noteToMappingIdLookup.TryGetValue(midiNote, out Guid mappingId) &&
                _mappingsById.TryGetValue(mappingId, out MidiMapping mapping))
            {
                foreach (var setting in mapping.Settings)
                {
                    ApplyVolumeSetting(setting);
                }
                Console.WriteLine($"Applied volume settings for note {midiNote} (Mapping ID: {mappingId})");
                
                // Notify listeners that volume changes have occurred
                OnVolumeChanged();
            }
            else
            {
                Console.WriteLine($"No mapping found for note {midiNote}");
            }
        }

        // Method to trigger the VolumeChanged event
        protected virtual void OnVolumeChanged()
        {
            VolumeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ApplyVolumeSetting(VolumeSetting setting)
        {
            if (setting.ApplicationName.Equals("All Applications", StringComparison.OrdinalIgnoreCase))
            {
                _volumeController.SetVolumeForAllApplications(setting.VolumeLevel);
            }
            else
            {
                _volumeController.SetVolumeForApplication(setting.ApplicationName, setting.VolumeLevel);
            }
        }

        public void AddMapping(int midiNote, VolumeSetting[] settings)
        {
            // Create new mapping with unique ID
            var newMapping = new MidiMapping
            {
                Id = Guid.NewGuid(),
                MidiNote = midiNote,
                Settings = settings
            };
            
            // If there was a previous mapping for this note, remove it first
            RemoveMappingForNote(midiNote);
            
            // Add the new mapping
            _mappingsById.Add(newMapping.Id, newMapping);
            _noteToMappingIdLookup.Add(midiNote, newMapping.Id);
        }

        public void UpdateMapping(Guid mappingId, int midiNote, VolumeSetting[] settings)
        {
            if (_mappingsById.TryGetValue(mappingId, out var existingMapping))
            {
                // If the MIDI note changed, update the lookup
                if (existingMapping.MidiNote != midiNote)
                {
                    _noteToMappingIdLookup.Remove(existingMapping.MidiNote);
                    
                    // If there was a previous mapping for the new note, remove it first
                    RemoveMappingForNote(midiNote);
                    
                    _noteToMappingIdLookup.Add(midiNote, mappingId);
                }
                
                // Update the mapping
                existingMapping.MidiNote = midiNote;
                existingMapping.Settings = settings;
            }
        }

        private void RemoveMappingForNote(int midiNote)
        {
            if (_noteToMappingIdLookup.TryGetValue(midiNote, out Guid existingId))
            {
                _noteToMappingIdLookup.Remove(midiNote);
                _mappingsById.Remove(existingId);
            }
        }

        public bool RemoveMapping(Guid mappingId)
        {
            if (_mappingsById.TryGetValue(mappingId, out var mapping))
            {
                _noteToMappingIdLookup.Remove(mapping.MidiNote);
                return _mappingsById.Remove(mappingId);
            }
            return false;
        }

        public List<MidiMapping> GetAllMappings()
        {
            return _mappingsById.Values.ToList();
        }

        public MidiMapping GetMappingForNote(int midiNote)
        {
            if (_noteToMappingIdLookup.TryGetValue(midiNote, out Guid mappingId) &&
                _mappingsById.TryGetValue(mappingId, out MidiMapping mapping))
            {
                return mapping;
            }
            return null;
        }

        public MidiMapping GetMappingById(Guid mappingId)
        {
            if (_mappingsById.TryGetValue(mappingId, out MidiMapping mapping))
            {
                return mapping;
            }
            return null;
        }
        
        public bool SaveMappings(string filePath = null)
        {
            filePath = filePath ?? GetDefaultSaveFilePath();
            
            try
            {
                var mappingsToSave = new MappingFile
                {
                    Mappings = _mappingsById.Values.ToList(),
                    SaveTimestamp = DateTime.Now,
                    SelectedMidiDeviceIndex = _selectedMidiDeviceIndex
                };
                
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(mappingsToSave, options);
                
                File.WriteAllText(filePath, json);
                Console.WriteLine($"Saved {_mappingsById.Count} mappings to {filePath} with MIDI device index {_selectedMidiDeviceIndex}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving mappings: {ex.Message}");
                return false;
            }
        }
        
        public bool LoadMappings(string filePath = null)
        {
            filePath = filePath ?? GetDefaultSaveFilePath();
            
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Mappings file not found: {filePath}");
                    return false;
                }
                
                string json = File.ReadAllText(filePath);
                var mappingsFile = JsonSerializer.Deserialize<MappingFile>(json);
                
                if (mappingsFile == null)
                {
                    Console.WriteLine("Invalid mappings file format");
                    return false;
                }
                
                // Load MIDI device index
                _selectedMidiDeviceIndex = mappingsFile.SelectedMidiDeviceIndex;
                
                // If no mappings are found, we still consider this a success for device selection
                if (mappingsFile.Mappings == null || mappingsFile.Mappings.Count == 0)
                {
                    Console.WriteLine("No mappings found in the file, but device index was loaded");
                    return true;
                }
                
                // Clear existing mappings
                _mappingsById.Clear();
                _noteToMappingIdLookup.Clear();
                
                // Load mappings from file
                foreach (var mapping in mappingsFile.Mappings)
                {
                    if (mapping.Id == Guid.Empty)
                    {
                        mapping.Id = Guid.NewGuid();
                    }
                    
                    _mappingsById[mapping.Id] = mapping;
                    _noteToMappingIdLookup[mapping.MidiNote] = mapping.Id;
                }
                
                Console.WriteLine($"Loaded {_mappingsById.Count} mappings from {filePath} with MIDI device index {_selectedMidiDeviceIndex}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading mappings: {ex.Message}");
                return false;
            }
        }
        
        private string GetDefaultSaveFilePath()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataFolder, "MidiVolumeMixer");
            
            // Create the directory if it doesn't exist
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            return Path.Combine(appFolder, DEFAULT_SAVE_FILE);
        }
    }

    public class MappingFile
    {
        public List<MidiMapping> Mappings { get; set; }
        public DateTime SaveTimestamp { get; set; }
        public int SelectedMidiDeviceIndex { get; set; } = 0;
    }

    public class MidiMapping
    {
        public Guid Id { get; set; }
        public int MidiNote { get; set; }
        public VolumeSetting[] Settings { get; set; }
    }

    public class VolumeSetting
    {
        // To make the class serializable for JsonSerializer
        public VolumeSetting() { }
        
        public VolumeSetting(string applicationName, int volumeLevel)
        {
            ApplicationName = applicationName;
            VolumeLevel = volumeLevel;
        }

        public string ApplicationName { get; set; }
        public int VolumeLevel { get; set; }
    }
}