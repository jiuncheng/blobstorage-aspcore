using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace blobstorage.Models
{
    public class CustomerEntity : TableEntity 
    {
        // Create construction to add on the partition key and row key data
        public CustomerEntity(string familyName, string givenName)
        {
            this.PartitionKey = familyName;
            this.RowKey = givenName;
        }
        
        public CustomerEntity() { }
        
        public string Address { get; set; }
        public string Email { get; set; }
        public DateTime BirthDate { get; set; }
    }
}