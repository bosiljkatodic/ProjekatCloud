using Microsoft.WindowsAzure.Storage.Table;

namespace Common
{
    public class PorudzbinaEntity : TableEntity
    {
        public PorudzbinaEntity()
        {
            // Postavljanje PartitionKey na ID porudžbine i RowKey na ID proizvoda
            this.PartitionKey = "Porudzbine";
            this.RowKey = Guid.NewGuid().ToString();
        }

        public int IdProizvoda { get; set; }
        public int IdPorudzbine { get; set; }

        public string NazivProizvoda { get; set; }
        public string OpisProizvoda { get; set; }
        public double CijenaProizvoda { get; set; }
        public int KolicinaProizvoda { get; set; }
        public string KorisnikEmail { get; set; } // Email korisnika koji je napravio porudžbinu
        public double UkupnaCijenaPorudzbine { get; set; } // Ukupna cijena porudžbine
    }
}
