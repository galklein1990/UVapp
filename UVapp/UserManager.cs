using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using System.Threading.Tasks;

// ADD THIS PART TO YOUR CODE
using System.Net;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;


namespace UVapp
{
    class UserManager
    {
        // ADD THIS PART TO YOUR CODE
        private const string EndpointUrl = "https://uvsafe2.documents.azure.com:443/";
        private const string PrimaryKey = "Oc2HgAOWqt71ykwIVN4lOtsjYCVDQXxBuXEzXWqUxRBy42v9NNKD1cziNPyu5YBBzHla8JE1UDvn7PcZQlcEjg==";
        static private DocumentClient client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey); 
        static private string databaseName = "UsersDB";
        static private string collectionName = "UsersCollection";
        enum LoginStatus {Logged , NotLogged };
        public class User
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set;}
            public string UserName { get; set; }
            public string Password { get; set; }
            public int TimeExposed { get; set; }
            public override string ToString()
            {
                return JsonConvert.SerializeObject(this);
            }
        }

        // ADD THIS PART TO YOUR CODE
        public async Task GetStartedDemo()
        {
            // 
          //  this.client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);
            //await this.client.CreateDatabaseIfNotExistsAsync(new Database { Id = this.databaseName });

          //  await this.client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(this.databaseName), new DocumentCollection { Id = this.collectionName });
            Console.WriteLine("gotHere 2");
         /*   User dani = new User
            {
                UserName = "gal",
                Password = "1234",
                TimeExposed = 0
            };
      //      await this.CreateUserDocumentIfNotExists(this.databaseName, this.collectionName, dani);
            */
            
            // this.createUser("gal", "1234");

            this.GetUserLoginStatus( "gal", "1234");
        }



        public async static void UpdateUserExposedField(string userName, string password ,int exposed)
        {
            User gal = new User
            {
                Id = (userName + "-" + password),
                UserName = userName,
                Password = password,
                TimeExposed = exposed
            };
            await UserManager.client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, gal.Id), gal);

        }

        public async static void createUser(string userName, string password)
        {
            User gal = new User
            {
                Id = (userName + "-" + password),
                UserName = userName,
                Password = password,
                TimeExposed = 0
            };
            await UserManager.client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(UserManager.databaseName, UserManager.collectionName), gal);

        }


        // ADD THIS PART TO YOUR CODE
        private void WriteToConsoleAndPromptToContinue(string format, params object[] args)
        {
            Console.WriteLine(format, args);
            Console.WriteLine("Press any key to continue ...");
            Console.ReadKey();
        }




        /*

        private async Task CreateUserDocumentIfNotExists(string databaseName, string collectionName, User user)
        {
            try
            {
                await this.client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), user);
                //    await this.client.ReadDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collectionName, user.UserName));
                this.WriteToConsoleAndPromptToContinue("Found {0}", user.UserName);
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await this.client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), user);
                    this.WriteToConsoleAndPromptToContinue("Created user {0}", user.UserName);
                }
                else
                {
                    throw;
                }
            }
        }

    */

        

        public int GetUserLoginStatus(string userName, string password)
        {
            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Here we find the Andersen family via its LastName
            IQueryable<User> userQuery = UserManager.client.CreateDocumentQuery<User>(
                    UriFactory.CreateDocumentCollectionUri(UserManager.databaseName, UserManager.collectionName), queryOptions)
                    .Where(f => (f.UserName == userName && f.Password == password));

            Console.WriteLine("Running direct SQL query...");
            foreach (User user in userQuery)
            {
                Console.WriteLine("\tRead and found user {0}", user);
                return (int)LoginStatus.Logged;
            }

            Console.WriteLine("we did nor found user!");

            return (int)LoginStatus.NotLogged;
        }


    }
}