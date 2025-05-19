using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Windows.Media.Playback;
using WindowsMediaController;
using static WindowsMediaController.MediaManager;

namespace DynamicWin.Utils
{
    /*
    *   Overview:
    *    - Allow user to interact with media controls inside a widget that implements it.
    *    
    *   Author:                 Florian Butz
    *   GitHub:                 https://github.com/FlorianButz
    *   Implementation Date:    3 August 2024
    *   Last Modified:          3 August 2024 12:46 CET (UTC+1)
    */

    public class MediaController
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const byte VK_MEDIA_NEXT_TRACK = 0xB0;
        private const byte VK_MEDIA_PREV_TRACK = 0xB1;

        public void PlayPause()
        {
            keybd_event(VK_MEDIA_PLAY_PAUSE, 0, 0, 0);
        }

        public void Next()
        {
            keybd_event(VK_MEDIA_NEXT_TRACK, 0, 0, 0);
        }

        public void Previous()
        {
            keybd_event(VK_MEDIA_PREV_TRACK, 0, 0, 0);
        }
    }

    /*
    *   Overview:
    *    - Allows the fetching of currently playing media, returning its artist name, media title, and its corresponding image.
    *    - Handles metadata mapping through Media class which returns the three mentioned information.
    *    
    *   Author:                 Megan Park
    *   GitHub:                 https://github.com/59xa
    *   Implementation Date:    19 May 2025
    *   Last Modified:          19 May 2025 16:12 KST (UTC+9)
    */

    // TO-DO: Not adding comments for now, as this implementation will be required for v1.4.0b
    public class MediaInfo
    {
        private static MediaInfo? _i;
        private static MediaManager _m;
        public static MediaInfo Instance => _i ??= new MediaInfo();

        public static Media? Current { get; private set; }

        public static async Task<Media?> FetchCurrentMediaAsync()
        {
            _m = new MediaManager();
            await _m.StartAsync();

            var _s = _m.GetFocusedSession();
            if (_s == null) return null;

            var _p = _s.ControlSession?.TryGetMediaPropertiesAsync()?.GetAwaiter().GetResult();
            if (_p == null) return null;

            BitmapImage? _i = null;
            if (_p.Thumbnail != null)
            {
                using var stream = await _p.Thumbnail.OpenReadAsync();
                _i = new BitmapImage();
                _i.BeginInit();
                _i.StreamSource = stream.AsStreamForRead();
                _i.CacheOption = BitmapCacheOption.OnLoad;
                _i.EndInit();
            }

            var result = new Media { Title = _p.Title, Artist = _p.Artist, Thumbnail = _i };

            Current = result;

            Debug.WriteLine("TITLE: {0}, ARTIST: {1}, IMAGE: {2}", _p.Title, _p.Artist, _p.Thumbnail);

            return result;
        }
    }

    public class Media
    {
        public string? Title { get; set; }
        public string? Artist { get; set; }
        public BitmapImage? Thumbnail { get; set; }
    }
}
