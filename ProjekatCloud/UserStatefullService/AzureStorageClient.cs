using Common;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserStatefullService
{
    public class AzureStorageClient
    {
        private readonly CloudStorageAccount storageAccount;

        public AzureStorageClient(string connectionString)
        {
            this.storageAccount = CloudStorageAccount.Parse(connectionString);
        }

        public async Task DodajKorisnikaAsync(Korisnik korisnik)
        {
            try
            {
                CloudTable korisniciTable = GetKorisniciTableReference();

                KorisnikEntity korisnikEntity = new KorisnikEntity
                {
                    //Id = korisnik.Id,
                    Ime = korisnik.Ime,
                    Prezime = korisnik.Prezime,
                    Email = korisnik.Email,
                    Lozinka = korisnik.Lozinka
                };

                TableOperation insertOperation = TableOperation.Insert(korisnikEntity);

                await korisniciTable.ExecuteAsync(insertOperation);

                // Logovanje uspešnosti (opciono)
              //  Console.WriteLine($"Dodat korisnik sa ID: {korisnik.Id} u tabelu Korisnici");
            }
            catch (Exception ex)
            {
                // Logovanje grešaka (opciono)
                Console.WriteLine($"Greška pri dodavanju korisnika: {ex.Message}");
                throw; // Propagiranje izuzetka dalje
            }
        }

        private CloudTable GetKorisniciTableReference()
        {
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable korisniciTable = tableClient.GetTableReference("Korisnici");

            // Kreiraj tabelu ako ne postoji
            korisniciTable.CreateIfNotExists();

            return korisniciTable;
        }

        public async Task<bool> UpdateKorisnikAsync(Korisnik korisnik)
        {
            try
            {
                CloudTable korisniciTable = GetKorisniciTableReference();

                // Kreiraj upit za pretragu korisnika prema email adresi
                TableQuery<KorisnikEntity> query = new TableQuery<KorisnikEntity>()
                    .Where(TableQuery.GenerateFilterCondition("Email", QueryComparisons.Equal, korisnik.Email));

                // Izvrši upit i preuzmi rezultate
                var queryResult = await korisniciTable.ExecuteQuerySegmentedAsync(query, null);

                // Proveri da li je pronađen korisnik
                if (queryResult.Results.Count > 0)
                {
                    // Ako je pronađen, ažuriraj njegove informacije
                    KorisnikEntity existingEntity = queryResult.Results.First();
                    existingEntity.Ime = korisnik.Ime;
                    existingEntity.Prezime = korisnik.Prezime;
                    existingEntity.Lozinka = korisnik.Lozinka;

                    // Kreiraj operaciju za ažuriranje entiteta
                    TableOperation updateOperation = TableOperation.Replace(existingEntity);

                    // Izvrši operaciju ažuriranja
                    await korisniciTable.ExecuteAsync(updateOperation);

                    // Uspješno ažuriranje
                    return true;
                }
                else
                {
                    // Korisnik nije pronađen, nije moguće izvršiti ažuriranje
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Logovanje grešaka (opciono)
                Console.WriteLine($"Greška pri ažuriranju korisnika: {ex.Message}");
                throw; // Propagiranje izuzetka dalje
            }
        }

    }
}
