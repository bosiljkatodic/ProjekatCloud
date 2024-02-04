using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]

    public enum NacinPlacanja
    {
        PayPal,
        PlacanjePouzecem
    }

    public class Porudzbina
    {
        [DataMember]

        public List<Proizvod> Proizvodi { get; set; } = new List<Proizvod>();
        [DataMember]

        public NacinPlacanja NacinPlacanja { get; set; }
        [DataMember]
        public Korisnik Narucilac { get; set; } = new Korisnik();

        [DataMember]
        public double UkupnaCijena { get; set; }
        [DataMember]
        public int Id { get; set; }
    }
}
