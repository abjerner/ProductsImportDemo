using Newtonsoft.Json;

namespace SkriftProductsImport.Models.Products {

    public class ProductList {

        [JsonProperty("products")]
        public Product[] Products { get; set; }

    }

}