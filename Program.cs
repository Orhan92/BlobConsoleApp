using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace BlobConsole
{
    class Program
    { 
        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcome to the Blob application!");

            //Print out connectionString to make sure we have the right connection string
            string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
            Console.WriteLine(connectionString);
            Console.WriteLine();
            Console.WriteLine("Press any key to continue..");
            Console.ReadKey();
            Console.Clear();

            //Create a BlobServiceClient object which will be used to create a container client
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            //Call for CreateSampleContainerAsync (this method creates a container) and store it inside a variable
            var containerClient = await CreateSampleContainerAsync(blobServiceClient);

            //Containing list of containers in Azure / showing us the list in console
            await ListContainers(blobServiceClient);

            //containerClient will be null if the container name already exists. Then we will enter this IF block
            if (containerClient == null)
            {
                Console.WriteLine("Type which container to work with:");
                var input = Console.ReadLine();
                Console.WriteLine();

                //the GetContainer method will retrieve a container based on our input
                var item = await GetContainer(blobServiceClient, input);
                if (item == null)
                {
                    Console.WriteLine("We could not find a corresponding container.\n");
                    Environment.Exit(0);
                }

                //If our input matches with a existing container, we will create a new instance of BlobContainerClient
                BlobContainerClient newClient = blobServiceClient.GetBlobContainerClient(input);

                //We call the method below to push our image to the blob container (newClient)
                CreateImageInContainer(newClient);

                //Listing all our files/images inside the container based on our input above
                Console.WriteLine("Blobs uploaded:\t");
                await foreach (BlobItem blobItem in newClient.GetBlobsAsync())
                {
                    //We print the URL to every image inside of our blob container
                    var path = Path.Combine(newClient.Uri.AbsoluteUri, blobItem.Name);
                    Console.WriteLine("\t" + "URL: " + path);
                }
            }

            //The else block will push image to the newly created container (see row 30 above)
            else
            {
                CreateImageInContainer(containerClient);
                Console.WriteLine("Blobs uploaded:\t");
                await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                {
                    //Console.WriteLine("\t" + blobItem.Name);
                    var path = Path.Combine(containerClient.Uri.AbsoluteUri, blobItem.Name);
                    Console.WriteLine("\t" + "URL: " + path);
                }
            }
        }

        private static async Task<BlobContainerClient> CreateSampleContainerAsync(BlobServiceClient blobServiceClient)
        {
            // The container name must be lowercase.
            Console.WriteLine("Type a container name you want to create (ONLY LOWCASE LETTERS) ");
            string containerName = Console.ReadLine();
            Console.WriteLine();

            try
            {
                // Create the container
                BlobContainerClient container = await blobServiceClient.CreateBlobContainerAsync(containerName);

                if (await container.ExistsAsync())
                {
                    Console.WriteLine("Created container: {0}", container.Name);
                    return container;
                }
            }

            //If we couldnt create a container/ or there is already a container with the same name, we will enter the catch block
            catch (RequestFailedException e)
            {
                Console.WriteLine("HTTP error code {0}: {1}",
                                    e.Status, e.ErrorCode);
                Console.WriteLine("Proceeding..");
            }
            return null;
        }

        private async static Task<IEnumerable<BlobContainerItem>> ListContainers(BlobServiceClient blobServiceClient)
        {
            try
            {
                // Call the listing operation and enumerate the result segment.
                var resultSegment =
                    blobServiceClient.GetBlobContainersAsync(BlobContainerTraits.Metadata, default)
                    .AsPages(default);

                Console.WriteLine("\nListing Containers..");

                //Create a new list to store our container objects 
                List<BlobContainerItem> list = new List<BlobContainerItem>();

                //For each container inside our storage, print out name of every container
                await foreach (Page<BlobContainerItem> containerPage in resultSegment)
                {
                    foreach (BlobContainerItem containerItem in containerPage.Values)
                    {
                        Console.WriteLine("Container name: {0}", containerItem.Name);
                        list.Add(containerItem);
                    }
                    Console.WriteLine();
                }

                //If no containers are found, exit application.
                if(list.Count() == 0)
                {
                    Console.WriteLine("There is no Blob containers\n");
                    Environment.Exit(0);
                }
                return list;
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                return null;
            }
        }

        private async static Task<BlobContainerItem> GetContainer(BlobServiceClient blobServiceClient, string containerInput)
        {
            try
            {
                // Call the listing operation and enumerate the result segment.
                var resultSegment =
                    blobServiceClient.GetBlobContainersAsync(BlobContainerTraits.Metadata, default)
                    .AsPages(default);

                //Store every container available inside "list"
                List<BlobContainerItem> list = new List<BlobContainerItem>();
                //Create a new list "selectedItem" to push the selectedItem from "list" above
                List<BlobContainerItem> selectedItem = new List<BlobContainerItem>();

                //Loops through every container in our storage
                await foreach (Page<BlobContainerItem> containerPage in resultSegment)
                {
                    foreach (BlobContainerItem containerItem in containerPage.Values)
                    {
                        //Adds every container in the cloud to the list
                        list.Add(containerItem);
                    }

                    //if use input matches with any of the containers in the cloud we will add it to the selectedItem list and return it.
                    foreach (var item in list)
                    {
                        if (containerInput.Contains(item.Name))
                        {
                            selectedItem.Add(item);
                            return item;
                        }
                    }
                    Console.WriteLine();
                }
                return null;
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                return null;
            }
        }

        private static async void CreateImageInContainer(BlobContainerClient container)
        {
            try
            {
                //Absolute path to our image folder, because relative didn't work.
                string localPath = @"C:\Users\orhan\source\repos\BlobConsoleApp\images\";
                string fileName = "fire-fist.jpg";

                //Combine localpath with filename in order to find the image inside of the folder
                string localFilePath = Path.Combine(localPath, fileName);

                // Write image to the file
                using FileStream uploadFileStream = File.OpenRead(localFilePath);

                // Get a reference to a blob
                BlobClient blobClient = container.GetBlobClient(fileName);
                Console.WriteLine($"Blob container: \n\t{container.Name}\n");

                // Set the HttpHeader to be able to display image through URL in WebBrowser.
                var blobHttpHeader = new BlobHttpHeaders { ContentType = "image/jpeg" };
                // Upload data from the local file path with header in order to open the image with the URL inside our web browser
                await blobClient.UploadAsync(localFilePath, new BlobUploadOptions { HttpHeaders = blobHttpHeader });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
