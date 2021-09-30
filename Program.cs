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

            //This is to make sure we have the right connection string
            string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
            Console.WriteLine(connectionString);
            Console.WriteLine();
            Console.WriteLine("Press any key to continue..");
            Console.ReadKey();
            Console.Clear();

            //Create a BlobServiceClient object which will be used to create a container client
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            //Call for CreateSampleContainerAsync and store it inside a variable
            var containerClient = await CreateSampleContainerAsync(blobServiceClient);

            //Containing list of containers in Azure / showing us the list in console
            await ListContainers(blobServiceClient);

            if (containerClient == null)
            {
                Console.WriteLine("Type which container to work with:");
                var input = Console.ReadLine();
                Console.WriteLine();

                var item = await GetContainer(blobServiceClient, input);
                if (item == null)
                {
                    Console.WriteLine("We could not find a corresponding container.\n");
                    Environment.Exit(0);
                }
                BlobContainerClient newClient = blobServiceClient.GetBlobContainerClient(input);
                CreateTextFileInContainer(newClient);
                Console.WriteLine("Blobs uploaded:\t");
                await foreach (BlobItem blobItem in newClient.GetBlobsAsync())
                {
                    //To get the full path to the blob URL
                    var path = Path.Combine(newClient.Uri.AbsoluteUri, blobItem.Name);
                    Console.WriteLine("\t" + "URL: " + path);
                }
            }
            else
            {
                CreateTextFileInContainer(containerClient);
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
            // Name the sample container based on new GUID to ensure uniqueness.
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

                List<BlobContainerItem> list = new List<BlobContainerItem>();
                await foreach (Page<BlobContainerItem> containerPage in resultSegment)
                {
                    foreach (BlobContainerItem containerItem in containerPage.Values)
                    {
                        Console.WriteLine("Container name: {0}", containerItem.Name);
                        list.Add(containerItem);
                    }
                    Console.WriteLine();
                }

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

                //Console.WriteLine("Listing Containers..");

                List<BlobContainerItem> list = new List<BlobContainerItem>();
                List<BlobContainerItem> selectedItem = new List<BlobContainerItem>();

                await foreach (Page<BlobContainerItem> containerPage in resultSegment)
                {
                    foreach (BlobContainerItem containerItem in containerPage.Values)
                    {
                        //Console.WriteLine("Container name: {0}", containerItem.Name);
                        list.Add(containerItem);
                    }

                    //containerInput = Console.ReadLine();
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

        private static async void CreateTextFileInContainer(BlobContainerClient container)
        {
            try
            {
                string localPath = @"C:\Users\orhan\source\repos\BlobConsoleApp\images\";
                //string fileName = "quickstart" + Guid.NewGuid().ToString() + ".jpg";
                string fileName = "wolf.jpg";
                string localFilePath = Path.Combine(localPath, fileName);

                // Write text to the file
                using FileStream uploadFileStream = File.OpenRead(localFilePath);

                // Get a reference to a blob
                BlobClient blobClient = container.GetBlobClient(fileName);
                Console.WriteLine($"Blob container: \n\t{container.Name}\n");

                // Set the HttpHeader to be able to display image through URL in WebBrowser.
                var blobHttpHeader = new BlobHttpHeaders { ContentType = "image/jpeg" };
                // Upload data from the local file
                await blobClient.UploadAsync(localFilePath, new BlobUploadOptions { HttpHeaders = blobHttpHeader });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
