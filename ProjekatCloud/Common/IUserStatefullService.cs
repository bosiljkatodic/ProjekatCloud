using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceContract]

    public interface IUserStatefullService : IService
    {
        [OperationContract]
        Task<bool> Register(Korisnik korisnik);

        /*[OperationContract]
        Task<bool> Login(LoginViewModel loginViewModel);
        */
        /* [OperationContract]
         Task<bool> CheckIfUserExists(string userEmail);
        */
        [OperationContract]
        Task<bool> ValidateCredentials(string email, string password);
        [OperationContract]
        public Task<Korisnik> GetUserByEmail(string email);

        [OperationContract]
        public Task<bool> UpdateKorisnik(Korisnik korisnik);


    }
}
