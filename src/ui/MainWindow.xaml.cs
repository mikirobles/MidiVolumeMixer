using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Serialization;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using MidiVolumeMixer.Audio;
using MidiVolumeMixer.Midi;
using MidiVolumeMixer.ui.Dialogs;
using MidiVolumeMixer.Utils;

namespace MidiVolumeMixer.ui
{
    public partial class MainWindow : Window
    {
        private readonly VolumeController _volumeController;
        private readonly MidiMapper _midiMapper;
        private MidiListener _midiListener;
        private ObservableCollection<AudioSession> _audioSessions;
        private ObservableCollection<MappingViewModel> _mappings;
        private bool _isLearningMidi = false;
        private int _midiNoteToLearn = -1;
        private MappingWindow _activeMappingWindow;
        private TrayIcon _trayIcon;
        private SettingsManager _settings => SettingsManager.Instance;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize controllers
            _volumeController = new VolumeController();
            _midiMapper = new MidiMapper(_volumeController);

            // Create tray icon
            _trayIcon = new TrayIcon(this);

            // Subscribe to volume changed events
            _midiMapper.VolumeChanged += OnVolumeChanged;

            // Initialize collections
            _audioSessions = new ObservableCollection<AudioSession>();
            _mappings = new ObservableCollection<MappingViewModel>();

            // Set data contexts
            ApplicationsListView.ItemsSource = _audioSessions;
            MappingsListView.ItemsSource = _mappings;

            // Register for window load and closing events
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load and apply settings
            LoadSettings();

            // Load available MIDI devices
            LoadMidiDevices();

            // Load running applications
            RefreshApplicationsList();

            // Convert mappings to view models
            UpdateMappingsListFromMidiMapper();

            // Apply theme from settings
            ApplyTheme(_settings.UseDarkMode);

            // Apply startup state
            if (_settings.StartMinimized && _settings.MinimizeToTray)
            {
                _trayIcon.ShowInTray();
            }
        }

        private void LoadSettings()
        {
            // Load settings from file
            StartWithWindowsCheckBox.IsChecked = _settings.StartWithWindows;
            MinimizeToTrayCheckBox.IsChecked = _settings.MinimizeToTray;
            StartMinimizedCheckBox.IsChecked = _settings.StartMinimized;
            DarkModeCheckBox.IsChecked = _settings.UseDarkMode;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                // Save current settings state before closing
                _settings.StartWithWindows = StartWithWindowsCheckBox.IsChecked ?? false;
                _settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? false;
                _settings.StartMinimized = StartMinimizedCheckBox.IsChecked ?? false;
                _settings.UseDarkMode = DarkModeCheckBox.IsChecked ?? false;
                _settings.Save();

                // Auto-save mappings when the application is closing
                _midiMapper.SaveMappings();
                Console.WriteLine("Settings and mappings auto-saved on application exit");

                // Clean up tray icon
                _trayIcon?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during application close: {ex.Message}");
                // Don't show message box on closing as it would be annoying
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (_settings.MinimizeToTray && this.WindowState == WindowState.Minimized)
            {
                _trayIcon.ShowInTray();
            }
        }

        private void DarkModeCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isDarkMode = DarkModeCheckBox.IsChecked ?? false;
            ApplyTheme(isDarkMode);

