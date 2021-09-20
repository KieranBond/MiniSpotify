using MiniSpotify.HelperScripts;
using MiniSpotify.Source.Interfaces;
using MiniSpotify.Source.VO;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TinYard.Extensions.CallbackTimer.API.Services;
using TinYard.Framework.Impl.Attributes;

namespace MiniSpotify.Source.Impl
{
    public class SpotifyService : ISpotifyService
    {
        [Inject]
        public ICallbackTimer CallbackTimer { get; private set; }

        public event Action<ContextualUpdateVO> OnPlaybackUpdated;

        private SpotifyClient _spotifyClient;
        private EmbedIOAuthServer _server;
        private string _authToken;
        private string _clientID;

        //https://developer.spotify.com/documentation/general/guides/scopes/
        private string[] _accessScopes = new string[]
        {
            Scopes.UserModifyPlaybackState,
            Scopes.Streaming,
            Scopes.UserReadRecentlyPlayed,
            Scopes.UserReadCurrentlyPlaying,
            Scopes.UserReadPlaybackState,
            Scopes.UserLibraryModify,
            Scopes.UserLibraryRead
        };

        private double _updateInterval = 0.5d;

        public SpotifyService(string clientID)
        {
            _clientID = clientID;
        }

        public async Task<bool> Connect()
        {
            if (!string.IsNullOrWhiteSpace(_authToken))
                return false;

            // Information:
            // https://github.com/JohnnyCrazy/SpotifyAPI-NET
            // https://johnnycrazy.github.io/SpotifyAPI-NET/auth/implicit_grant.html

            int redirectPort = 4002;
            string redirectURI = $"http://localhost:{redirectPort}";

            _server = new EmbedIOAuthServer(new Uri(redirectURI), redirectPort);
            await _server.Start();

            var auth = new LoginRequest(
                new Uri(redirectURI),
                _clientID,
                LoginRequest.ResponseType.Token)
            {
                Scope = _accessScopes,
            };

            BrowserUtil.Open(auth.ToUri());

            await AwaitImplicitGrantReceived();

            return true;
        }

        public void SetupUpdate()
        {
            CallbackTimer.AddRecurringTimer(_updateInterval, Update);
        }

        private async Task AwaitImplicitGrantReceived()
        {
            bool grantReceived = false;

            _server.ImplictGrantReceived += async (sender, response) =>
            {
                await _server.Stop();

                _authToken = response.AccessToken;

                _spotifyClient = new SpotifyClient(_authToken);

                grantReceived = true;

                // Trigger a UI catchup
                UpdatePlayback();
            };

            while (!grantReceived)
            {
                await Task.Delay(300);
            }
        }

        private void Update()
        {
            if (_spotifyClient == null)
                return;

            UpdatePlayback();
        }

        public void Disconnect()
        {
            _server?.Dispose();
        }

        private async void UpdatePlayback()
        {
            var currentPlayback = await GetCurrentPlayback();

            float songProgress = currentPlayback != null ? currentPlayback.ProgressMs : 0f;
            FullTrack latestSong;
            if (currentPlayback == null || currentPlayback.Item == null)
                latestSong = await GetLastPlayedSong();
            else
                latestSong = currentPlayback.Item as FullTrack;

            if (latestSong == null)
                return;

            bool songLiked = await IsSongLiked(latestSong);
            bool isSongPlaying = await IsSongPlaying(latestSong);
            string artworkURL = GetSongArtworkUrl(latestSong);
            string playingContext = await GetPlayingContext();
            songProgress = LerpEaser.GetLerpT(LerpEaser.EaseType.Linear, songProgress, latestSong.DurationMs);// Normalize
            ContextualUpdateVO updateVO = new ContextualUpdateVO(latestSong, artworkURL, playingContext, songLiked, isSongPlaying, songProgress);

            OnPlaybackUpdated.Invoke(updateVO);
        }

