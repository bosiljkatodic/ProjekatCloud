using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]

    public class Korisnik
    {
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string Ime { get; set; } = null!;
        [DataMember]
        public string Prezime { get; set; } = null!;
        [DataMember]
        public string Email { get; set; } = null!;
        [DataMember]
        public string Lozinka { get; set; } = null!;
    }
}
