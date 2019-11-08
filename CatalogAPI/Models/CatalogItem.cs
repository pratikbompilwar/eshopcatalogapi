using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CatalogAPI.Models
{
    public class CatalogItem
    {
        public CatalogItem()
        {
            Vendors = new List<Vendor>();
        }

        [BsonId(IdGenerator =typeof(StringObjectIdGenerator))]
        public string Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("price")]
        public double Price { get; set; }

        [BsonElement("quantity")]
        public int Quantity { get; set; }

        [BsonElement("reorderedLevel")]
        public int ReorderLevel { get; set; }

        [BsonElement("imageUrl")]
        public string ImageUrl { get; set; }

        [BsonElement("manufacturingDate")]
        public DateTime ManufacturingDate { get; set; }

        [BsonElement("Vendors")]
        public List<Vendor> Vendors { get; set; }
    }

    public class Vendor
    {
        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("contactNO")]
        public string ContactNo { get; set; }

        [BsonElement("address")]
        public string Address { get; set; }       
    }
}
