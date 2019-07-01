using MiniSpotify.API.Base;
using MiniSpotify.API.Impl;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
            if(!string.IsNullOrEmpty(artworkURL))
            {
                BitmapImage bmpImg = new BitmapImage(new Uri(artworkURL, UriKind.Absolute));
                AlbumArtworkImage.Source = bmpImg;
            }

        }

        public void OnClickPlayPause(object a_sender, RoutedEventArgs a_args)
        {
            APIRequestor.Instance.ModifyPlayback();
        }

        public void OnClickNextSong(object a_sender, RoutedEventArgs a_args)
        {
            APIRequestor.Instance.SkipSongPlayback(true);
        }
        public void OnClickPreviousSong(object a_sender, RoutedEventArgs a_args)
        {
            APIRequestor.Instance.SkipSongPlayback(false);
        }

        private void UpdateDisplayImage()
        {

        }
    }
}
