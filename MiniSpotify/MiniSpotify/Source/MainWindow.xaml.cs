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
using MiniSpotify.Source.Interfaces;
using TinYard.Framework.Impl.Attributes;
using SpotifyAPI.Web;
using MiniSpotify.Source.VO;

namespace MiniSpotify
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [Inject]
        public ISpotifyService Service { get; private set; }
        
        private FullTrack _latestSong;
        private bool m_pinnedToTop = true;

        private bool m_editWindowOpen = false;
        private double m_editWindowGap = 10;
        EditorWindow editWindow;

        public MainWindow()
        {
            InitializeComponent();

            editWindow = new EditorWindow();
            editWindow.Hide();

            LocationChanged += UpdateEditWindowPosition;
        }

        public void Setup()
        {
            Service.OnPlaybackUpdated += OnPlaybackUpdated;
            Closing += (e, f) => Service.Disconnect();
        }

        private void OnPlaybackUpdated(ContextualUpdateVO playingContext)
        {
            UpdateProgressBar(playingContext.latestSongProgress);
            UpdatePlayIcon(playingContext.IsSongPlaying);
            UpdateLikeIcon(playingContext.IsSongLiked);

            //We don't need to update these every time
            if(_latestSong != playingContext.LatestSong)
            {
                UpdateDisplayImage(playingContext.LatestSongArtworkURL);
                UpdateTrackName(playingContext.LatestSong);
                UpdateArtists(playingContext.LatestSong);
                UpdatePlaybackContext(playingContext.PlaybackContext);

                _latestSong = playingContext.LatestSong;
            }
        }

        private void UpdatePlaybackContext(string playbackContext)
        {
            this.Dispatcher.Invoke(() =>
            {
                ContextText.Text = playbackContext;
            });
        }

        public void OnClickLike(object a_sender, RoutedEventArgs a_args)
        {
            Service.ToggleLikeCurrentSong();
        }

        public void OnClickPlayPause(object a_sender, RoutedEventArgs a_args)
        {
            Service.TogglePlayingStatus();
        }

        public void OnClickNextSong(object a_sender, RoutedEventArgs a_args)
        {
            Service.PlayNextSong();
        }

        public void OnClickPreviousSong(object a_sender, RoutedEventArgs a_args)
        {
            if(SongProgress.Value > 0f)
            {
                Service.RestartCurrentSong();
            }
            else
            {
                Service.PlayPreviousSong();
            }
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
                    //EditorButton.RenderTransform = rotateTransform;
                });


            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    //Update any UI in this block.
                    RotateTransform rotateTransform = new RotateTransform(0);
                    //EditorButton.RenderTransform = rotateTransform;
                });

                editWindow.Hide();
            }
        }

        private async void UpdateDisplayImage(string artworkUrl)
        {
            if (string.IsNullOrWhiteSpace(artworkUrl))
                return;

            Uri artworkAbsURI = new Uri(artworkUrl, UriKind.Absolute);

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
                bmpImage.DecodePixelWidth = 512;
                bmpImage.DecodePixelHeight = 512;
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

        private void UpdateProgressBar(float progress)
        {
            this.Dispatcher.Invoke(() =>
            {
                SongProgress.Value = progress;
            });
        }

        private void UpdateLikeIcon(bool a_isLiked)
        {
            this.Dispatcher.Invoke(() =>
            {
                //Always update UI like this.
                if (a_isLiked)
                {
                    LikeSongButton.IsEnabled = false;
                    LikeSongButton.Visibility = Visibility.Hidden;

                    UnlikeSongButton.IsEnabled = true;
                    UnlikeSongButton.Visibility = Visibility.Visible;
                }
                else
                {
                    LikeSongButton.IsEnabled = true;
                    LikeSongButton.Visibility = Visibility.Visible;

                    UnlikeSongButton.IsEnabled = false;
                    UnlikeSongButton.Visibility = Visibility.Hidden;
                }
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

        private void UpdateTrackName(FullTrack a_latestTrackPlaying)
        {
            string trackName = a_latestTrackPlaying.Name;
            this.Dispatcher.Invoke(() =>
            {
                TitleText.Text = a_latestTrackPlaying.Name;
            });
        }

        private void UpdateArtists(FullTrack a_latestTrackPlaying)
        {
            string artists = a_latestTrackPlaying.Artists[0].Name;
            for (int i = 1; i < a_latestTrackPlaying.Artists.Count; i++)
            {
                artists = string.Concat(artists, ", ", a_latestTrackPlaying.Artists[i].Name);
            }
            this.Dispatcher.Invoke(() =>
            {
                ArtistText.Text = artists;
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
