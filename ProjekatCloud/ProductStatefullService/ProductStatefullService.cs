using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace ProductStatefullService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class ProductStatefullService : StatefulService, IProductStatefullService
    {
        private AzureStorageClient storageClient;

        public ProductStatefullService(StatefulServiceContext context)
            : base(context)
        {
            // Inicijalizuj Azure Storage Client
            string azureStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=onlinestoreftn;AccountKey=zKWefU5Jq1NrKQoWaaiBKZEYukHCOwfhFz0M6o5TR3s4X/LXvNO5mxzEJuv1OY+oklshIhHw1Ikl+AStwNnFPg==;EndpointSuffix=core.windows.net"; // Postavite stvarni connection string
            this.storageClient = new AzureStorageClient(azureStorageConnectionString);
        }

        public async Task InitializeAsync()
        {
            //citanje iz baze
            try
            {
                // Čitanje svih korisnika iz Azure Storage baze
                var FromStorage = await this.storageClient.GetAllProductsAsync();

                // Provera da li postoje korisnici u bazi
                if (FromStorage != null && FromStorage.Any())
                {
                    var stateManager = this.StateManager;

                    // Dobijanje ili dodavanje Reliable Dictionary
                    var productDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<int, Proizvod>>("productDictionary");

                    // Pisanje svih korisnika iz Azure Storage baze u Reliable Dictionary
                    using (var tx = this.StateManager.CreateTransaction())
                    {
                        foreach (var proizvod in FromStorage)
                        {
                            await productDictionary.AddOrUpdateAsync(tx, proizvod.Id, proizvod, (key, value) => proizvod);
                        }

                        // Commit transakcije
                        await tx.CommitAsync();
                    }

                    ServiceEventSource.Current.ServiceMessage(this.Context, $"Successfully loaded products from Azure Storage.");
                }
                else
                {
                    ServiceEventSource.Current.ServiceMessage(this.Context, $"No products found in Azure Storage.");
                }
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, $"Error during loading products from Azure Storage: {ex.Message}");
            }
        }

        public async Task<IEnumerable<Proizvod>> GetAllProducts()
        {
            await InitializeAsync();

            var stateManager = this.StateManager;
            var productDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<int, Proizvod>>("productDictionary");

            var products = new List<Proizvod>();

            using (var tx = stateManager.CreateTransaction())
            {
                var enumerable = await productDictionary.CreateEnumerableAsync(tx);

                var enumerator = enumerable.GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    KeyValuePair<int, Proizvod> current = enumerator.Current;
                    products.Add(current.Value);
                }
            }

            return products;
        }

        public async Task<IEnumerable<Proizvod>> GetCijeluKorpu()
        {
            try
            {
                // Dobijanje Reliable Dictionary-ja za korpu ili kreiranje ako ne postoji
                var korpaDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, Proizvod>>("korpaDictionary");

                List<Proizvod> korpaList = new List<Proizvod>();

                // Otvori novu transakciju
                using (var tx = this.StateManager.CreateTransaction())
                {
                    // Dobij sve stavke iz korpe
                    var korpaEnumerable = await korpaDictionary.CreateEnumerableAsync(tx);

                    // Iteriraj kroz sve stavke korpe
                    using (var enumerator = korpaEnumerable.GetAsyncEnumerator())
                    {
                        while (await enumerator.MoveNextAsync(default))
                        {
                            // Dodaj stavku u listu
                            korpaList.Add(enumerator.Current.Value);
                        }
                    }

                    // Potvrdi transakciju
                    await tx.CommitAsync();
                }

                // Vrati cijelu korpu
                return korpaList;
            }
            catch (Exception ex)
            {
                // Logovanje greške ako se desi
                Console.WriteLine($"Greška pri dobijanju cijele korpe: {ex.Message}");
                return null; // Vrati null ako se desi greška
            }
        }


        public async Task<bool> DodajUKorpu(int productId)
        {
            try
            {
                // Dobijanje ili kreiranje Reliable Dictionary za korpu
                var korpaDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, Proizvod>>("korpaDictionary");

                // Dobij proizvod na osnovu ID-a
                var productToAdd = await GetProductById(productId);

                // Provera da li je proizvod pronađen
                if (productToAdd != null)
                {

                    // Provera da li proizvod već postoji u korpi
                    using (var tx = this.StateManager.CreateTransaction())
                    {
                        var existingProduct = await korpaDictionary.TryGetValueAsync(tx, productId);
                        if (existingProduct.HasValue)
                        {
                            // Ako proizvod već postoji, povećaj njegovu trenutnu količinu za 1
                            var updatedProduct = existingProduct.Value;
                            updatedProduct.KolicinaProizvoda++;

                            // Ažuriraj proizvod u korpi
                            await korpaDictionary.SetAsync(tx, productId, updatedProduct);

                        }
                        else
                        {

                            // Ako proizvod ne postoji, dodaj ga u korpu sa količinom 1
                            productToAdd.KolicinaProizvoda = 1; // Postavljamo početnu količinu na 1
                            await korpaDictionary.AddAsync(tx, productId, productToAdd);

                        }

                        // Potvrdi transakciju
                        await tx.CommitAsync();

                        return true; // Uspješno dodan proizvod u korpu
                    }
                }
                else
                {
                    // Proizvod sa datim ID-om nije pronađen, vratimo false
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Logovanje greške ako se desi
                Console.WriteLine($"Greška pri dodavanju proizvoda u korpu: {ex.Message}");
                return false; // Greška pri dodavanju proizvoda u korpu
            }
        }

        public async Task<bool> ReduceProductQuantity(int productId)
        {
            try
            {
                var stateManager = this.StateManager;
                var productDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<int, Proizvod>>("productDictionary");

                using (var tx = stateManager.CreateTransaction())
                {
                    // Pokušaj dobijanja proizvoda iz rečnika
                    var result = await productDictionary.TryGetValueAsync(tx, productId);

                    // Provera da li je proizvod pronađen
                    if (result.HasValue)
                    {
                        // Proizvod je pronađen, smanji količinu ako je veća od 0
                        var product = result.Value;
                        if (product.KolicinaProizvoda > 0)
                        {
                            product.KolicinaProizvoda--;

                            // Ažuriraj proizvod u rečniku
                            await productDictionary.SetAsync(tx, productId, product);

                            // Commit transakcije
                            await tx.CommitAsync();

                            await this.storageClient.UpdateProizvodAsync(product);

                            // Uspješno smanjena količina
                            return true;
                        }
                        else
                        {
                            // Količina proizvoda je već 0, ne može se više smanjiti
                            return false;
                        }
                    }
                    else
                    {
                        // Proizvod sa datim ID-om nije pronađen u rečniku
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                // Logovanje grešaka (opciono)
                ServiceEventSource.Current.ServiceMessage(this.Context, $"Error reducing product quantity: {ex.Message}");
                throw; // Propagiranje izuzetka dalje
            }
        }

        public async Task<Proizvod> GetProductById(int productId)
        {
            try
            {
                // Dobijanje ili kreiranje Reliable Dictionary za proizvode
                var productDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, Proizvod>>("productDictionary");

                // Otvori novu transakciju
                using (var tx = this.StateManager.CreateTransaction())
                {
                    // Pokušaj dobijanja proizvoda iz rečnika
                    var result = await productDictionary.TryGetValueAsync(tx, productId);

                    // Provera da li je proizvod pronađen
                    if (result.HasValue)
                    {
                        // Vrati pronađeni proizvod
                        return result.Value;
                    }
                    else
                    {
                        // Proizvod sa datim ID-om nije pronađen u rečniku
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                // Logovanje grešaka ako se desi
                ServiceEventSource.Current.ServiceMessage(this.Context, $"Error getting product by ID: {ex.Message}");
                return null; // Vrati null ako se desi greška
            }
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
            await InitializeAsync();

            await base.RunAsync(cancellationToken);

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