            // Save the setting immediately
            _settings.UseDarkMode = isDarkMode;
            _settings.Save();
        }

        private void ApplyTheme(bool isDarkMode)
        {
            var app = Application.Current;

            // Get the resource dictionaries
            var darkTheme = app.Resources.MergedDictionaries[0]["DarkTheme"] as ResourceDictionary;
            var lightTheme = app.Resources.MergedDictionaries[0]["LightTheme"] as ResourceDictionary;

            if (isDarkMode && darkTheme != null)
            {
                // Apply dark theme
                foreach (var key in darkTheme.Keys)
                {
                    if (app.Resources.Contains(key))
                    {
                        app.Resources[key] = darkTheme[key];
                    }
                }
            }
            else if (lightTheme != null)
            {
                // Apply light theme
                foreach (var key in lightTheme.Keys)
                {
                    if (app.Resources.Contains(key))
                    {
                        app.Resources[key] = lightTheme[key];
                    }
                }
            }
        }

        private void LoadMidiDevices()
        {
            try
            {
                MidiDevicesComboBox.Items.Clear();

                var inputDevices = InputDevice.GetAll().ToList();
                foreach (var device in inputDevices)
                {
                    MidiDevicesComboBox.Items.Add(device.Name);
                }

                if (inputDevices.Count > 0)
                {
                    // Use the saved device index if it's valid
                    int savedIndex = _midiMapper.SelectedMidiDeviceIndex;
                    if (savedIndex >= 0 && savedIndex < inputDevices.Count)
                    {
                        MidiDevicesComboBox.SelectedIndex = savedIndex;
                        MidiStatusTextBlock.Text = $"Using saved MIDI device: {inputDevices[savedIndex].Name}";
                    }
                    else
                    {
                        // Fall back to the first device if saved index is invalid
                        MidiDevicesComboBox.SelectedIndex = 0;
                        MidiStatusTextBlock.Text = $"Found {inputDevices.Count} MIDI devices";
                    }
                }
                else
                {
                    MidiStatusTextBlock.Text = "No MIDI devices found";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading MIDI devices: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MidiStatusTextBlock.Text = "Error loading MIDI devices";
            }
        }

        private void RefreshApplicationsList()
        {
            try
            {
                var sessions = _volumeController.GetAllAudioSessions();

                _audioSessions.Clear();
                foreach (var session in sessions)
                {
                    _audioSessions.Add(session);
                }

                if (_audioSessions.Count == 0)
                {
                    MessageBox.Show("No audio applications found", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing applications: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMappingsListFromMidiMapper()
        {
            _mappings.Clear();

            foreach (var mapping in _midiMapper.GetAllMappings())
            {
                var description = string.Join(", ", mapping.Settings.Select(s => $"{s.ApplicationName}: {s.VolumeLevel}%"));
                _mappings.Add(new MappingViewModel
                {
                    MappingId = mapping.Id,
                    MidiNote = mapping.MidiNote,
                    MappingDescription = description
                });
            }
        }

        private void MidiDevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MidiDevicesComboBox.SelectedIndex >= 0)
            {
                try
                {
                    // Store the selected device index for persistence
                    _midiMapper.SelectedMidiDeviceIndex = MidiDevicesComboBox.SelectedIndex;

                    // Dispose of previous listener if one exists
                    _midiListener?.Dispose();

                    // Create new listener
                    _midiListener = new MidiListener(_midiMapper);

                    // Subscribe to MIDI note events
                    _midiListener.MidiNoteReceived += OnMidiNoteReceived;

                    _midiListener.StartListening(MidiDevicesComboBox.SelectedIndex);

                    MidiStatusTextBlock.Text = $"Listening to {MidiDevicesComboBox.SelectedItem}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error connecting to MIDI device: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    MidiStatusTextBlock.Text = "Error connecting to MIDI device";
                }
            }
        }

        private void OnMidiNoteReceived(object sender, MidiNoteEventArgs e)
        {
            // Use dispatcher to update UI from another thread
            Dispatcher.Invoke(() =>
            {
                // Update status text with pressed note
                MidiStatusTextBlock.Text = $"Listening to {MidiDevicesComboBox.SelectedItem} - Note: {e.NoteNumber} Velocity: {e.Velocity}";

                // If in learning mode, notify the mapping window
                _activeMappingWindow?.OnMidiNoteReceived(e.NoteNumber);

                // Support for the learn MIDI button in the main window (legacy)
                if (_isLearningMidi)
                {
                    _midiNoteToLearn = e.NoteNumber;
                    _isLearningMidi = false;

                    ShowMappingWindow(_midiNoteToLearn);
                }
            });
        }

        private void RefreshDevicesButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMidiDevices();
        }

        private void RefreshApplications_Click(object sender, RoutedEventArgs e)
        {
            RefreshApplicationsList();
        }

        private void SetVolume_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                try
                {
                    // Get the button's tag directly from the DataContext and Tag
                    var tag = button.Tag as string;
                    var audioSession = button.DataContext as AudioSession;

                    if (tag != null && audioSession != null)
                    {
                        string processName = audioSession.ProcessName;
                        if (int.TryParse(tag, out int volumePercent))
                        {
                            Console.WriteLine($"Setting volume for {processName} to {volumePercent}%");
                            _volumeController.SetVolumeForApplication(processName, volumePercent);

                            // Refresh the list to show updated volumes
                            RefreshApplicationsList();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Invalid tag or missing DataContext: Tag={tag}, AudioSession={audioSession != null}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error setting volume: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SetAllVolumes_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                try
                {
                    if (int.TryParse(tag.ToString(), out int volumePercent))
                    {
                        _volumeController.SetVolumeForAllApplications(volumePercent);

                        // Refresh the list to show updated volumes
                        RefreshApplicationsList();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error setting volumes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddMapping_Click(object sender, RoutedEventArgs e)
        {
            ShowMappingWindow();
        }

        private void ShowMappingWindow(int? existingMidiNote = null, Guid? mappingId = null)
        {
            try
            {
                if (mappingId.HasValue)
                {
                    // Open the mapping window with a specific mapping ID (preferred method)
                    _activeMappingWindow = new MappingWindow(this, _volumeController, _midiMapper, mappingId.Value);
                }
                else if (existingMidiNote.HasValue)
                {
                    // Try to find a mapping ID for this MIDI note
                    var mapping = _midiMapper.GetMappingForNote(existingMidiNote.Value);
                    if (mapping != null)
                    {
                        _activeMappingWindow = new MappingWindow(this, _volumeController, _midiMapper, mapping.Id);
                    }
                    else
                    {
                        // Create a new mapping for this note
                        _activeMappingWindow = new MappingWindow(this, _volumeController, _midiMapper, existingMidiNote.Value);
                    }
                }
                else
                {
                    // Create a completely new mapping
                    _activeMappingWindow = new MappingWindow(this, _volumeController, _midiMapper);
                }

                bool? result = _activeMappingWindow.ShowDialog();

                if (result == true)
                {
                    // Refresh mappings list
                    UpdateMappingsListFromMidiMapper();
                }

                _activeMappingWindow = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing mapping window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LearnMidi_Click(object sender, RoutedEventArgs e)
        {
            _isLearningMidi = true;
            MessageBox.Show("Press a MIDI key or pad to learn. The next received MIDI note will be used for a new mapping.", "MIDI Learn", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditMapping_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                var mappingViewModel = button.DataContext as MappingViewModel;
                if (mappingViewModel != null)
                {
                    ShowMappingWindow(null, mappingViewModel.MappingId);
                }
            }
        }

        private void DeleteMapping_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var mappingViewModel = button.DataContext as MappingViewModel;
                if (mappingViewModel != null)
                {
                    if (MessageBox.Show($"Are you sure you want to delete the mapping for MIDI note {mappingViewModel.MidiNote}?",
                                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        _midiMapper.RemoveMapping(mappingViewModel.MappingId);
                        UpdateMappingsListFromMidiMapper();
                    }
                }
            }
        }

        private void SaveMappings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool success = _midiMapper.SaveMappings();

                if (success)
                {
                    MessageBox.Show("Mappings saved successfully.", "Save Mappings", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to save mappings. Please check the application logs for more details.",
                                   "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving mappings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMappings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool success = _midiMapper.LoadMappings();

                if (success)
                {
                    // Update the UI with the loaded mappings
                    UpdateMappingsListFromMidiMapper();
                    MessageBox.Show("Mappings loaded successfully.", "Load Mappings", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to load mappings. No saved mappings were found or the file was invalid.",
                                   "Load Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading mappings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Update settings based on UI
                _settings.StartWithWindows = StartWithWindowsCheckBox.IsChecked ?? false;
                _settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? false;
                _settings.StartMinimized = StartMinimizedCheckBox.IsChecked ?? false;
                _settings.UseDarkMode = DarkModeCheckBox.IsChecked ?? false;

                // Configure auto-start with Windows
                _settings.ConfigureAutoStart(_settings.StartWithWindows);

                // Save settings to file
                _settings.Save();

                MessageBox.Show("Settings saved successfully", "Save Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnVolumeChanged(object sender, EventArgs e)
        {
            // Update the application list when volume changes occur via MIDI
            Dispatcher.Invoke(() =>
            {
                RefreshApplicationsList();
            });
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Already handled in MainWindow_Closing
        }
    }

    public class MappingViewModel
    {
        public Guid MappingId { get; set; }
        public int MidiNote { get; set; }
        public string MappingDescription { get; set; }
    }
}