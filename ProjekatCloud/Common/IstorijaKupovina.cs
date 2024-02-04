using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]

    public class IstorijaKupovina
    {
        [DataMember]

        public List<Porudzbina> Porudzbine { get; set; } = new List<Porudzbina>();
        [DataMember]

        public Korisnik Kupac { get; set; } = new Korisnik();
    }
}
