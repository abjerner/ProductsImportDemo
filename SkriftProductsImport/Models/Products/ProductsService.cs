using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Hangfire.Console;
using Hangfire.Console.Progress;
using Hangfire.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkriftProductsImport.Models.Products.Import;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace SkriftProductsImport.Models.Products {

    public class ProductsService {

        protected ApplicationContext Application { get; }

        protected IContentService ContentService => Application.Services.ContentService;

        protected IMediaService MediaService => Application.Services.MediaService;

        public ProductsService() : this(ApplicationContext.Current) { }

        public ProductsService(ApplicationContext application) {
            Application = application;
        }

        public List<ImportProductResult> ImportProducts(PerformContext hangfire = null) {

            // Hangfire logging
            hangfire.WriteLine("Fetching products from source");

            // Map the full path to the JSON file on disk
            string path = IOHelper.MapPath("~/App_Data/Products.json");

            // Read the raw contents of the file
            string contents = System.IO.File.ReadAllText(path);

            // Hangfire logging
            hangfire.WriteLine("> Done");

            // Hangfire logging
            hangfire.WriteLine("Parsing products from JSON");

            // Deserialize the products
            ProductList list = JsonConvert.DeserializeObject<ProductList>(contents);

            // Hangfire logging
            hangfire.WriteLine("> Done");

            // Hangfire logging
            hangfire.WriteLine("Fetching existing products and images from Umbraco");

            // Get all children (products) from the container
            Dictionary<string, IContent> existing = ContentService.GetChildren(ProductConstants.Content.Products).ToDictionary(x => x.GetValue<string>("productId"));

            // Get all existing product images
            Dictionary<string, IMedia> existingImages = MediaService.GetChildren(ProductConstants.Media.Products).ToDictionary(x => x.GetValue<string>("externalImageId"));

            // Hangfire logging
            hangfire.WriteLine("> Done");

            // Hangfire logging
            hangfire.WriteLine("Synchronizing products in Umbraco");

            // Initialize a new progress bar
            IProgressBar progress1 = hangfire.WriteProgressBar();

            List<ImportProductResult> results = new List<ImportProductResult>();

            // Iterate through each product from the JSON file
            foreach (Product product in list.Products.WithProgress(progress1)) {

                // Hangfire logging
                hangfire.WriteLine("  Importing product " + product.Name + " (" + product.Id + ")");

                // Has the product already been imported?
                IContent content;
                if (existing.TryGetValue(product.Id, out content)) {

                    // Get the old and new data values
                    string oldValue = content.GetValue<string>("productData");

                    // Serialize the product to a raw JSON string
                    string newValue = "_" + JsonConvert.SerializeObject(product);

                    // Does the two values match?
                    if (oldValue == newValue) {
                        
                        // Remove the product from the dictinary
                        existing.Remove(product.Id);

                        // Add the result to the overall list
                        results.Add(new ImportProductResult {
                            Status = ImportProductStatus.NotModified,
                            Content = content,
                            ProductId = product.Id,
                            Product = product
                        });

                        // Hangfire logging
                        hangfire.WriteLine("  > Not modified");

                        // Continue to the newxt product
                        continue;

                    }

                    // Import the product image(s)
                    string images = ImportImages(existingImages, product);

                    // Update all readonly properties
                    content.SetValue("productData", "_" + JsonConvert.SerializeObject(product));
                    content.SetValue("price", product.Price);

                    // Update other properties (outcommented as we don't want to overwrite if an editor has changed the properties)
                    //content.SetValue("category", String.Join(",", product.Categories));
                    //content.SetValue("description", product.Description);
                    //content.SetValue("photos", images);
                    //content.SetValue("features", GetFeaturesValue(product));

                    // Save and publish the product
                    ContentService.SaveAndPublishWithStatus(content, ProductConstants.ImportUserId);

                    // Remove the product from the dictinary
                    existing.Remove(product.Id);

                    // Add the result to the overall list
                    results.Add(new ImportProductResult {
                        Status = ImportProductStatus.Updated,
                        Content = content,
                        ProductId = product.Id,
                        Product = product
                    });

                    // Hangfire logging
                    hangfire.WriteLine("  > Updated");

                } else {

                    // Create a new content item
                    content = ContentService.CreateContent(product.Name, ProductConstants.Content.Products, ProductConstants.ContentTypes.Product, ProductConstants.ImportUserId);

                    // Import the product image(s)
                    string images = ImportImages(existingImages, product);

                    // Set the properties
                    content.SetValue("productName", product.Name);
                    content.SetValue("price", product.Price);
                    content.SetValue("category", String.Join(",", product.Categories));
                    content.SetValue("description", product.Description);
                    content.SetValue("sku", product.Sku);
                    content.SetValue("photos", images);
                    content.SetValue("features", GetFeaturesValue(product));
                    content.SetValue("productId", product.Id);
                    content.SetValue("productData", "_" + JsonConvert.SerializeObject(product));

                    // Save and publish the product
                    ContentService.SaveAndPublishWithStatus(content, ProductConstants.ImportUserId);

                    // Remove the product from the dictinary
                    existing.Remove(product.Id);

                    // Add the result to the overall list
                    results.Add(new ImportProductResult {
                        Status = ImportProductStatus.Added,
                        Content = content,
                        ProductId = product.Id,
                        Product = product
                    });

                    // Hangfire logging
                    hangfire.WriteLine("  > Added");

                }

            }
            
            // Hangfire logging
            hangfire.WriteLine("> Done");

            // Any products left in "existing" no longer exists in the JSON file and should be deleted
            if (existing.Any()) {

                // Hangfire logging
                hangfire.WriteLine("Deleting products from Umbraco");
                
                // Initialize a new progress bar
                IProgressBar progress2 = hangfire.WriteProgressBar();
                
                foreach (IContent content in existing.Values.WithProgress(progress2)) {
                
                    // Hangfire logging
                    hangfire.WriteLine(" Deleting " + content.Name);
                
                    // Delete the product in Umbraco
                    ContentService.Delete(content, ProductConstants.ImportUserId);
                
                    // Add the result to the overall list
                    results.Add(new ImportProductResult {
                        Status = ImportProductStatus.Deleted,
                        Content = content,
                        ProductId = content.GetValue<string>("productId")
                    });

                    // Hangfire logging
                    hangfire.WriteLine("  > Done");

                }
                
                // Hangfire logging
                hangfire.WriteLine("> Done");

            }

            return results;

        }

        /// <summary>
        /// Imports images that haven't already been imported in Umbraco. Also generates the string value for the media picker.
        /// </summary>
        /// <param name="existing">Dictionary of existing images.</param>
        /// <param name="product">The product.</param>
        /// <returns></returns>
        private string ImportImages(Dictionary<string, IMedia> existing, Product product) {
            
            List<string> temp = new List<string>();

            foreach (ProductImage image in product.Images) {

                IMedia media;
                if (existing.TryGetValue(image.Id, out media)) {
                    temp.Add("umb://media/" + media.Key.ToString("N"));
                    continue;
                }

                // Determine the full URL and the name and the full path of the image
                string url = image.Url;
                string filename = image.Name;
                string path1 = IOHelper.MapPath("~/App_Data/TEMP/Import/");
                string path2 = IOHelper.MapPath("~/App_Data/TEMP/Import/" + image.Id + ".jpg");

                // Make sure we have a directory
                Directory.CreateDirectory(path1);
                
                // Download the image to the disk
                using (WebClient client = new WebClient()) {
                    client.DownloadFile(url, path2);
                }
                
                // Add the image in Umbraco
                using (FileStream fs = new FileStream(path2, FileMode.Open)) {
                    media = MediaService.CreateMedia(filename, ProductConstants.Media.Products, "Image", ProductConstants.ImportUserId);
                    media.SetValue("umbracoFile", filename, fs);
                    media.SetValue("umbracoWidth", image.Width);
                    media.SetValue("umbracoHeight", image.Height);
                    media.SetValue("umbracoBytes", image.Size);
                    media.SetValue("externalImageId", image.Id);
                    MediaService.Save(media, ProductConstants.ImportUserId);
                    temp.Add("umb://media/" + media.Key.ToString("N"));
                }

            }

            return String.Join(",", temp);

        }

        /// <summary>
        /// Generates the correct value for saving the features using Nested Content.
        /// </summary>
        /// <param name="product">The product.</param>
        private string GetFeaturesValue(Product product) {
            
            JArray temp = new JArray();

            foreach (ProductFeature feature in product.Features) {
                
                temp.Add(new JObject {
                    {"name", feature.Name},
                    {"ncContentTypeAlias", "feature"},
                    {"featureName", feature.Name},
                    {"featureDetails", feature.Details}
                });

            }

            return temp.ToString();

        }

    }

}