        private async Task<string> GetPlayingContext()
        {
            var playbackContext = await GetCurrentPlayback();
            if (playbackContext == null || playbackContext.Context == null)
                return string.Empty;

            string id = playbackContext.Context?.Uri.Split(':').Last();
            switch (playbackContext.Context.Type)
            {
                case "album":
                    return (await _spotifyClient.Albums.Get(id)).Name;
                case "artist":
                    return (await _spotifyClient.Artists.Get(id)).Name;
                case "playlist":
                    return (await _spotifyClient.Playlists.Get(id)).Name;
                default:
                    return string.Empty;
            }
        }

        private async Task<FullTrack> GetLastPlayedSong()
        {
            var currentlyPlaying = await GetCurrentPlayback();
            if(currentlyPlaying != null && currentlyPlaying.IsPlaying)
            {
                return currentlyPlaying.Item as FullTrack;
            }

            var recentlyPlayed = await _spotifyClient.Player.GetRecentlyPlayed();
            var historyItem = recentlyPlayed.Items?[0];

            if (historyItem == null)
                return null;

            return await _spotifyClient.Tracks.Get(historyItem.Track.Id);
        }

        public async Task<CurrentlyPlayingContext> GetCurrentPlayback()
        {
            CurrentlyPlayingContext currentPlayback;
            try
            {
                currentPlayback = await _spotifyClient.Player.GetCurrentPlayback();
            }
            catch
            {
                // TODO : Add error handling / logging
                return null;
            }

            if (currentPlayback == null || currentPlayback.Item == null)
                return null;

            return currentPlayback;
        }

        public async Task<bool> IsSongLiked(FullTrack song)
        {
            try
            {
                var likedTracks = await _spotifyClient.Library.GetTracks();
                var fullPlaylist = await _spotifyClient.PaginateAll(likedTracks);

                return fullPlaylist.Any(track => track.Track.Id == song.Id);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsSongPlaying(FullTrack song)
        {
            var playback = await GetCurrentPlayback();
            if (playback == null || !playback.IsPlaying)
                return false;

            if (!(playback.Item is FullTrack))
                return false;

            return (playback.Item as FullTrack).Id == song.Id;
        }

        public string GetSongArtworkUrl(FullTrack song)
        {
            return song.Album.Images[0].Url;
        }

        public async Task<float> GetCurrentSongProgress()
        {
            var playback = await GetCurrentPlayback();
            if (playback == null)
                return 0f;

            return playback.ProgressMs;
        }

        public async Task<bool> ToggleLikeCurrentSong()
        {
            var currentSong = await GetLastPlayedSong();
            try
            {
                var tracksToModify = new List<string>() { currentSong.Id };
                if(await IsSongLiked(currentSong))
                {
                    await _spotifyClient.Library.RemoveTracks(new LibraryRemoveTracksRequest(tracksToModify));
                }
                else
                {
                    await _spotifyClient.Library.SaveTracks(new LibrarySaveTracksRequest(tracksToModify));
                }   

                return await IsSongLiked(currentSong);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TogglePlayingStatus()
        {
            var playbackContext = await GetCurrentPlayback();
            try
            {
                if (playbackContext.IsPlaying)
                    await _spotifyClient.Player.PausePlayback();
                else
                    await _spotifyClient.Player.ResumePlayback();
                return (await GetCurrentPlayback()).IsPlaying;
            }
            catch
            {
                return false;
            }
        }

        public async void PlayNextSong()
        {
            try
            {
                await _spotifyClient.Player.SkipNext();
            }
            catch
            {
                // TODO : Log something
            }
        }

        public async void PlayPreviousSong()
        {
            try
            {
                await _spotifyClient.Player.SkipPrevious();
            }
            catch
            {
                // TODO : Log something
            }
        }

        public async void RestartCurrentSong()
        {
            try
            {
                await _spotifyClient.Player.SeekTo(new PlayerSeekToRequest(0));
            }
            catch
            {

            }
        }
    }
}
