using MiniSpotify.Source.Impl;
using MiniSpotify.Source.Interfaces;
using System;
using TinYard.API.Interfaces;
using TinYard.Framework.Impl.Attributes;

namespace MiniSpotify
{
    public class MiniSpotifyConfig : IConfig
    {
        [Inject]
        public IContext context;

        public object Environment => null;

        private SpotifyService _spotifyService;

        private string _clientID = "93f2598a9eaf4056b34f7b5ca254ff17";

        private event Action _onServiceConnected;

        public void Configure()
        {
            _spotifyService = new SpotifyService(_clientID);
            context.Mapper.Map<ISpotifyService>().ToValue(_spotifyService);

            context.PostInitialize += OnContextInitialized;
        }

        private async void OnContextInitialized()
        {
            await _spotifyService.Connect();
            _onServiceConnected.Invoke();
            _spotifyService.SetupUpdate();
        }

        public MiniSpotifyConfig OnServiceConnected(Action callback)
        {
            _onServiceConnected += callback;

            return this;
        }
    }
}
