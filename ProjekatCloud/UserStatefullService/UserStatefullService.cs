using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Common;

namespace UserStatefullService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    public sealed class UserStatefullService : StatefulService, IUserStatefullService
    {
        private AzureStorageClient storageClient;
        private IReliableDictionary<string, Korisnik> userDictionary;

        public UserStatefullService(StatefulServiceContext context)
            : base(context)
        {
            // Inicijalizuj Azure Storage Client
            string azureStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=onlinestoreftn;AccountKey=zKWefU5Jq1NrKQoWaaiBKZEYukHCOwfhFz0M6o5TR3s4X/LXvNO5mxzEJuv1OY+oklshIhHw1Ikl+AStwNnFPg==;EndpointSuffix=core.windows.net"; // Postavite stvarni connection string
            this.storageClient = new AzureStorageClient(azureStorageConnectionString);

        }

       /* public async Task<bool> Login(LoginViewModel loginViewModel)
        {
            throw new NotImplementedException();
        }
       */
        public async Task<bool> Register(Korisnik korisnik)
        {
            if (korisnik == null)
            {
                return false;
            }
            var stateManager = this.StateManager;
            var userDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, Korisnik>>("usersDictionary");
            using var transaction = this.StateManager.CreateTransaction();

            try
            {

                if (await userDictionary.ContainsKeyAsync(transaction, korisnik.Email))
                {
                    return false;
                }
                else
                {
                    await userDictionary.AddOrUpdateAsync(transaction, korisnik.Email, korisnik, (k, v) => korisnik);
                    await transaction.CommitAsync();
                    // Dodaj korisnika u Azure Storage tabelu
                    await this.storageClient.DodajKorisnikaAsync(korisnik);
                    return true;
                }
             }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, $"Error during user registration: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ValidateCredentials(string email, string password)
        {
            try
            {
                // Ovde ćete proveriti kredencijale u vašem skladištu podataka (na primer, u IReliableDictionary)
                // Ako korisničko ime i lozinka odgovaraju, vratite true, inače false
                var userDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, Korisnik>>("usersDictionary");

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var userExists = await userDictionary.ContainsKeyAsync(tx, email);

                    if (userExists)
                    {
                        // Ako korisnik postoji, proverite lozinku 
                        var user = await userDictionary.TryGetValueAsync(tx, email);

                        if (user.HasValue && user.Value.Lozinka == password)
                        {
                            return true; // Kredencijali su validni
                        }
                    }
                }

                return false; // Kredencijali nisu validni ili korisnik ne postoji
            }
            catch (Exception ex)
            {
                // Obrada grešaka (logovanje, slanje emaila, itd.)
                ServiceEventSource.Current.ServiceMessage(this.Context, $"Error during credential validation: {ex.Message}");
                return false;
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
