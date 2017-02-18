using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private MusicBeeApiInterface _mbApiInterface;
        private readonly PluginInfo _about = new PluginInfo();

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            _mbApiInterface = new MusicBeeApiInterface();
            _mbApiInterface.Initialise(apiInterfacePtr);

            _mbApiInterface.MB_AddMenuItem("mnuTools/Update AutoRating", "Tools: Update AutoRating", MenuItemUpdateAutoRating);

            _about.PluginInfoVersion = PluginInfoVersion;
            _about.Name = "AutoRating";
            _about.Description = "Automatically updates the rating of tracks based on your listening behaviour.";
            _about.Author = "NekuSoul";
            _about.TargetApplication = string.Empty;
            _about.Type = PluginType.General;

            _about.VersionMajor = 1;
            _about.VersionMinor = 0;
            _about.Revision = 1;

            _about.MinInterfaceVersion = MinInterfaceVersion;
            _about.MinApiRevision = MinApiRevision;

            _about.ReceiveNotifications = (ReceiveNotificationFlags.PlayerEvents | ReceiveNotificationFlags.TagEvents);
            _about.ConfigurationPanelHeight = 0;

            return _about;
        }

        private void MenuItemUpdateAutoRating(object sender, EventArgs eventArgs)
        {
            _mbApiInterface.Library_QueryFiles("domain=SelectedFiles");
            string nextFile = _mbApiInterface.Library_QueryGetNextFile();
            while (!string.IsNullOrEmpty(nextFile))
            {
                UpdateAutoRating(nextFile);
                nextFile = _mbApiInterface.Library_QueryGetNextFile();
            }
        }

        private void UpdateAutoRating(string file)
        {
            string playCountString = _mbApiInterface.Library_GetFileProperty(file, FilePropertyType.PlayCount);
            string skipCountString = _mbApiInterface.Library_GetFileProperty(file, FilePropertyType.SkipCount);

            int playCount = 0;
            int skipCount = 0;

            if (playCountString.Length > 0)
                playCount = int.Parse(playCountString);

            if (skipCountString.Length > 0)
                skipCount = int.Parse(skipCountString);

            int playDelta = playCount - skipCount;
            int newRating = -1;

            if (playDelta < -5)
                newRating = 1;
            else if (playDelta < -1)
                newRating = 2;
            else if (playDelta < 2)
                newRating = 3;
            else if (playDelta < 6)
                newRating = 4;
            else
                newRating = 5;

            _mbApiInterface.Library_SetFileTag(file, MetaDataType.Rating, newRating.ToString());
            _mbApiInterface.Library_CommitTagsToFile(file);
        }

        public bool Configure(IntPtr panelHandle)
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = _mbApiInterface.Setting_GetPersistentStoragePath();
            // panelHandle will only be set if you set about.ConfigurationPanelHeight to a non-zero value
            // keep in mind the panel width is scaled according to the font the user has selected
            // if about.ConfigurationPanelHeight is set to 0, you can display your own popup window
            if (panelHandle == IntPtr.Zero) return false;

            var configPanel = (Panel)Control.FromHandle(panelHandle);
            var prompt = new Label
            {
                AutoSize = true,
                Location = new Point(0, 0),
                Text = "prompt:"
            };
            var textBox = new TextBox();
            textBox.Bounds = new Rectangle(60, 0, 100, textBox.Height);
            configPanel.Controls.AddRange(new Control[] { prompt, textBox });
            return false;
        }

        // called by MusicBee when the user clicks Apply or Save in the MusicBee Preferences screen.
        // its up to you to figure out whether anything has changed and needs updating
        public void SaveSettings()
        {
            // save any persistent settings in a sub-folder of this path
            string dataPath = _mbApiInterface.Setting_GetPersistentStoragePath();
        }

        // MusicBee is closing the plugin (plugin is being disabled by user or MusicBee is shutting down)
        public void Close(PluginCloseReason reason)
        { }

        // uninstall this plugin - clean up any persisted files
        public void Uninstall()
        { }

        // receive event notifications from MusicBee
        // you need to set about.ReceiveNotificationFlags = PlayerEvents to receive all notifications, and not just the startup event
        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            if (type == NotificationType.PlayCountersChanged)
            {
                string file =_mbApiInterface.NowPlaying_GetFileUrl();
                if (file.Length > 0)
                {
                    UpdateAutoRating(file);
                }
            }
        }

        // return an array of lyric or artwork provider names this plugin supports
        // the providers will be iterated through one by one and passed to the RetrieveLyrics/ RetrieveArtwork function in order set by the user in the MusicBee Tags(2) preferences screen until a match is found
        public string[] GetProviders()
        {
            return null;
        }

        // return lyrics for the requested artist/title from the requested provider
        // only required if PluginType = LyricsRetrieval
        // return null if no lyrics are found
        public string RetrieveLyrics(string sourceFileUrl, string artist, string trackTitle, string album, bool synchronisedPreferred, string provider)
        {
            return null;
        }

        // return Base64 string representation of the artwork binary data from the requested provider
        // only required if PluginType = ArtworkRetrieval
        // return null if no artwork is found
        public string RetrieveArtwork(string sourceFileUrl, string albumArtist, string album, string provider)
        {
            //Return Convert.ToBase64String(artworkBinaryData)
            return null;
        }
    }
}