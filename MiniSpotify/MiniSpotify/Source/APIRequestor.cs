using Microsoft.Win32;
using MiniSpotify.API.Base;
using MiniSpotify.HelperScripts;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace MiniSpotify.API.Impl
{
    public class APIRequestor : IAPIRequestor
    {
        private static APIRequestor m_instance = null;
        public static APIRequestor Instance
        {
            get
            {
                if (m_instance == null)//If no instance is set, create one.
                {
                    m_instance = new APIRequestor();
                }

                return m_instance;
            }
        }

        private Task m_pollingTask;

        private FullTrack m_latestTrack;

        public Action<FullTrack> m_onSongChanged;
        public Action<FullTrack> m_onAuthComplete;
        public Action<FullTrack> m_onAPIPolled;

        private int m_songChangePollDelayMS = /*10000*/1000;//1 second

        private string m_baseURL = default;
        private string m_authURL = default;

        private string m_clientID = "93f2598a9eaf4056b34f7b5ca254ff17";
        private string m_clientSecret = "";
        private string m_clientSecretPath = "\\Assets\\Files\\ClientSecret.txt";

        private string m_albumimage = "";

        //https://developer.spotify.com/documentation/general/guides/scopes/
        private Scope m_accessScopes = Scope.UserModifyPlaybackState | Scope.Streaming | Scope.UserReadRecentlyPlayed | Scope.UserReadCurrentlyPlaying | Scope.UserReadPlaybackState | Scope.UserLibraryModify | Scope.UserLibraryRead;
        private HttpClient m_webClient;
        private static SpotifyWebAPI m_spotifyWebAPI;
        private Token m_authToken;

        public void Initialise()
        {
            m_instance = new APIRequestor();

            //string secret = FileHelper.GetFileText(m_clientSecretPath);
            //if (!string.IsNullOrEmpty(secret))
            //{
            //    m_instance.m_clientSecret = secret;
            //}

            AuthAPI();
        }

        private async void AuthAPI()
        {
            //Source:
            //https://github.com/JohnnyCrazy/SpotifyAPI-NET
            //https://johnnycrazy.github.io/SpotifyAPI-NET/auth/implicit_grant.html

            if (m_instance.m_authToken == null || m_instance.m_authToken.IsExpired())
            {
                string redirectURI = "http://localhost:4002";

                ImplicitGrantAuth auth = new ImplicitGrantAuth(
                        m_instance.m_clientID,
                        redirectURI,
                        redirectURI,
                        m_accessScopes);

                auth.AuthReceived += async (sender, payload) =>
                {
                    auth.Stop(); // `sender` is also the auth instance

                    m_instance.m_authToken = payload;

                    m_spotifyWebAPI = new SpotifyWebAPI()
                    {
                        TokenType = m_instance.m_authToken.TokenType,
                        AccessToken = m_instance.m_authToken.AccessToken,
                        UseAuth = true
                    };

                    m_instance.m_pollingTask = Task.Run(PollSongChange);//Start the song change polling
                };

                auth.Start();//Starts an internal http server
                auth.OpenBrowser();//Opens browser to authenticate app
            }
        }

        public void Close()
        {
            //Dispose of anything we need to
            if (m_instance.m_webClient != null)
            {
                m_instance.m_webClient.Dispose();
                m_instance.m_webClient = null;
            }

            if (m_pollingTask != null)
            {
                m_pollingTask.Dispose();
                m_pollingTask = null;
            }

            if (m_spotifyWebAPI != null)
            {
                m_spotifyWebAPI.Dispose();
                m_spotifyWebAPI = null;
            }

        }

        public bool ModifyLike() //Return true if the song becomes liked. False otherwise.
        {
            FullTrack currentTrack = m_instance.GetLatestTrack();

            if (currentTrack != null)
            {
                List<string> currentID = new List<string> { currentTrack.Id };

                List<bool> list = m_spotifyWebAPI.CheckSavedTracks(currentID).List;

                if (list.Count == 1)
                {
                    if (list[0]) // If liked, dislike.
                    {
                        m_spotifyWebAPI.RemoveSavedTracks(currentID);
                        return false;
                    }
                    else // If disliked, like.
                    {
                        m_spotifyWebAPI.SaveTracks(currentID);
                        return true;
                    }
                }
                else
                    return false;
            }
            else
                return false;
        }

        public void ModifyPlayback()
        {
            //See if they're already listening to music.
            if (m_spotifyWebAPI.GetPlayingTrack().IsPlaying)
            {
                //Already listening to music, we'll modify and pause it
                ErrorResponse e = m_spotifyWebAPI.PausePlayback();

                if (e.HasError())
                {
                    Console.WriteLine(e.Error.Message);
                }
            }
            else
            {
                //Not currently listening, so let's get it resumed
                //Get their last played track
                CursorPaging<PlayHistory> history = m_spotifyWebAPI.GetUsersRecentlyPlayedTracks();
                if (history.HasError())
                {
                    Console.WriteLine(history.Error.ToString());
                }
                else if (history.Items.Count > 0)
                {
                    //By not setting any variables, we continue playback from the last playlist, with the 
                    //last song listened to. 
                    ErrorResponse e = m_spotifyWebAPI.ResumePlayback("", "", null, "", 0);
                    if (e.HasError())
                    {
                        Console.WriteLine(e.Error.Message);

                        if (e.Error.Status == 404)
                        {
                            List<Device> devices = m_spotifyWebAPI.GetDevices().Devices;

                            for (int i = 0; i < devices.Count; i++)
                            {
                                if (devices[i].IsRestricted == false)
                                {
                                    m_spotifyWebAPI.ResumePlayback(devices[i].Id, "", null, "", 0);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        //Let whatever wants to know that the song has 'changed'.
                        //Because technically it hasn't, our polling method won't pick this up.
                        //But anything wanting to know about this, won't be told there's a song playing 
                        //so we'll trigger this ourself.
                        FullTrack lastSong = m_spotifyWebAPI.GetPlayingTrack().Item;
                        m_instance.m_onSongChanged.Invoke(lastSong);

                    }
                }
            }
        }

        public bool SkipSongPlayback(bool a_nextSong = true)
        {
            //See if they're already listening to music.
            if (m_spotifyWebAPI.GetPlayingTrack().IsPlaying)
            {
                if (a_nextSong)
                {
                    try
                    {
                        m_spotifyWebAPI.SkipPlaybackToNext();
                    }
                    catch (ArgumentException e) // Spammed the button before the system could register it as event
                    {
                        Console.WriteLine(e.StackTrace);
                    }
                }
                else
                {
                    try
                    {
                        m_spotifyWebAPI.SkipPlaybackToPrevious();
                    }
                    catch (ArgumentException e) // Spammed the button before the system could register it as event
                    {
                        Console.WriteLine(e.StackTrace);
                    }
                }

                FullTrack latestSong = m_spotifyWebAPI.GetPlayback().Item;
                m_instance.m_onSongChanged.Invoke(latestSong);

                return true;
            }
            return false;
        }

        public string GetSongArtwork(string a_trackID)
        {
            if (m_spotifyWebAPI != null && !string.IsNullOrEmpty(a_trackID))
            {
                return m_spotifyWebAPI.GetTrack(a_trackID).Album.Images[0].Url;
            }

            return null;
        }
        public string GetCurrentSongArtwork()
        {
            string imageURL = null;

            if (m_spotifyWebAPI != null && m_spotifyWebAPI.GetPlayback().IsPlaying)
            {
                if (!m_spotifyWebAPI.GetPlayingTrack().HasError())
                {
                    try
                    {
                        imageURL = m_spotifyWebAPI.GetPlayingTrack().Item.Album.Images[0].Url;
                        m_albumimage = imageURL; //Set it as 'backup'
                    }
                    catch (Exception e) // Returned a hard null and not normal null
                    {
                        //Use backup image
                        if (m_albumimage != null)
                            imageURL = m_albumimage;

                        Console.WriteLine(e.StackTrace);
                    }
                }
                else if (m_albumimage != null)
                {
                    //Use backup image
                    imageURL = m_albumimage;
                }
            }

            return imageURL;
        }

        public FullTrack GetLatestTrack()
        {
            if (m_spotifyWebAPI != null)
            {
                if (m_spotifyWebAPI.GetPlayback().IsPlaying || m_spotifyWebAPI.GetPlayingTrack().Item != null)
                {
                    return m_spotifyWebAPI.GetPlayingTrack().Item;
                }
                else
                {
                    FullTrack latestTrack = m_spotifyWebAPI.GetPlayingTrack().Item;
                    if (latestTrack != null)
                    {
                        return latestTrack;
                    }
                    else if (m_spotifyWebAPI.GetUsersRecentlyPlayedTracks().Error == null)
                    {
                        CursorPaging<PlayHistory> history = m_spotifyWebAPI.GetUsersRecentlyPlayedTracks();
                        return m_spotifyWebAPI.GetTrack(history.Items[0].Track.Id);
                    }
                }
            }
            return null;
        }

        public float GetLatestSongProgress()
        {
            if (m_spotifyWebAPI != null)
            {
                try
                {
                    FullTrack currentTrack = Instance.GetLatestTrack();
                    float progress = (float)m_spotifyWebAPI.GetPlayback().ProgressMs / (float)currentTrack.DurationMs;
                    return progress;
                }
                catch (NullReferenceException e) //Unexpected spotify closed
                {
                    Console.WriteLine(e.StackTrace);
                    return -1;
                }
            }
            else
                return -1;
        }

        public bool GetSongIsLiked()
        {
            if (m_spotifyWebAPI != null)
            {
                try
                {
                    FullTrack currentTrack = Instance.GetLatestTrack();

                    List<string> toCheck = new List<string> { currentTrack.Id };

                    return m_spotifyWebAPI.CheckSavedTracks(toCheck).List[0];
                }
                catch (NullReferenceException e) //Unexpected spotify closed
                {
                    Console.WriteLine(e.StackTrace);
                    return false;
                }
            }

            return false;
        }

        public bool GetIsPlaying()
        {
            if (m_spotifyWebAPI != null)
            {
                return m_spotifyWebAPI.GetPlayback().IsPlaying;
            }

            return false;
        }

        public string GetPlaybackContext()
        {
            string InvalidReturn = string.Empty;

            if (m_spotifyWebAPI != null)
            {
                Context context = m_spotifyWebAPI.GetPlayback().Context;

                if (context == null)
                {
                    return InvalidReturn;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(context.Type))
                    {
                        return InvalidReturn;
                    }
                    else
                    {
                        string id = context.Uri.Split(':').Last();

                        switch (context.Type)
                        {
                            case "album":
                                return m_spotifyWebAPI.GetAlbum(id).Name;
                            case "artist":
                                return m_spotifyWebAPI.GetArtist(id).Name;
                            case "playlist":
                                return m_spotifyWebAPI.GetPlaylist(id).Name;
                            default:
                                return InvalidReturn;
                        }
                    }
                }
            }
            else
            {
                return InvalidReturn;
            }
        }

        private async void PollSongChange()
        {
            while (m_spotifyWebAPI != null)
            {
                FullTrack currentTrack = m_instance.GetLatestTrack();

                if (currentTrack != null)
                {
                    if (m_instance.m_latestTrack == null)
                    {
                        m_instance.m_onSongChanged(currentTrack);
                    }
                    else if (currentTrack != m_instance.m_latestTrack)
                    {
                        m_instance.m_onSongChanged(currentTrack);
                        m_instance.m_latestTrack = currentTrack;
                    }

                    m_instance.m_onAPIPolled(currentTrack);
                }

                //Update for next time.
                m_instance.m_latestTrack = m_instance.GetLatestTrack();

                await Task.Delay(m_songChangePollDelayMS);
            }
        }
        public void RepeatSongOn() //repeat the current track
        {
            m_spotifyWebAPI.SetRepeatMode(RepeatState.Track);
        }
        public void RepeatSongOff() //repeat the current track
        {
            m_spotifyWebAPI.SetRepeatMode(RepeatState.Off);
        }

        public void VolumeChangedEvent(double sv) //changed spotify volume not application volume
        {
            int val = Convert.ToInt32(sv);

            Console.WriteLine(val);
            m_spotifyWebAPI.SetVolume(val);
        }
    }
}