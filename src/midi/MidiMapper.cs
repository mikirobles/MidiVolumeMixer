using System;
using System.Collections.Generic;
using System.Linq;
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

        public MidiMapper(VolumeController volumeController)
        {
            _volumeController = volumeController;
            _mappingsById = new Dictionary<Guid, MidiMapping>();
            _noteToMappingIdLookup = new Dictionary<int, Guid>();
            
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
            }
            else
            {
                Console.WriteLine($"No mapping found for note {midiNote}");
            }
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
    }

    public class MidiMapping
    {
        public Guid Id { get; set; }
        public int MidiNote { get; set; }
        public VolumeSetting[] Settings { get; set; }
    }

    public class VolumeSetting
    {
        public string ApplicationName { get; }
        public int VolumeLevel { get; }

        public VolumeSetting(string applicationName, int volumeLevel)
        {
            ApplicationName = applicationName;
            VolumeLevel = volumeLevel;
        }
    }
}