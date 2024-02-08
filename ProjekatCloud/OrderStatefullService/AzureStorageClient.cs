using Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderStatefullService
{
    public class AzureStorageClient
    {
        private readonly CloudStorageAccount storageAccount;

        public AzureStorageClient(string connectionString)
        {
            this.storageAccount = CloudStorageAccount.Parse(connectionString);
        }

        private CloudTable GetPorudzbineTableReference()
        {
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable ordersTable = tableClient.GetTableReference("Porudzbine");

            // Kreiraj tabelu ako ne postoji
            ordersTable.CreateIfNotExists();

            return ordersTable;
        }

        public async Task AddPorudzbina(int idPorudzbina, string emailKorisnika,string opis, double cijenaProizvoda, int proizvodId, string proizvodNazivProizvoda, int proizvodKolicinaProizvoda, string nacinPlacanja, double ukupnaCijena)
        {

            try
            {
                CloudTable ordersTable = GetPorudzbineTableReference();

                PorudzbinaEntity porudzbinaEntity = new PorudzbinaEntity()
                {
                    IdPorudzbine = idPorudzbina,
                    IdProizvoda = proizvodId,
                    NazivProizvoda = proizvodNazivProizvoda,
                    OpisProizvoda = opis,
                    CijenaProizvoda = cijenaProizvoda,
                    KolicinaProizvoda = proizvodKolicinaProizvoda,
                    KorisnikEmail = emailKorisnika,
                    UkupnaCijenaPorudzbine = ukupnaCijena
                };

                // Kreirajte operaciju za dodavanje entiteta u tabelu
                TableOperation insertOperation = TableOperation.Insert(porudzbinaEntity);

                // Izvršite operaciju dodavanja entiteta u tabelu
                await ordersTable.ExecuteAsync(insertOperation);
            }
            catch (Exception ex)
            {
                // Logovanje grešaka (opciono)
                Console.WriteLine($"Greška pri dodavanju korisnika: {ex.Message}");
                throw; // Propagiranje izuzetka dalje
            }
        }

        public async Task<List<PorudzbinaEntity>> GetPorudzbineZaKorisnika(string emailKorisnika)
        {
            try
            {
                CloudTable ordersTable = GetPorudzbineTableReference();

                // Kreirajte upit za čitanje porudžbina za određenog korisnika
                TableQuery<PorudzbinaEntity> query = new TableQuery<PorudzbinaEntity>()
                    .Where(TableQuery.GenerateFilterCondition("KorisnikEmail", QueryComparisons.Equal, emailKorisnika));

                var porudzbine = new List<PorudzbinaEntity>();

                // Izvršite upit nad tabelom
                TableContinuationToken token = null;
                do
                {
                    TableQuerySegment<PorudzbinaEntity> resultSegment = await ordersTable.ExecuteQuerySegmentedAsync(query, token);
                    token = resultSegment.ContinuationToken;

                    porudzbine.AddRange(resultSegment.Results);
                }
                while (token != null);

                return porudzbine;
            }
            catch (Exception ex)
            {
                // Logovanje grešaka (opciono)
                Console.WriteLine($"Greška pri čitanju porudžbina za korisnika: {ex.Message}");
                throw; // Propagiranje izuzetka dalje
            }
        }

    }
}
