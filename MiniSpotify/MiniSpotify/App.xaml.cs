using MiniSpotify.API.Impl;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MiniSpotify
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            APIRequestor.Instance.Initialise();
        }
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            APIRequestor.Instance.Close();
        }
    }
}
