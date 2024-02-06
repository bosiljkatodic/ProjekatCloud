using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common
{

    public class KorisnikEntity : TableEntity
    {
        public KorisnikEntity()
        {
            // Postavljanje particionog ključa i rednog ključa
            this.PartitionKey = "Korisnici";
            this.RowKey = Guid.NewGuid().ToString();
        }

       // public int Id { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public string Email { get; set; }
        public string Lozinka { get; set; }

    }

}
