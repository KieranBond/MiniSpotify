using MiniSpotify.API.Impl;
using SpotifyAPI.Web.Models;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ColorConverter = System.Windows.Media.ColorConverter;
using Color = System.Windows.Media.Color;

namespace MiniSpotify
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool m_pinnedToTop = true;

        private bool m_editWindowOpen = false;
        private double m_editWindowGap = 10;
        EditorWindow editWindow;

        public MainWindow()
        {
            InitializeComponent();

            editWindow = new EditorWindow();
            editWindow.Hide();

            APIRequestor.Instance.m_onSongChanged += OnSongChanged;
            APIRequestor.Instance.m_onAuthComplete += UpdateUI;
            APIRequestor.Instance.m_onAPIPolled += UpdateUI;

            LocationChanged += UpdateEditWindowPosition;
        }

        public void UpdateUI(FullTrack a_latestTrack = null)
        {
            bool playing = APIRequestor.Instance.GetIsPlaying();
            UpdatePlayIcon(playing);

            float progress = APIRequestor.Instance.GetLatestSongProgress();
            UpdateProgressBar(progress);

            string artworkURL = APIRequestor.Instance.GetCurrentSongArtwork();
            if (string.IsNullOrEmpty(artworkURL))
            {
                Console.WriteLine("No track playing. Getting last track.");
                try
                {
                    if (APIRequestor.Instance.GetLatestTrack().Id != null)
                    {
                        artworkURL = APIRequestor.Instance.GetLatestTrack().Id;//Get the track ID
                    }
                }catch(NullReferenceException e)// ID returned a hard null and not normal null
                {
                    Console.WriteLine(e.StackTrace);
                    artworkURL = null;
                }
                artworkURL = APIRequestor.Instance.GetSongArtwork(artworkURL);//Get the artwork url
            }

            if(!string.IsNullOrEmpty(artworkURL))
            {
                UpdateDisplayImage(artworkURL);
            }

            if(a_latestTrack == null)
            {
                a_latestTrack = APIRequestor.Instance.GetLatestTrack();
            }

            if(a_latestTrack != null)
            {
                string trackName = a_latestTrack.Name;
                string artists = a_latestTrack.Artists[0].Name;

                for (int i = 1; i < a_latestTrack.Artists.Count; i++)
                {
                    artists = string.Concat(artists, ", ", a_latestTrack.Artists[i].Name);
                }

                this.Dispatcher.Invoke(() =>
                {
                    //Update any UI in this block.
                    TitleText.Text = a_latestTrack.Name;
                    ArtistText.Text = artists;
                });
            }
        }

        private void OnSongChanged(FullTrack a_latestTrackPlaying)
        {
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

        public void OnClickLike(object a_sender, RoutedEventArgs a_args)
        {
            throw new NotImplementedException();
        }

        public void OnClickPlayPause(object a_sender, RoutedEventArgs a_args)
        {
            APIRequestor.Instance.ModifyPlayback();//Returns true if now playing, else false
            UpdatePlayIcon(APIRequestor.Instance.GetIsPlaying());
        }

        public void OnClickNextSong(object a_sender, RoutedEventArgs a_args)
        {
            APIRequestor.Instance.SkipSongPlayback(true);
        }
        public void OnClickPreviousSong(object a_sender, RoutedEventArgs a_args)
        {
            APIRequestor.Instance.SkipSongPlayback(false);
        }

        public void OnClickEditorButton(object a_sender, RoutedEventArgs a_args)
        {
            m_editWindowOpen = !m_editWindowOpen;


            if (m_editWindowOpen)
            {
                editWindow.Show();
                double mainWidth = RootWindow.ActualWidth;
                double editWindowX = Application.Current.MainWindow.Left + mainWidth;
                double editWindowY = Application.Current.MainWindow.Top;
                editWindow.Top = editWindowY;
                editWindow.Left = editWindowX + m_editWindowGap;

                this.Dispatcher.Invoke(() =>
                {
                    //Update any UI in this block.
                    RotateTransform rotateTransform = new RotateTransform(180);
                    EditorButton.RenderTransform = rotateTransform;
                });


            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    //Update any UI in this block.
                    RotateTransform rotateTransform = new RotateTransform(0);
                    EditorButton.RenderTransform = rotateTransform;
                });

                editWindow.Hide();
            }
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

        private void UpdateProgressBar(float a_progress)
        {
            a_progress = 100 * a_progress;//a_progress is normalized between 0-1, *100 so that it's between 0 - 100
            this.Dispatcher.Invoke(() =>
            {
                SongProgress.Value = a_progress;
            });
        }

        private void UpdatePlayIcon(bool a_isPlaying)
        {
            this.Dispatcher.Invoke(() =>
            {
                //Always update UI like this.
                if (a_isPlaying)
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
            });
        }
        private void UpdateEditWindowPosition(object sender, EventArgs e)
        {
            double mainWidth = RootWindow.ActualWidth;
            double editWindowX = Application.Current.MainWindow.Left + mainWidth;
            double editWindowY = Application.Current.MainWindow.Top;
            editWindow.Top = editWindowY;
            editWindow.Left = editWindowX + m_editWindowGap;
        }


        #region Window Bar controls
        private void OnClickClose(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Application.Current.MainWindow.DragMove();
            }
        }

        private void OnClickMinimise(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void OnClickPinButton(object sender, RoutedEventArgs e)
        {
            m_pinnedToTop = !m_pinnedToTop;
            Application.Current.MainWindow.Topmost = m_pinnedToTop;

            this.Dispatcher.Invoke(() =>
            {
                ImageBrush pinnedBrush = new ImageBrush();
                if (m_pinnedToTop)
                {
                    pinnedBrush.ImageSource = new BitmapImage(new Uri("Assets/Images/Icons/pin-icon.png", UriKind.Relative));
                }
                else
                {
                    pinnedBrush.ImageSource = new BitmapImage(new Uri("Assets/Images/Icons/pin-icon-red.png", UriKind.Relative));
                }

                PinToTopButton.Background = pinnedBrush;
            });
        }

        private void OnChangeWindowBackgroundColour(string a_colourHexCode)
        {
            if(string.IsNullOrEmpty(a_colourHexCode))
            {
                Color val = (Color)ColorConverter.ConvertFromString(a_colourHexCode);
                Application.Current.MainWindow.Background = new SolidColorBrush(val); ;
            }
        }

        #endregion

    }
}
