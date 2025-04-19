using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace MidiVolumeMixer.Midi
{
    public class MidiListener : IDisposable
    {
        private readonly MidiMapper _midiMapper;
        private InputDevice _inputDevice;
        private bool _isDisposed = false;

        // Event for notifying about received MIDI notes
        public event EventHandler<MidiNoteEventArgs> MidiNoteReceived;

        public MidiListener(MidiMapper midiMapper)
        {
            _midiMapper = midiMapper;
        }

        public void StartListening(int deviceIndex = 0)
        {
            if (_inputDevice != null)
            {
                _inputDevice.Dispose();
            }

            try
            {
                var availableDevices = InputDevice.GetAll().ToList();
                if (availableDevices.Count == 0)
                {
                    Console.WriteLine("No MIDI input devices available.");
                    return;
                }

                if (deviceIndex >= availableDevices.Count)
                {
                    deviceIndex = 0;
                    Console.WriteLine($"Device index out of range. Using device 0: {availableDevices[0].Name}");
                }

                _inputDevice = availableDevices[deviceIndex];
                _inputDevice.EventReceived += OnMidiEventReceived;
                _inputDevice.StartEventsListening();

                Console.WriteLine($"Listening to MIDI device: {_inputDevice.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing MIDI device: {ex.Message}");
            }
        }

        private void OnMidiEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            _midiMapper.MapMidiEventToVolume(e.Event);
            
            // Notify about MIDI note events
            if (e.Event is NoteOnEvent noteOnEvent)
            {
                MidiNoteReceived?.Invoke(this, new MidiNoteEventArgs(noteOnEvent.NoteNumber, noteOnEvent.Velocity));
            }
        }

        public void StopListening()
        {
            if (_inputDevice != null)
            {
                _inputDevice.EventReceived -= OnMidiEventReceived;
                _inputDevice.StopEventsListening();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    StopListening();
                    _inputDevice?.Dispose();
                }

                _isDisposed = true;
            }
        }

        ~MidiListener()
        {
            Dispose(false);
        }
    }

    public class MidiNoteEventArgs : EventArgs
    {
        public int NoteNumber { get; }
        public int Velocity { get; }

        public MidiNoteEventArgs(int noteNumber, int velocity)
        {
            NoteNumber = noteNumber;
            Velocity = velocity;
        }
    }
}