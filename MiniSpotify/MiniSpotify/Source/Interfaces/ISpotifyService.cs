using MiniSpotify.Source.VO;
using SpotifyAPI.Web;
using System;
using System.Threading.Tasks;

namespace MiniSpotify.Source.Interfaces
{
    public interface ISpotifyService
    {
        event Action<ContextualUpdateVO> OnPlaybackUpdated;

        Task<bool> Connect();
        void Disconnect();

        Task<CurrentlyPlayingContext> GetCurrentPlayback();
        string GetSongArtworkUrl(FullTrack song);
        Task<float> GetCurrentSongProgress();
        Task<bool> IsSongLiked(FullTrack song);
        Task<bool> IsSongPlaying(FullTrack song);

        Task<bool> ToggleLikeCurrentSong();
        Task<bool> TogglePlayingStatus();
        void PlayNextSong();
        void PlayPreviousSong();
        void RestartCurrentSong();
    }
}
