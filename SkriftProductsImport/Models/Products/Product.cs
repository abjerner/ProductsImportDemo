using Newtonsoft.Json;

namespace SkriftProductsImport.Models.Products {

    public class Product {

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("categories")]
        public string[] Categories { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("sku")]
        public string Sku { get; set; }

        [JsonProperty("features")]
        public ProductFeature[] Features { get; set; }

        [JsonProperty("images")]
        public ProductImage[] Images { get; set; }

    }

}