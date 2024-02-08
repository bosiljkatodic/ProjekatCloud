using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceContract]

    public interface IOrderStatefullService : IService
    {
        [OperationContract]
        public Task<bool> KreirajPorudzbinu(string emailKorisnika,IEnumerable<Proizvod> proizvodi, string nacinPlacanja, double ukupnaCijena);
        [OperationContract]
        public Task<IEnumerable<PorudzbinaEntity>> GetPorudzbineZaKorisnikaAsync(string emailKorisnika);

    }
}
