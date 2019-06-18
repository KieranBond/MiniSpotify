using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSpotify.API.Base
{
    public interface IAPIRequestor
    {

        void Initialise();
        void Close();

        Task<string> Request(string a_reqPath, REST a_type = REST.GET);
    }
    public enum REST
    {
        GET,
        POST,
        PUT,
        DELETE
    };
}
