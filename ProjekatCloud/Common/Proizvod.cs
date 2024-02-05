using System.Runtime.Serialization;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Common
{
    [DataContract]

    public class Proizvod 
    {
        [DataMember]
        public int Id { get; set; }
        [DataMember]
        public string KategorijaProizvoda { get; set; } = null!;
        [DataMember]
        public string NazivProizvoda { get; set; } = null!;
        [DataMember]
        public string OpisProizvoda { get; set; } = null!;
        [DataMember]
        public double CijenaProizvoda { get; set; }
        [DataMember]
        public int KolicinaProizvoda { get; set; }
    }

}