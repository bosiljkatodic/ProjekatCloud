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
                    Id = korisnik.Id,
                    Ime = korisnik.Ime,
                    Prezime = korisnik.Prezime,
                    Email = korisnik.Email
                    // Dodajte ostale podatke korisnika prema potrebi
                };

                TableOperation insertOperation = TableOperation.Insert(korisnikEntity);

                await korisniciTable.ExecuteAsync(insertOperation);

                // Logovanje uspešnosti (opciono)
                Console.WriteLine($"Dodat korisnik sa ID: {korisnik.Id} u tabelu Korisnici");
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
    }
}
