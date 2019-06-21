using Microsoft.Win32;
using MiniSpotify.API.Base;
using MiniSpotify.Source.Helpers;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public Action<FullTrack> m_onSongChanged;
        private int m_songChangePollDelayMS = 10000;//10 seconds

        private string m_baseFilePath = "\\Assets\\Files\\URLPath.txt";
        private string m_authFilePath = "\\Assets\\Files\\AuthorisePath.txt";
        //private string m_tokenFilePath = "\\Assets\\Files\\AuthToken.txt";
        private string m_baseURL = default;
        private string m_authURL = default;

        private string m_clientID = "93f2598a9eaf4056b34f7b5ca254ff17";
        private string m_clientSecret = "e39c12c66fa249cea93c3015619346f5";

        //https://developer.spotify.com/documentation/general/guides/scopes/
        //private string m_accessScopes = "user-modify-playback-state";//These are seperated by %20's
        private Scope m_accessScopes = Scope.UserModifyPlaybackState | Scope.Streaming | Scope.UserReadRecentlyPlayed | Scope.UserReadCurrentlyPlaying | Scope.UserReadPlaybackState;
        private HttpClient m_webClient;
        private static SpotifyWebAPI m_spotifyWebAPI;
        private Token m_authToken;

        public void Initialise()
        {
            m_instance = new APIRequestor();

            string webBase = FileHelper.GetFileText(m_baseFilePath);
            if (webBase != null && !string.IsNullOrEmpty(webBase))
            {
                m_instance.m_baseURL = webBase;
            }

            string authBase = FileHelper.GetFileText(m_authFilePath);
            if (authBase != null && !string.IsNullOrEmpty(authBase))
            {
                m_instance.m_authURL = authBase;
            }

            AuthAPI();
        }

        private async void AuthAPI()
        {
            //Source:
            //https://github.com/JohnnyCrazy/SpotifyAPI-NET

            //If token isn't set, or has expired
            if (m_instance.m_authToken == null || m_instance.m_authToken.IsExpired())
            {
                string redirectURI = "http://localhost:4002";

                AuthorizationCodeAuth auth = new AuthorizationCodeAuth(
                            m_instance.m_clientID,
                            m_instance.m_clientSecret,
                            redirectURI,
                            redirectURI,
                            m_accessScopes);

                auth.AuthReceived += async (sender, payload) =>
                {
                    auth.Stop(); //Sender is also the auth instance

                    m_instance.m_authToken = await auth.ExchangeCode(payload.Code);

                    m_spotifyWebAPI = new SpotifyWebAPI()
                    {
                        //TokenType = payload.TokenType,
                        TokenType = m_instance.m_authToken.TokenType,
                        //AccessToken = payload.AccessToken
                        AccessToken = m_instance.m_authToken.AccessToken,
                        UseAuth = true
                    };

                    m_pollingTask = Task.Run(PollSongChange);//Start the song change polling
                };

                auth.Start();//Starts an internal http server
                auth.OpenBrowser();//Opens brower to authenticate app
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

            if(m_pollingTask != null)
            {
                m_pollingTask.Dispose();
                m_pollingTask = null;
            }

            m_spotifyWebAPI.Dispose();
            m_spotifyWebAPI = null;

            
        }

        public bool ModifyPlayback()
        {
            //See if they're already listening to music.
            if (m_spotifyWebAPI.GetPlayingTrack().IsPlaying)
            {
                //Already listening to music, we'll modify and pause it
                ErrorResponse e = m_spotifyWebAPI.PausePlayback();

                if(e.HasError())
                {
                    Console.WriteLine(e.Error.Message);
                    return false;
                }

                return true;
            }
            else
            {
                //Not currently listening, so let's get it resumed
                //Get their last played track
                CursorPaging<PlayHistory> history = m_spotifyWebAPI.GetUsersRecentlyPlayedTracks();
                if (history.HasError())
                {
                    Console.WriteLine(history.Error.ToString());
                    return false;
                }
                else if (history.Items.Count > 0)
                {
                    //By not setting any variables, we continue playback from the last playlist, with the 
                    //last song listened to. 
                    ErrorResponse e = m_spotifyWebAPI.ResumePlayback("", "", null, "", 0);
                    if (e.HasError())
                    {
                        Console.WriteLine(e.Error.Message);
                    }
                    else
                    {
                        //Let whatever wants to know that the song has 'changed'.
                        //Because technically it hasn't, our polling method won't pick this up.
                        //But anything wanting to know about this, won't be told there's a song playing 
                        //so we'll trigger this ourself.
                        FullTrack lastSong = m_spotifyWebAPI.GetTrack(history.Items[0].Track.Id);
                        m_instance.m_onSongChanged.Invoke(lastSong);
                    }

                }

            }

            return true;
        }

        public bool SkipSongPlayback(bool a_nextSong = true)
        {
            //See if they're already listening to music.
            if (m_spotifyWebAPI.GetPlayingTrack().IsPlaying)
            {
                if (a_nextSong)
                    m_spotifyWebAPI.SkipPlaybackToNext();
                else
                    m_spotifyWebAPI.SkipPlaybackToPrevious();

                FullTrack latestSong = m_spotifyWebAPI.GetPlayback().Item;
                m_instance.m_onSongChanged.Invoke(latestSong);

                return true;
            }
            return false;
        }

        private async void PollSongChange()
        {
            //FullTrack lastTrack = m_spotifyWebAPI.GetPlayback().Item;
            FullTrack lastTrack = m_spotifyWebAPI.GetPlayingTrack().Item;
            while (m_spotifyWebAPI != null)
            {
                //FullTrack currentTrack = m_spotifyWebAPI.GetPlayback().Item;
                if (m_spotifyWebAPI.GetPlayingTrack().HasError())
                {
                    Console.WriteLine(m_spotifyWebAPI.GetPlayingTrack().Error);
                }
                FullTrack currentTrack = m_spotifyWebAPI.GetPlayingTrack().Item;
                
                if(lastTrack.HasError() || currentTrack.HasError())
                {
                    //Do something about error?
                }
                else if ((lastTrack == null && currentTrack != null) || lastTrack.Id != currentTrack.Id)
                {
                    //If the two track objs are different
                    m_instance.m_onSongChanged(currentTrack);
                    lastTrack = currentTrack;
                }

                await Task.Delay(m_songChangePollDelayMS);
            }
        }

        #region Our own API implement - Not using until fully functional.

        public async Task<string> Request(string a_reqPath, REST a_type = REST.GET)
        {
            string rest = FileHelper.GetRestString(a_type);
            // dispose it when done, so the app doesn't leak resources
            using (m_instance.m_webClient)
            {
                string uri = rest + "" + m_baseURL + "/" + a_reqPath;
                // Call asynchronous network methods in a try/catch block to handle exceptions
                try
                {
                    return await m_instance.m_webClient.GetStringAsync(uri);
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine("\nFailed to load request");
                    Console.WriteLine("Message :{0} ", e.Message);
                    return null;
                }
            }
        }


        //My one.. Might work now if we use local host, but using the github lib instead for now
        private async void Authenticate()
        {
            //https://developers.google.com/identity/protocols/OAuth2InstalledApp
            //https://github.com/googlesamples/oauth-apps-for-windows/blob/master/OAuthDesktopApp/OAuthDesktopApp/

            // Creates a redirect URI using an available port on the loopback address.
            //string redirectURI = string.Format("http://{0}/{1}/", "kieranbond.github.io", "minispotify");            //string redirectURI = string.Format("http://{0}/{1}/", "kieranbond.github.io", "minispotify");
            string redirectURI = "http://www.kieranbond.co.uk/minispotify/";
            //string redirectURI = string.Format("http://{0}:{1}", IPAddress.Loopback, GetRandomUnusedPort());
            string rest = FileHelper.GetRestString(REST.GET);

            // Creates an HttpListener to listen for requests on that redirect URI.
            HttpListener redirectListener = new HttpListener();
            redirectListener.Prefixes.Add(redirectURI);
            redirectListener.Start();
            //We need to be running in Admin mode for this request
            //to work - Otherwise we get an exception.
            //https://stackoverflow.com/questions/4019466/httplistener-access-denied

            string requestURI = string.Format("{0}?client_id={1}&response_type=code&" +
                "redirect_uri={2}&scope={3}", m_authURL, m_clientID, redirectURI, m_accessScopes);
            string restedRequestURI = rest + " " + requestURI;

            // Opens request in the browser.
            Process.Start(requestURI);

            // Waits for the OAuth authorization response.
            var context = await redirectListener.GetContextAsync();
            string code = context.Request.QueryString.Get("code");
            Console.WriteLine("Code: " + code);
            redirectListener.Stop();
            //var response = context.Response;
            //var responseStream = response.OutputStream;
            //Task responseTask = responseStream.WriteAsync(buffer, 0, buffer.Length).ContinueWith((task) =>
            //{
            //    responseStream.Close();
            //    redirectListener.Stop();
            //    Console.WriteLine("HTTP server stopped.");
            //});

        }

        #endregion
    }
}
