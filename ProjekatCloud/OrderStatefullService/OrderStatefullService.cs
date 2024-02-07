using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace OrderStatefullService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class OrderStatefullService : StatefulService, IOrderStatefullService
    {
        public OrderStatefullService(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<bool> DodajUKorpu(int productId)
        {
            try
            {
                // Dobijanje ili kreiranje Reliable Dictionary za korpu
                var korpaDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<int, Proizvod>>("korpaDictionary");

                // Provera da li proizvod već postoji u korpi
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var existingProduct = await korpaDictionary.TryGetValueAsync(tx, productId);
                    if (existingProduct.HasValue)
                    {
                        // Ako proizvod već postoji, samo povećaj njegovu količinu
                        await korpaDictionary.AddOrUpdateAsync(tx, productId, existingProduct.Value, (key, value) =>
                        {
                            value.KolicinaProizvoda++;
                            return value;
                        });
                    }
                    else
                    {
                        // Ako proizvod ne postoji, dodaj ga u korpu sa količinom 1
                        var newProduct = new Proizvod { Id = productId, KolicinaProizvoda = 1 };
                        await korpaDictionary.AddAsync(tx, productId, newProduct);
                    }

                    // Potvrdi transakciju
                    await tx.CommitAsync();

                    return true; // Uspješno dodan proizvod u korpu
                }
            }
            catch (Exception ex)
            {
                // Logovanje greške ako se desi
                Console.WriteLine($"Greška pri dodavanju proizvoda u korpu: {ex.Message}");
                return false; // Greška pri dodavanju proizvoda u korpu
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
            return new ServiceReplicaListener[0];
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
