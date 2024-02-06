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
        public ProductStatefullService(StatefulServiceContext context)
            : base(context)
        { }

        public async Task InitializeAsync()
        {
            List<Proizvod> proizvodi = new()
            {
                new Proizvod() { Id = 1, CijenaProizvoda=200, KategorijaProizvoda="Knjige", KolicinaProizvoda=10, NazivProizvoda="Zdravi recepti", OpisProizvoda="100 zdravih recepata" },
                new Proizvod() { Id = 2, CijenaProizvoda=100, KategorijaProizvoda="Uredjaji", KolicinaProizvoda=2, NazivProizvoda="Fen", OpisProizvoda="Fen za kosu" },
                new Proizvod() { Id = 3, CijenaProizvoda=100, KategorijaProizvoda="Kozmetika", KolicinaProizvoda=5, NazivProizvoda="Umivalica", OpisProizvoda="pjena za umivanje" },
                new Proizvod() { Id = 4, CijenaProizvoda=100, KategorijaProizvoda="Kozmetika", KolicinaProizvoda=3, NazivProizvoda="Krema", OpisProizvoda="krema za hidarataciju" },
                new Proizvod() { Id = 5, CijenaProizvoda=100, KategorijaProizvoda="Cvijece", KolicinaProizvoda=10, NazivProizvoda="Lale", OpisProizvoda="vise boja" }
            };

            var stateManager = this.StateManager;
            var productDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<int, Proizvod>>("productDictionary");

            using var transaction = stateManager.CreateTransaction();
            foreach (Proizvod proizvod in proizvodi)
                await productDictionary.AddOrUpdateAsync(transaction, proizvod.Id, proizvod, (k, v) => proizvod);

            await transaction.CommitAsync();
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
