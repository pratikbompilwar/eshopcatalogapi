using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CatalogAPI.Helpers;
using CatalogAPI.Infrastructure;
using CatalogAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace CatalogAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAll")]    
    public class CatalogController : ControllerBase
    {
        private CatalogContext db;
        IConfiguration config;
        public CatalogController(CatalogContext db, IConfiguration configuration)
        {
            this.db = db;
            this.config = configuration;
        }

        [AllowAnonymous]
        [HttpGet("",Name ="GetProducts")]
        public async Task<ActionResult<List<CatalogItem>>> GetProducts()
        {
           var result = await this.db.Catalog.FindAsync<CatalogItem>(FilterDefinition<CatalogItem>.Empty);
           return result.ToList();
        }

        [Authorize(Roles ="admin")]
        [HttpPost("",Name ="AddProduct")]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public ActionResult<CatalogItem> AddProduct(CatalogItem item)
        {
            TryValidateModel(item);
            if (ModelState.IsValid)
            {
                this.db.Catalog.InsertOne(item);
                return Created("", item);
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        [AllowAnonymous]
        [HttpGet("{id}", Name = "FindById")]
        public async Task<ActionResult<CatalogItem>> FindProductById(string id)
        {
            var builder = Builders<CatalogItem>.Filter;
            var filter = builder.Eq("Id",id);
            var item = await db.Catalog.FindAsync(filter);
         
            return item.FirstOrDefault();
            
        }

       // [Authorize(Roles = "admin")]
        [HttpPost("product")]
        public ActionResult<CatalogItem> AddProductWithImage()
        {
            //var imageName = SaveImageToLocal(Request.Form.Files[0]);
            var imageName = SaveImageToCloudAsync(Request.Form.Files[0]).GetAwaiter().GetResult();

            var catalogItem = new CatalogItem()
            {
                Name = Request.Form["name"],
                Price = Double.Parse(Request.Form["price"]),
                Quantity = Int32.Parse(Request.Form["quantity"]),
                ReorderLevel = Int32.Parse(Request.Form["reorderLevel"]),
                ManufacturingDate = DateTime.Parse(Request.Form["manufacturingDate"]),
                Vendors = new List<Vendor>(),
                ImageUrl = imageName
            };
            db.Catalog.InsertOne(catalogItem);  // saving to mongo
            BackupToTableAsync(catalogItem).GetAwaiter().GetResult();  //backup to azure
            return catalogItem;
        }

        [NonAction]
        private string SaveImageToLocal(IFormFile image)
        {
            var imageName = $"{Guid.NewGuid()}_{Request.Form.Files[0].FileName}";
            var Image = Request.Form.Files[0];


            var dirName = Path.Combine(Directory.GetCurrentDirectory(), "Images");
            if (Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            var filePath = Path.Combine(dirName, imageName);
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                Image.CopyTo(fs);
            }
            return $"/Images/{imageName}";
        }

        [NonAction]
        private async Task<string> SaveImageToCloudAsync(IFormFile image)
        {
            var imageName = $"{Guid.NewGuid()}_{Request.Form.Files[0].FileName}";
            var tempFile = Path.GetTempFileName();
            using (FileStream fs = new FileStream(tempFile, FileMode.Create))
            {
               await image.CopyToAsync(fs);
            }

            var imageFile = Path.Combine(Path.GetDirectoryName(tempFile), imageName);
            System.IO.File.Move(tempFile, imageFile);
            StorageAccountHelper storageHelper = new StorageAccountHelper();
            storageHelper.StorageConnectionString = config.GetConnectionString("StorageConnection");
            var fileUri = await storageHelper.UploadFileToBlobAsync(imageFile, "eshopimages");

            System.IO.File.Delete(imageFile);

            //fileUri = fileUri + "?sv=2019-02-02&ss=bfq&srt=sco&sp=rwdlacup&se=2019-11-15T15:22:16Z&st=2019-11-06T07:22:16Z&spr=https&sig=Bshl%2Bplh44Wu3w4e8XIGemZgLhamR%2FrFXdDvIqJdkbg%3D";
            return fileUri;
        }

        [NonAction] async Task<CatalogEntity> BackupToTableAsync(CatalogItem item)
        {
            StorageAccountHelper storageHelper = new StorageAccountHelper();
            storageHelper.TableConnectionString = config.GetConnectionString("TableConnection");
            return await storageHelper.SaveToTableAsync(item);
        }
    }
}