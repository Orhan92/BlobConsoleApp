﻿using Azure;
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
            string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
            Console.WriteLine(connectionString);
            Console.WriteLine();
            Console.ReadKey();

            //Create a BlobServiceClient object which will be used to create a container client
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            //Call for CreateSampleContainerAsync and store it inside a variable
            var containerClient = await CreateSampleContainerAsync(blobServiceClient);

            //Containing list of containers in Azure
            var containerList = await ListContainers(blobServiceClient);

            if (containerClient == null)
            {
                Console.WriteLine("Type which container to work with:");
                var input = Console.ReadLine();
                var item = await GetContainer(blobServiceClient, input);
                if (item == null)
                {
                    Console.WriteLine("We could not find a corresponding container.\n");
                    Environment.Exit(0);
                }
                BlobContainerClient newClient = blobServiceClient.GetBlobContainerClient(input);

                string localPath = @"C:\Users\orhan\source\repos\BlobConsoleApp\textFiles\";
                string fileName = "quickstart" + Guid.NewGuid().ToString() + ".txt";
                string localFilePath = Path.Combine(localPath, fileName);

                // Write text to the file
                await File.WriteAllTextAsync(localFilePath, "Hello, World!");
                // Get a reference to a blob
                BlobClient blobClient = newClient.GetBlobClient(fileName);
                Console.WriteLine($"Uploading Blob to Container: {item.Name}\n\t {blobClient.Uri}\n");
                // Upload data from the local file
                await blobClient.UploadAsync(localFilePath, true);

                Console.WriteLine("Listing blobs...");
                // List all blobs in the container
                await foreach (BlobItem blobItem in newClient.GetBlobsAsync())
                {
                    Console.WriteLine("\t" + blobItem.Name);
                }
            }

            else
            {
                // Create a local file in the ./textFiles/ directory for uploading and downloading
                string localPath = @"C:\Users\orhan\source\repos\BlobConsoleApp\textFiles\";
                string fileName = "quickstart" + Guid.NewGuid().ToString() + ".txt";
                string localFilePath = Path.Combine(localPath, fileName);

                // Write text to the file
                await File.WriteAllTextAsync(localFilePath, "Hello, World!");
                // Get a reference to a blob
                BlobClient blobClient = containerClient.GetBlobClient(fileName);
                Console.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);
                // Upload data from the local file
                await blobClient.UploadAsync(localFilePath, true);

                Console.WriteLine("Listing blobs...");
                // List all blobs in the container
                await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                {
                    Console.WriteLine("\t" + blobItem.Name);
                }
            }
        }


        /// <summary>
        /// Create Blob Container
        /// </summary>
        /// <param name="blobServiceClient"></param>
        /// <returns></returns>
        private static async Task<BlobContainerClient> CreateSampleContainerAsync(BlobServiceClient blobServiceClient)
        {
            // Name the sample container based on new GUID to ensure uniqueness.
            // The container name must be lowercase.
            Console.WriteLine("Type a container name you want to create: ");
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

        /// <summary>
        /// List all Containers
        /// </summary>
        /// <param name="blobServiceClient"></param>
        /// <returns></returns>
        async static Task<IEnumerable<BlobContainerItem>> ListContainers(BlobServiceClient blobServiceClient)
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
                return list;
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                return null;
            }
        }

        async static Task<BlobContainerItem> GetContainer(BlobServiceClient blobServiceClient, string containerInput)
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

        //private static async Task<BlobClient> CreateTextFileInContainer(BlobClient blobClient, BlobContainerClient container)
        //{
        //    string localPath = "./data/";
        //    string fileName = "quickstart" + Guid.NewGuid().ToString() + ".txt";
        //    string localFilePath = Path.Combine(localPath, fileName);

        //    Console.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);
        //    // Upload data from the local file
        //    await blobClient.UploadAsync(localFilePath, true);

        //    try
        //    {
        //        if (await container.ExistsAsync())
        //        {
        //            await File.WriteAllTextAsync(localFilePath, "Hello, World!");
        //            blobClient = container.GetBlobClient(fileName);
        //            Console.WriteLine("Uploading to Blob storage as blob:\n\t {0}\n", blobClient.Uri);
        //        }
        //    }
        //}
    }
}
