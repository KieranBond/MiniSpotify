using SpotifyAPI.Web;

namespace MiniSpotify.Source.VO
{
    public class ContextualUpdateVO
    {
        public readonly FullTrack LatestSong;
        public readonly string LatestSongArtworkURL;
        public readonly string PlaybackContext;
        public readonly bool IsSongLiked;
        public readonly bool IsSongPlaying;
        public readonly float latestSongProgress;

        public ContextualUpdateVO(
            FullTrack latestSong, 
            string latestSongArtworkUrl, 
            string playbackContext, 
            bool isSongLiked, 
            bool isSongPlaying,
            float latestSongProgress)
        {
            LatestSong = latestSong;
            LatestSongArtworkURL = latestSongArtworkUrl;
            PlaybackContext = playbackContext;
            IsSongLiked = isSongLiked;
            IsSongPlaying = isSongPlaying;
            this.latestSongProgress = latestSongProgress;
        }
    }
}
