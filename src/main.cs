using System;
using System.Windows;
using MidiVolumeMixer.Audio;
using MidiVolumeMixer.Midi;

namespace MidiVolumeMixer
{
    // Partial class extending the App class defined in App.xaml.cs
    public partial class App
    {
        private MidiListener _midiListener;
        private VolumeController _volumeController;
        private MidiMapper _midiMapper;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            try
            {
                // Initialize audio components
                _volumeController = new VolumeController();
                
                // Initialize MIDI components
                _midiMapper = new MidiMapper(_volumeController);
                _midiListener = new MidiListener(_midiMapper);
                
                // Start listening for MIDI events
                _midiListener.StartListening();
                
                Console.WriteLine("MIDI Volume Mixer initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing components: {ex.Message}");
                LogException(ex); // Use our logging method
                MessageBox.Show($"Error initializing application: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up resources
            _midiListener?.Dispose();
            
            base.OnExit(e);
        }
    }
}