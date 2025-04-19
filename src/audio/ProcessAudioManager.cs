using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace MidiVolumeMixer.Audio
{
    public class ProcessAudioManager
    {
        private VolumeController volumeController;

        public ProcessAudioManager(VolumeController volumeController)
        {
            this.volumeController = volumeController;
        }

        public List<Process> GetRunningProcesses()
        {
            return Process.GetProcesses().ToList();
        }

        public void SetVolumeForProcess(string processName, int volumePercent)
        {
            volumeController.SetVolumeForApplication(processName, volumePercent);
        }

        public void SetVolumeForMultipleProcesses(Dictionary<string, int> processVolumes)
        {
            foreach (var entry in processVolumes)
            {
                SetVolumeForProcess(entry.Key, entry.Value);
            }
        }
    }
}