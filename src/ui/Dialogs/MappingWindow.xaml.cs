using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MidiVolumeMixer.Audio;
using MidiVolumeMixer.Midi;

namespace MidiVolumeMixer.ui.Dialogs
{
    public partial class MappingWindow : Window, INotifyPropertyChanged
    {
        private readonly VolumeController _volumeController;
        private readonly MidiMapper _midiMapper;
        private ObservableCollection<MappedAppViewModel> _mappedApps;
        private ObservableCollection<AppVolumeViewModel> _applications;
        private bool _isLearningMidi;
        private int _currentMidiNote;
        private Guid? _currentMappingId;
        private bool _isEditMode;

        public event PropertyChangedEventHandler PropertyChanged;

        public MappingWindow(Window owner, VolumeController volumeController, MidiMapper midiMapper)
        {
            InitializeComponent();

            Owner = owner;
            _volumeController = volumeController;
            _midiMapper = midiMapper;
            _mappedApps = new ObservableCollection<MappedAppViewModel>();
            _applications = new ObservableCollection<AppVolumeViewModel>();
            _currentMidiNote = -1;
            _isEditMode = false;

            // Set data contexts
            MappedAppsListBox.ItemsSource = _mappedApps;
            ApplicationsListView.ItemsSource = _applications;

            // Load applications
            LoadApplications();
        }

        public MappingWindow(Window owner, VolumeController volumeController, MidiMapper midiMapper, int midiNote)
            : this(owner, volumeController, midiMapper)
        {
            _currentMidiNote = midiNote;
            MidiNoteTextBox.Text = midiNote.ToString();

            // Check if there's an existing mapping for this note
            var mapping = _midiMapper.GetMappingForNote(midiNote);
            if (mapping != null)
            {
                _currentMappingId = mapping.Id;
                _isEditMode = true;
                LoadExistingMapping(mapping);
            }
        }

        public MappingWindow(Window owner, VolumeController volumeController, MidiMapper midiMapper, Guid mappingId)
            : this(owner, volumeController, midiMapper)
        {
            var mapping = _midiMapper.GetMappingById(mappingId);
            if (mapping != null)
            {
                _currentMappingId = mappingId;
                _currentMidiNote = mapping.MidiNote;
                MidiNoteTextBox.Text = mapping.MidiNote.ToString();
                _isEditMode = true;

                LoadExistingMapping(mapping);
            }
            else
            {
                MessageBox.Show("The specified mapping could not be found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void LoadApplications()
        {
            _applications.Clear();

            foreach (var session in _volumeController.GetAllAudioSessions())
            {
                _applications.Add(new AppVolumeViewModel
                {
                    ProcessName = session.ProcessName,
                    CurrentVolume = (int)(session.Volume * 100),
                    TargetVolume = (int)(session.Volume * 100)
                });
            }

            // Add "All Applications" option
            _applications.Add(new AppVolumeViewModel
            {
                ProcessName = "All Applications",
                CurrentVolume = 100,
                TargetVolume = 100,
                IsAllApplications = true
            });
        }

        private void LoadExistingMapping(MidiMapping mapping)
        {
            _mappedApps.Clear();

            foreach (var setting in mapping.Settings)
            {
                _mappedApps.Add(new MappedAppViewModel
                {
                    ApplicationName = setting.ApplicationName,
                    VolumeLevel = setting.VolumeLevel,
                    DisplayText = $"{setting.ApplicationName}: {setting.VolumeLevel}%"
                });
            }
        }

        public void OnMidiNoteReceived(int note)
        {
            if (_isLearningMidi)
            {
                _currentMidiNote = note;

                // Update the UI on the UI thread
                Dispatcher.Invoke(() =>
                {
                    MidiNoteTextBox.Text = note.ToString();
                    _isLearningMidi = false;
                    LearnButton.Content = "Learn";
                });
            }
        }

        private void LearnButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isLearningMidi)
            {
                _isLearningMidi = false;
                LearnButton.Content = "Learn";
            }
            else
            {
                _isLearningMidi = true;
                LearnButton.Content = "Cancel";
                MessageBox.Show("Press a key on your MIDI controller.", "MIDI Learn", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddSelectedAppsButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = ApplicationsListView.SelectedItem as AppVolumeViewModel;

            if (selectedItem != null)
            {
                // Check if this app is already mapped
                bool isDuplicate = _mappedApps.Any(app => app.ApplicationName == selectedItem.ProcessName);

                if (!isDuplicate)
                {
                    _mappedApps.Add(new MappedAppViewModel
                    {
                        ApplicationName = selectedItem.ProcessName,
                        VolumeLevel = selectedItem.TargetVolume,
                        DisplayText = $"{selectedItem.ProcessName}: {selectedItem.TargetVolume}%",
                        IsAllApplications = selectedItem.IsAllApplications
                    });
                }
                else
                {
                    MessageBox.Show("This application is already in the mapping.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Please select an application first.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveMappedApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is MappedAppViewModel app)
            {
                _mappedApps.Remove(app);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMidiNote < 0)
            {
                MessageBox.Show("Please select a MIDI note first.", "No MIDI Note", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_mappedApps.Count == 0)
            {
                MessageBox.Show("Please add at least one application mapping.", "No Mappings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Convert to volume settings
            var settings = _mappedApps
                .Select(app => new VolumeSetting(app.ApplicationName, app.VolumeLevel))
                .ToArray();

            // Save or update the mapping
            if (_isEditMode && _currentMappingId.HasValue)
            {
                // Update existing mapping
                _midiMapper.UpdateMapping(_currentMappingId.Value, _currentMidiNote, settings);
                Console.WriteLine($"Updated mapping {_currentMappingId} for MIDI note {_currentMidiNote}");
            }
            else
            {
                // Add a new mapping
                _midiMapper.AddMapping(_currentMidiNote, settings);
                Console.WriteLine($"Added new mapping for MIDI note {_currentMidiNote}");
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class AppVolumeViewModel : INotifyPropertyChanged
    {
        public string ProcessName { get; set; }
        public int CurrentVolume { get; set; }
        public bool IsAllApplications { get; set; }

        private int _targetVolume;
        public int TargetVolume
        {
            get => _targetVolume;
            set
            {
                if (_targetVolume != value)
                {
                    _targetVolume = value;
                    OnPropertyChanged(nameof(TargetVolume));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MappedAppViewModel
    {
        public string ApplicationName { get; set; }
        public int VolumeLevel { get; set; }
        public string DisplayText { get; set; }
        public bool IsAllApplications { get; set; }
    }
}