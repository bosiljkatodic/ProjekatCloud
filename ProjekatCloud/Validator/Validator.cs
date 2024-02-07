using Common;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Fabric;

namespace Validator
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Validator : StatelessService, IValidator
    {
        public Validator(StatelessServiceContext context)
            : base(context)
        {

        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        public async Task<string> Validate(Korisnik korisnik)
        {
            if (korisnik == null)
            {
                return "Model is not okay";
            }

            if (string.IsNullOrWhiteSpace(korisnik.Ime) || string.IsNullOrWhiteSpace(korisnik.Prezime) ||
                string.IsNullOrWhiteSpace(korisnik.Email) || string.IsNullOrWhiteSpace(korisnik.Lozinka))
            {
                return "Model is not okay";
            }

            var fabricClient = new FabricClient();

            var serviceUri = new Uri("fabric:/ProjekatCloud/UserStatefullService");
            var partitionList = await fabricClient.QueryManager.GetPartitionListAsync(serviceUri);
            IUserStatefullService proxy = null;

            foreach (var partition in partitionList)
            {
                var partitionKey = partition.PartitionInformation as Int64RangePartitionInformation;

                if (partitionKey != null)
                {
                    var servicePartitionKey = new ServicePartitionKey(partitionKey.LowKey);

                    proxy = ServiceProxy.Create<IUserStatefullService>(serviceUri, servicePartitionKey);
                    break;
                }
            }
            
            try
            {
               /* bool userExists = await proxy.CheckIfUserExists(korisnik.Email);

                if (userExists)
                {
                    return "Korisnik već postoji u bazi podataka";
                }
               */
                await proxy.Register(korisnik);
                return "Uspjesna registracija";


            }
            catch (Exception)
            {
               
                return "Neuspjesna registracija";
            }
            
        }

        public async Task<string> ValidateUpdate(Korisnik korisnik)
        {
            if (korisnik == null)
            {
                return "Model is not okay";
            }

            if (string.IsNullOrWhiteSpace(korisnik.Ime) || string.IsNullOrWhiteSpace(korisnik.Prezime) ||
                string.IsNullOrWhiteSpace(korisnik.Email) || string.IsNullOrWhiteSpace(korisnik.Lozinka))
            {
                return "Model is not okay";
            }

            var fabricClient = new FabricClient();

            var serviceUri = new Uri("fabric:/ProjekatCloud/UserStatefullService");
            var partitionList = await fabricClient.QueryManager.GetPartitionListAsync(serviceUri);
            IUserStatefullService proxy = null;

            foreach (var partition in partitionList)
            {
                var partitionKey = partition.PartitionInformation as Int64RangePartitionInformation;

                if (partitionKey != null)
                {
                    var servicePartitionKey = new ServicePartitionKey(partitionKey.LowKey);

                    proxy = ServiceProxy.Create<IUserStatefullService>(serviceUri, servicePartitionKey);
                    break;
                }
            }

            try
            {
                /* bool userExists = await proxy.CheckIfUserExists(korisnik.Email);

                 if (userExists)
                 {
                     return "Korisnik već postoji u bazi podataka";
                 }
                */
                await proxy.UpdateKorisnik(korisnik);
                return "Uspjesna izmjena";


            }
            catch (Exception)
            {

                return "Neuspjesna izmjena";
            }

        }

        public async Task<string> ValidateLogin(LoginViewModel korisnik)
        {
            if (korisnik == null)
            {
                return "Model is not okay";
            }

            if (string.IsNullOrWhiteSpace(korisnik.Password) || string.IsNullOrWhiteSpace(korisnik.Email))
            {
                return "Model is not okay";
            }

            var fabricClient = new FabricClient();

            var serviceUri = new Uri("fabric:/ProjekatCloud/UserStatefullService");
            var partitionList = await fabricClient.QueryManager.GetPartitionListAsync(serviceUri);
            IUserStatefullService proxy = null;

            foreach (var partition in partitionList)
            {
                var partitionKey = partition.PartitionInformation as Int64RangePartitionInformation;

                if (partitionKey != null)
                {
                    var servicePartitionKey = new ServicePartitionKey(partitionKey.LowKey);

                    proxy = ServiceProxy.Create<IUserStatefullService>(serviceUri, servicePartitionKey);
                    break;
                }
            }

            try
            {
                /* bool userExists = await proxy.CheckIfUserExists(korisnik.Email);

                 if (userExists)
                 {
                     return "Korisnik već postoji u bazi podataka";
                 }
                */
                var provjera = await proxy.ValidateCredentials(korisnik.Email, korisnik.Password);
                if(provjera == true)
                {
                    return "Uspjesno logovanje";
                }
                else
                {
                    return "Neuspjesno logovanje";

                }

            }
            catch (Exception)
            {

                return "Neuspjesno logovanje";
            }

        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {


            long iterations = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
