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
        private DocumentClient client;
        private string databaseName = "UsersDB";
        private string collectionName = "UsersCollection";

        public class User
        {
            [JsonProperty(PropertyName = "id")]
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
            //await this.client.CreateDatabaseIfNotExistsAsync(new Database { Id = this.databaseName });

            //await this.client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(this.databaseName), new DocumentCollection { Id = this.collectionName });
            Console.WriteLine("gotHere 2");
            User gal = new User
            {
                UserName = "gal",
                Password = "1234",
                TimeExposed = 0
            };
            await this.CreateUserDocumentIfNotExists(this.databaseName, this.collectionName, gal);

            await this.createUser("gal", "1234");
            this.DoesUserExist(this.databaseName, this.collectionName, "gal", "1234");
        }



        public async Task createUser(string userName, string password)
        {
            User gal = new User
            {
                UserName = userName,
                Password = password,
                TimeExposed = 0
            };
            await this.CreateUserDocumentIfNotExists(this.databaseName, this.collectionName, gal);


        }


        // ADD THIS PART TO YOUR CODE
        private void WriteToConsoleAndPromptToContinue(string format, params object[] args)
        {
            Console.WriteLine(format, args);
            Console.WriteLine("Press any key to continue ...");
            Console.ReadKey();
        }






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





        public bool DoesUserExist(string databaseName, string collectionName, string userName, string password)
        {
            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            // Here we find the Andersen family via its LastName
            IQueryable<User> userQuery = this.client.CreateDocumentQuery<User>(
                    UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                    .Where(f => (f.UserName == userName && f.Password == password));

            Console.WriteLine("Running direct SQL query...");
            foreach (User user in userQuery)
            {
                Console.WriteLine("\tRead and found user {0}", user);
                return true;
            }

            Console.WriteLine("we did nor found user!");

            return false;
        }


    }
}