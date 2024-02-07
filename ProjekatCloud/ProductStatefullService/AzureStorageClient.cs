using Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductStatefullService
{
    public class AzureStorageClient
    {
        private readonly CloudStorageAccount storageAccount;

        public AzureStorageClient(string connectionString)
        {
            this.storageAccount = CloudStorageAccount.Parse(connectionString);
        }

        private CloudTable GetProizvodiTableReference()
        {
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable proizvodiTable = tableClient.GetTableReference("Proizvodi");

            // Kreiraj tabelu ako ne postoji
            proizvodiTable.CreateIfNotExists();

            return proizvodiTable;
        }

        public async Task<List<Proizvod>> GetAllProductsAsync()
        {
            try
            {
                CloudTable productsTable = GetProizvodiTableReference();

                // Kreiraj upit za čitanje svih proizvoda iz tabele
                TableQuery<ProizvodEntity> query = new TableQuery<ProizvodEntity>();

                // Izvrši upit i preuzmi rezultate
                TableContinuationToken continuationToken = null;
                List<Proizvod> allProducts = new List<Proizvod>();

                do
                {
                    var queryResult = await productsTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                    foreach (var proizvodEntity in queryResult.Results)
                    {
                        // Mapiraj ProizvodEntity na Proizvod
                        Proizvod proizvod = new Proizvod
                        {
                            Id = proizvodEntity.Id,
                            KategorijaProizvoda = proizvodEntity.KategorijaProizvoda,
                            NazivProizvoda = proizvodEntity.NazivProizvoda,
                            OpisProizvoda = proizvodEntity.OpisProizvoda,
                            CijenaProizvoda = proizvodEntity.CijenaProizvoda,
                            KolicinaProizvoda = proizvodEntity.KolicinaProizvoda
                        };
                        allProducts.Add(proizvod);
                    }
                    continuationToken = queryResult.ContinuationToken;
                }
                while (continuationToken != null);

                return allProducts;
            }
            catch (Exception ex)
            {
                // Logovanje grešaka (opciono)
                Console.WriteLine($"Greška pri čitanju svih proizvoda: {ex.Message}");
                throw; // Propagiranje izuzetka dalje
            }
        }


        public async Task<bool> UpdateProizvodAsync(Proizvod proizvod)
        {
            try
            {
                CloudTable proizvodiTable = GetProizvodiTableReference();

                // Kreiraj upit za pretragu proizvoda prema ID-u
                TableQuery<ProizvodEntity> query = new TableQuery<ProizvodEntity>()
                    .Where(TableQuery.GenerateFilterConditionForInt("Id", QueryComparisons.Equal, proizvod.Id));

                // Izvrši upit i preuzmi rezultate
                var queryResult = await proizvodiTable.ExecuteQuerySegmentedAsync(query, null);

                // Proveri da li je pronađen proizvod
                if (queryResult.Results.Count > 0)
                {
                    // Ako je pronađen, ažuriraj njegove informacije
                    ProizvodEntity existingEntity = queryResult.Results.First();
                    existingEntity.KolicinaProizvoda = proizvod.KolicinaProizvoda;

                    // Kreiraj operaciju za ažuriranje entiteta
                    TableOperation updateOperation = TableOperation.Replace(existingEntity);

                    // Izvrši operaciju ažuriranja
                    await proizvodiTable.ExecuteAsync(updateOperation);

                    // Uspješno ažuriranje
                    return true;
                }
                else
                {
                    // Proizvod nije pronađen, nije moguće izvršiti ažuriranje
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Logovanje grešaka (opciono)
                Console.WriteLine($"Greška pri ažuriranju proizvoda: {ex.Message}");
                throw; // Propagiranje izuzetka dalje
            }
        }


    }
}
