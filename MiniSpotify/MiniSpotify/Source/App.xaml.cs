using MiniSpotify.Source.Interfaces;
using System.Windows;
using System.Windows.Navigation;
using TinYard;
using TinYard.API.Interfaces;
using TinYard.Extensions.Bundles;
using TinYard.Extensions.CallbackTimer;

namespace MiniSpotify
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IContext _context;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _context = new Context();
            _context
                .Install(new MVCBundle())
                .Install(new CallbackTimerExtension())
                .Configure(new MiniSpotifyConfig()
                .OnServiceConnected(OnServiceReady));
            
            _context.Initialize();

        }

        private void OnServiceReady()
        {
            var window = MainWindow as MainWindow;
            _context.Injector.Inject(window);
            window.Setup();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            _context.Mapper.GetMappingValue<ISpotifyService>()?.Disconnect();
        }
    }
}
