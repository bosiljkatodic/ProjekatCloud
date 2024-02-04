using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]

    public class Korpa
    {
        [DataMember]

        public List<Proizvod> Proizvodi { get; set; } = new List<Proizvod>();
        [DataMember]

        public Korisnik Kupac { get; set; } = new Korisnik();
    }
}
