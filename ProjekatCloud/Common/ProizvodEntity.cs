using Microsoft.WindowsAzure.Storage.Table;
using System;
namespace Common
{
    public class ProizvodEntity : TableEntity
    {
        public ProizvodEntity()
        {
            // PartitionKey i RowKey trebaju biti postavljeni kako bi se jedinstveno identifikovao entitet u tabeli.
            this.PartitionKey = "Proizvodi";
            this.RowKey = Guid.NewGuid().ToString();
        }

        public int Id { get; set; }
        public string KategorijaProizvoda { get; set; }
        public string NazivProizvoda { get; set; }
        public string OpisProizvoda { get; set; }
        public double CijenaProizvoda { get; set; }
        public int KolicinaProizvoda { get; set; }
    }
}
