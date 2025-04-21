using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ComponentModel;
using NAudio.CoreAudioApi;

namespace MidiVolumeMixer.Audio
{
    public class VolumeController
    {
        private readonly Dictionary<string, float> _applicationVolumes;
        private readonly MMDeviceEnumerator _deviceEnumerator;

        public VolumeController()
        {
            _applicationVolumes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            _deviceEnumerator = new MMDeviceEnumerator();
        }

        public void SetVolumeForApplication(string processName, int volumePercent)
        {
            float volume = volumePercent / 100.0f;
            _applicationVolumes[processName] = volume;

            try
            {
                // Get the default audio endpoint
                using (var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                {
                    // Get the session manager
                    var sessionManager = device.AudioSessionManager;
                    for (int i = 0; i < sessionManager.Sessions.Count; i++)
                    {
                        var session = sessionManager.Sessions[i];

                        try
                        {
                            // Get process ID for this session
                            string sessionProcessName = GetProcessNameFromId((int)session.GetProcessID);

                            if (string.Equals(sessionProcessName, processName, StringComparison.OrdinalIgnoreCase))
                            {
                                // Set the volume for the matching process
                                session.SimpleAudioVolume.Volume = volume;
                                Console.WriteLine($"Set volume of {processName} to {volumePercent}%");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error getting process info: {ex.Message}");
                        }
                        finally
                        {
                            session?.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting volume: {ex.Message}");
            }
        }

        public void SetVolumeForAllApplications(int volumePercent)
        {
            float volume = volumePercent / 100.0f;

            try
            {
                // Get the default audio endpoint
                using (var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                {
                    // Get the session manager
                    var sessionManager = device.AudioSessionManager;
                    for (int i = 0; i < sessionManager.Sessions.Count; i++)
                    {
                        var session = sessionManager.Sessions[i];

                        try
                        {
                            // Set the volume for all processes
                            session.SimpleAudioVolume.Volume = volume;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error setting volume: {ex.Message}");
                        }
                        finally
                        {
                            session?.Dispose();
                        }
                    }
                }

                Console.WriteLine($"Set volume for all applications to {volumePercent}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting volume: {ex.Message}");
            }
        }

        public float GetVolumeForApplication(string processName)
        {
            try
            {
                // Get the default audio endpoint
                using (var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                {
                    // Get the session manager
                    var sessionManager = device.AudioSessionManager;
                    for (int i = 0; i < sessionManager.Sessions.Count; i++)
                    {
                        var session = sessionManager.Sessions[i];

                        try
                        {
                            // Check if this session belongs to our target process
                            string sessionProcessName = GetProcessNameFromId((int)session.GetProcessID);

                            if (string.Equals(sessionProcessName, processName, StringComparison.OrdinalIgnoreCase))
                            {
                                // Get the volume for the matching process
                                return session.SimpleAudioVolume.Volume;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error getting process info: {ex.Message}");
                        }
                        finally
                        {
                            session?.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting volume: {ex.Message}");
            }

            return 0.0f;
        }

        private string GetProcessNameFromId(int processId)
        {
            try
            {
                using (var process = Process.GetProcessById(processId))
                {
                    return process.ProcessName;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        public List<AudioSession> GetAllAudioSessions()
        {
            var sessions = new List<AudioSession>();

            try
            {
                // Get the default audio endpoint
                using (var device = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                {
                    // Get the session manager
                    var sessionManager = device.AudioSessionManager;
                    for (int i = 0; i < sessionManager.Sessions.Count; i++)
                    {
                        var session = sessionManager.Sessions[i];

                        try
                        {
                            string processName = GetProcessNameFromId((int)session.GetProcessID);
                            float volume = session.SimpleAudioVolume.Volume;

                            sessions.Add(new AudioSession
                            {
                                ProcessId = (uint)session.GetProcessID,
                                ProcessName = processName,
                                Volume = volume
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error getting session info: {ex.Message}");
                        }
                        finally
                        {
                            session?.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enumerating audio sessions: {ex.Message}");
            }

            return sessions;
        }
    }

    public class AudioSession : INotifyPropertyChanged
    {
        private uint _processId;
        private string _processName;
        private float _volume;

        public uint ProcessId
        {
            get => _processId;
            set
            {
                if (_processId != value)
                {
                    _processId = value;
                    OnPropertyChanged(nameof(ProcessId));
                }
            }
        }

        public string ProcessName
        {
            get => _processName;
            set
            {
                if (_processName != value)
                {
                    _processName = value;
                    OnPropertyChanged(nameof(ProcessName));
                }
            }
        }

        public float Volume
        {
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return ProcessName ?? "Unknown Application";
        }
    }
}