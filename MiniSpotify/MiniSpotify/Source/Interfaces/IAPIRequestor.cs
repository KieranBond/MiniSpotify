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
    }
}
