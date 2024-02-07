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
