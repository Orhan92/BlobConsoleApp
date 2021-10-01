using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BlobAppPages.Pages
{
    public class ImagesModel : PageModel
    {
        public List<Uri> blobs = new List<Uri>();
        public async Task<IActionResult> OnGet()
        {
            string connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

            var storageAccount = CloudStorageAccount.Parse(connectionString);

            var _blobClient = storageAccount.CreateCloudBlobClient();

            var _blobContainer = _blobClient.GetContainerReference("orhan");


            BlobContinuationToken blobContinuationToken = null;

            do
            {
                var response = await _blobContainer.ListBlobsSegmentedAsync(blobContinuationToken);
                foreach (IListBlobItem blob in response.Results)
                {
                    if (blob.GetType() == typeof(CloudBlockBlob))
                        blobs.Add(blob.Uri);
                }
                blobContinuationToken = response.ContinuationToken;
            } while (blobContinuationToken != null);

            return Page();
        }
    }
}
