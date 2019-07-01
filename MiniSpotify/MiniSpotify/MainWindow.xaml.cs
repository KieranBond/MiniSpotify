using MiniSpotify.API.Base;
using MiniSpotify.API.Impl;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MiniSpotify
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            APIRequestor.Instance.m_onSongChanged += OnSongChanged;
        }

        private void OnSongChanged(FullTrack a_latestTrackPlaying)
        {
            //Update the information shown about current song.
            Console.WriteLine(a_latestTrackPlaying.Album.Name);

            string artworkURL = APIRequestor.Instance.GetCurrentSongArtwork();
            if (string.IsNullOrEmpty(artworkURL))
            {
                Console.WriteLine("No track playing. Cannot update image.");
            }
            else
            {
                UpdateDisplayImage(artworkURL);
            }

            string trackName = a_latestTrackPlaying.Name;
            string artists = a_latestTrackPlaying.Artists[0].Name;

            for(int i = 1; i < a_latestTrackPlaying.Artists.Count; i++)
            {
                artists = string.Concat(artists, ", ", a_latestTrackPlaying.Artists[i].Name);
            }

            this.Dispatcher.Invoke(() =>
            {
                //Update any UI in this block.
                TitleText.Text = a_latestTrackPlaying.Name;
                ArtistText.Text = artists;
            });
        }
        public void OnClickPlayPause(object a_sender, RoutedEventArgs a_args)
        {
            bool playing = APIRequestor.Instance.ModifyPlayback();//Returns true if now playing, else false

            if(playing)
            {
                PlaybackButton.IsEnabled = false;
                PlaybackButton.Visibility = Visibility.Hidden;

                PauseButton.IsEnabled = true;
                PauseButton.Visibility = Visibility.Visible;
            }
            else
            {
                PlaybackButton.IsEnabled = true;
                PlaybackButton.Visibility = Visibility.Visible;

                PauseButton.IsEnabled = false;
                PauseButton.Visibility = Visibility.Hidden;
            }

        }

        public void OnClickNextSong(object a_sender, RoutedEventArgs a_args)
        {
            APIRequestor.Instance.SkipSongPlayback(true);
        }
        public void OnClickPreviousSong(object a_sender, RoutedEventArgs a_args)
        {
            APIRequestor.Instance.SkipSongPlayback(false);
        }
        private async void UpdateDisplayImage(string a_artworkURL)
        {
            if (!string.IsNullOrEmpty(a_artworkURL))
            {
                Uri artworkAbsURI = new Uri(a_artworkURL, UriKind.Absolute);

                WebRequest request = WebRequest.Create(artworkAbsURI);
                WebResponse response = await request.GetResponseAsync();

                Stream responseStream = response.GetResponseStream();

                Bitmap art = new Bitmap(responseStream);
                BitmapImage bmpImage = new BitmapImage();

                using (MemoryStream memory = new MemoryStream())
                {
                    art.Save(memory, ImageFormat.Png);
                    memory.Position = 0;
                    bmpImage.BeginInit();
                    bmpImage.StreamSource = memory;
                    bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                    bmpImage.EndInit();
                }

                bmpImage.Freeze();
                this.Dispatcher.Invoke(() =>
                {
                    //Update any UI in this block.
                    AlbumArtworkImage.Source = bmpImage;
                });

            }
        }
    }
}
