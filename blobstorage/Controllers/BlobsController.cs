using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace blobstorage.Controllers
{
    public class BlobsController : Controller
    {
        // GET
        // public IActionResult Index()
        // {
        //     return View();
        // }

        private CloudBlobContainer GetStorageAndContainerInfo()
        {
            // Step 1: Read json
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            IConfigurationRoot configure = builder.Build();

            // Step 2: Add access key to account object by referring the appsettings.json
            CloudStorageAccount objectAccount =
                CloudStorageAccount.Parse(configure["ConnectionStrings:BlobStorageConnection"]);

            // Step 3: Find container that you prefer to refer
            CloudBlobClient clientObject = objectAccount.CreateCloudBlobClient();
            CloudBlobContainer container = clientObject.GetContainerReference("testblob");

            return container;
        }
        
        // Learn how to use the code to create a container in the blob storage
        public ActionResult CreateNewContainer()
        {
            // Link the container with the correct access key
            CloudBlobContainer container = GetStorageAndContainerInfo();
            
            // Check whether container is exist or not, if not exist, create new one.
            ViewBag.result = container.CreateIfNotExistsAsync().Result;
            
            // Get container name so that it can show in the page telling which container
            // is successfully build or not build
            ViewBag.name = container.Name;
            
            // Return to the page and display result whether container create success or not success!
            return View();
        }

        public string InsertBlobWithSimpleWay()
        {
            // Link the storage account and mentioned which container you want to edit
            CloudBlobContainer container = GetStorageAndContainerInfo();
            
            // Decide your new blob name first
            CloudBlockBlob blobItem = container.GetBlockBlobReference("test.jpg");
            
            // Send the file from the pc to the storage
            try
            {
                using var fileStream = System.IO.File.OpenRead(@"C:\\Users\\HOME\\Pictures\\apu.jpg");
                blobItem.UploadFromStreamAsync(fileStream).Wait();
                return "Congratulations, you have successfully uploaded to the blob storage!";
            }
            catch (Exception e)
            {
                return "Unable to upload to the blob storage! Error message : " + e.Message;
            }
        }

        public string UploadMultipleImages()
        {
            CloudBlobContainer container = GetStorageAndContainerInfo();
            string sourceFile = "";
            string message = "";
            
            // Assume I have 3 images to upload = image1.jpg, image2.jpg...
            for (int i = 1; i <= 3; i++)
            {
                try
                {
                    var fileStream = System.IO.File.OpenRead(@"C:\\Users\\HOME\\Pictures\\exam" + i + ".jpg");
                    string ext = Path.GetExtension(fileStream.Name); // Get the extension
                    CloudBlockBlob blob = container.GetBlockBlobReference("image " + i + ext);
                    blob.UploadFromStreamAsync(fileStream).Wait();
                    sourceFile = Path.GetFileName(fileStream.Name);
                    
                    // Display upload message
                    message = message + sourceFile + " is successfully uploaded to the blob storage!\n"; 
                }
                catch (Exception ex)
                {
                    message = "Technical issue: " + ex.ToString() + ". Please upload the file again";
                }
            }

            return message;
        }

        // Display the contents from the blob storage -> linked with interface
        public ActionResult ListBlobs(string displaymessage = null)
        {
            ViewBag.message = displaymessage;
            
            CloudBlobContainer container = GetStorageAndContainerInfo();
            
            // Create empty list
            List<string> blobItems = new List<string>();
            
            // Access blob storage to get the blob listing
            BlobResultSegment result = container.ListBlobsSegmentedAsync(null).Result;
            
            // Read one by one blob from result
            foreach (IListBlobItem item in result.Results)
            {
                // Blob type: block / append / page / directory
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob) item;
                    // Block blob = video / audio / images / text files
                    if (Path.GetExtension(blob.Name) == ".jpg")
                    {
                        blobItems.Add(blob.Name + "#" + blob.Uri); // Only text data, not yet image data
                    }
                }
            }
            
            return View(blobItems); // To create own view and bring the blob items to the frontend
        }

        // Remove the blob from blob storage using link button
        public ActionResult Delete(string imagename)
        {
            CloudBlobContainer container = GetStorageAndContainerInfo();
            string blobName = "";
            string message = "";

            try
            {
                // Delete item - firstly you need to know which item
                CloudBlockBlob blob = container.GetBlockBlobReference(imagename);
                blob.DeleteIfExistsAsync();
                message = imagename + " is successfully deleted from the blob storage!";

            }
            catch (Exception ex)
            {
                message = "Technical issue: " + ex.ToString() + ". Please try to delete the file again.";
            }
            
            return RedirectToAction("ListBlobs", "Blobs", new { displaymessage = message });
        }

        public ActionResult Download(string imagename, string imageurl)
        {
            CloudBlobContainer container = GetStorageAndContainerInfo();
            string message = "";
            
            // To download the file
            try
            {
                // Find the file
                CloudBlockBlob item = container.GetBlockBlobReference(imagename);
                
                // Set the destination file path
                var outputItem = System.IO.File.OpenWrite(@"C:\\Users\\HOME\\Pictures\\download\\" + imagename);
                
                // Copy the content to the file path that given by you
                item.DownloadToStreamAsync(outputItem).Wait();
                message = imagename + " is successfully downloaded from " + imageurl +
                          " to your desktop! Please check it!";
                outputItem.Close();
            }
            catch (Exception ex)
            {
                message = "Unable to download the file of " + imagename +
                          "\\n Technical issue: " + ex.ToString() + ". Please try to download the file again!";
            }
            
            return RedirectToAction("ListBlobs", "Blobs", new { displaymessage = message });
        }
    }
}