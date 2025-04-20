using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace MidiVolumeMixer.Utils
{
    public class TrayIcon : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly Window _window;
        private bool _isDisposed = false;

        public TrayIcon(Window window)
        {
            _window = window;
            _notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location),
                Text = "MIDI Volume Mixer",
                Visible = false
            };

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            
            // Open menu item
            var openMenuItem = new ToolStripMenuItem("Open");
            openMenuItem.Click += (s, e) => ShowWindow();
            contextMenu.Items.Add(openMenuItem);
            
            // Separator
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // Exit menu item
            var exitMenuItem = new ToolStripMenuItem("Exit");
            exitMenuItem.Click += (s, e) => 
            {
                _notifyIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            };
            contextMenu.Items.Add(exitMenuItem);
            
            _notifyIcon.ContextMenuStrip = contextMenu;
            
            // Double-click to show window
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();
        }

        public void ShowInTray()
        {
            _notifyIcon.Visible = true;
            _window.Hide();
        }

        public void ShowWindow()
        {
            _window.Show();
            _window.WindowState = WindowState.Normal;
            _window.Activate();
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
                    _notifyIcon.Dispose();
                }
                _isDisposed = true;
            }
        }

        ~TrayIcon()
        {
            Dispose(false);
        }
    }
}