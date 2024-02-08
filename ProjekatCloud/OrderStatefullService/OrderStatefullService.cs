using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace OrderStatefullService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    public sealed class OrderStatefullService : StatefulService, IOrderStatefullService
    {
        private AzureStorageClient storageClient;

        public OrderStatefullService(StatefulServiceContext context)
            : base(context)
        {
            // Inicijalizuj Azure Storage Client
            string azureStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=onlinestoreftn;AccountKey=zKWefU5Jq1NrKQoWaaiBKZEYukHCOwfhFz0M6o5TR3s4X/LXvNO5mxzEJuv1OY+oklshIhHw1Ikl+AStwNnFPg==;EndpointSuffix=core.windows.net"; // Postavite stvarni connection string
            this.storageClient = new AzureStorageClient(azureStorageConnectionString);
        }

        public async Task<bool> KreirajPorudzbinu(string emailKorisnika, IEnumerable<Proizvod> proizvodi, string nacinPlacanja, double ukupnaCijena)
        {
            // Pristupite kolekciji Porudzbina u Azure Storage-u
            var porudzbineDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, Porudzbina>>("PorudzbineDictionary");

            using (var tx = this.StateManager.CreateTransaction())
            {
                // Generišite novi ID za porudžbinu
                long id = DateTime.Now.Ticks;
                /*
                // Kreirajte novu porudžbinu
                var novaPorudzbina = new Porudzbina
                {
                    Id = (int)id,
                    Narucilac = new Korisnik { Email = emailKorisnika }, // Kreirajte korisnika na osnovu emaila
                    Proizvodi = proizvodi.ToList(),
                    NacinPlacanja = (NacinPlacanja)Enum.Parse(typeof(NacinPlacanja), nacinPlacanja),
                    UkupnaCijena = ukupnaCijena
                };
                */
                // Dodajte novu porudžbinu u kolekciju
                //await porudzbineDictionary.AddAsync(tx, novaPorudzbina.Id, novaPorudzbina);

                // Commit transakcije
                //await tx.CommitAsync();

                foreach (var proizvod in proizvodi)
                {
                    await this.storageClient.AddPorudzbina((int)id, emailKorisnika, proizvod.OpisProizvoda, proizvod.CijenaProizvoda, proizvod.Id, proizvod.NazivProizvoda, proizvod.KolicinaProizvoda, nacinPlacanja, ukupnaCijena);
                }
                return true;

            }
        }

        private async Task<long> GetNextIdAsync(ITransaction tx, string key)
        {
            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");
            var result = await myDictionary.AddOrUpdateAsync(tx, key, 0, (k, v) => ++v);
            return result;
        }

        public async Task<IEnumerable<PorudzbinaEntity>> GetPorudzbineZaKorisnikaAsync(string emailKorisnika)
        {
            // Pristupite Azure Storage tabeli za porudžbine
            var porudzbine = await storageClient.GetPorudzbineZaKorisnika(emailKorisnika);

            return porudzbine;
        }


        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            var myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}",
                        result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
