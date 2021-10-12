using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using blobstorage.Models;

namespace blobstorage.Controllers
{
    public class TablesController : Controller
    {
        // GET
        // public IActionResult Index()
        // {
        //     return View();
        // }

        // Create function to link with storage account
        private CloudTable GetStorageAccountInfo()
        {
            // Step 1: Read json
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            IConfigurationRoot configure = builder.Build();

            // Step 2: Add access key to account object by referring the appsettings.json
            CloudStorageAccount objectAccount =
                CloudStorageAccount.Parse(configure["ConnectionStrings:BlobStorageConnection"]);

            // Step 3: Find table that you prefer to refer
            CloudTableClient tableClient = objectAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("TestTable");

            return table;
        }

        public ActionResult CreateTable()
        {
            // Link the table with correct access key
            CloudTable table = GetStorageAccountInfo();

            // Check whether table is exist or not, if not yet exist, create a new one.
            ViewBag.success = table.CreateIfNotExistsAsync().Result;

            // Get table name so that it can show in the page telling which table is successfully build or not.
            ViewBag.tableName = table.Name;

            // Return to the page and display result whether table created successfully or not.
            return View();
        }

        // Insert single text data into the table storage (NoSQL)
        public ActionResult InsertSingleData()
        {
            CloudTable table = GetStorageAccountInfo();

            // Create the customer information
            CustomerEntity customer1 = new CustomerEntity("Wong", "Jiun Cheng");
            customer1.Address = "Kuala Lumpur";
            customer1.Email = "jc123@gmail.com";
            customer1.BirthDate = new DateTime(1999, 3, 28);

            try
            {
                TableOperation insert = TableOperation.Insert(customer1);
                TableResult result = table.ExecuteAsync(insert).Result;
                ViewBag.TableName = table.Name;
                ViewBag.Result = result.HttpStatusCode; // 204 = insert success
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return View();
        }

        public ActionResult AddEntities()
        {
            // Collect the entities info in a list and send to table storage
            CloudTable table = GetStorageAccountInfo();

            string[,] customerInfo =
            {
                {"Smith", "Jerry", "CyberJaya, Selangor", "Jerry@contoso.com", "1991-02-22"},
                {"Smith", "Berry", "Bukit Jalil, Kuala Lumpur", "Berry@contoso.com", "1991-02-22"},
                {"Smith", "Lonely", "Cheras, Kuala Lumpur", "Lonely@contoso.com", "1991-02-22"}
            };

            TableBatchOperation batchList = new TableBatchOperation();
            IList<TableResult> results;

            for (int i = 0; i < 3; i++)
            {
                // Create customer information
                CustomerEntity customer = new CustomerEntity(customerInfo[i, 0], customerInfo[i, 1]);
                customer.Address = customerInfo[i, 2];
                customer.Email = customerInfo[i, 3];
                customer.BirthDate = DateTime.Parse(customerInfo[i, 4]);
                batchList.Insert(customer);
            }

            try
            {
                results = table.ExecuteBatchAsync(batchList).Result;
                ViewBag.msg = "All data has been inserted to the table storage";
                ViewBag.success = true;
                return View(results);
            }
            catch (Exception ex)
            {
                ViewBag.msg = "Error: " + ex.ToString(); // Technical issue
            }

            return View();
        }

        public ActionResult SearchPage(string dialogMsg = null)
        {
            ViewBag.msg = dialogMsg;
            return View();
        }

        // Get single entity method
        [HttpPost]
        public ActionResult GetSingleEntity(string PartitionName, string RowName)
        {
            CloudTable table = GetStorageAccountInfo();
            string errorMessage = "";

            try
            {
                TableOperation retrieveAction = TableOperation.Retrieve<CustomerEntity>(PartitionName, RowName);
                TableResult results = table.ExecuteAsync(retrieveAction).Result;

                // If you success, the data will come along with its own E-Tag number
                if (results.Etag != null)
                {
                    return View(results);
                }
                else // If data not found, then send error message back to SearchPage
                {
                    errorMessage = "Data not found in the table!";
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Technical Issue: " + ex.ToString(); // Technical Error
            }

            return RedirectToAction("SearchPage", "Tables", new {dialogMsg = errorMessage});
        }

        // Delete Method
        public ActionResult DeletePage(string partitionkey, string rowkey)
        {
            string message = null;
            CloudTable table = GetStorageAccountInfo();

            try
            {
                TableOperation deleteAction =
                    TableOperation.Delete(new CustomerEntity(partitionkey, rowkey) {ETag = "*"});
                table.ExecuteAsync(deleteAction);

                message = "The data for the customer of " + partitionkey + " " + rowkey +
                          " is deleted from the table now!";
            }
            catch (Exception ex)
            {
                message = "Unable to delete data from the table! Error: " + ex.ToString();
            }

            return RedirectToAction("SearchPage", "Tables", new {dialogMsg = message});
        }

        [HttpPost]
        public ActionResult GetGroupEntity(string PartitionName)
        {
            string errormessage = null;
            CloudTable table = GetStorageAccountInfo();
            try
            {
                TableQuery<CustomerEntity> query = new TableQuery<CustomerEntity>().Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PartitionName));
                List<CustomerEntity> customers = new List<CustomerEntity>();
                TableContinuationToken token = null; // To identify still have next data or not.

                do
                {
                    TableQuerySegment<CustomerEntity> result = table.ExecuteQuerySegmentedAsync(query, token).Result;
                    token = result.ContinuationToken;

                    foreach (CustomerEntity customer in result.Results)
                    {
                        customers.Add(customer);
                    }
                } while (token != null);

                if (customers.Count != 0)
                {
                    return View(customers); // Back to display
                }

                errormessage = "Data not found.";
                return RedirectToAction("SearchPage", "Tables", new {dialogMsg = errormessage});
            }
            catch (Exception ex)
            {
                errormessage = "Technical Issue: " + ex.ToString(); // Technical Error
            }

            return RedirectToAction("SearchPage", "Tables", new {dialogMsg = errormessage});
        }
    }
}