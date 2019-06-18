using MiniSpotify.API.Base;
using MiniSpotify.API.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MiniSpotify
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private APIRequestor m_apiRequestor;
        public MainWindow()
        {
            InitializeComponent();

            //m_apiRequestor = new APIRequestor();
            //m_apiRequestor.Initialise();
        }

        public void OnClickPlay(object a_sender, RoutedEventArgs a_args)
        {
            APIRequestor.Instance.ResumePlayback();

            //Send our API request for what is playing.
            //Task<string> request = m_apiRequestor.Request("", REST.GET);
            //if(request != null)
            //{
            //    request.Wait();//Wait for it to return.
            //
            //    string reqContent = request.Result;
            //    Console.WriteLine(reqContent);
            //}
        }
    }
}
