using Newtonsoft.Json;

namespace SkriftProductsImport.Models.Products {

    public class ProductFeature {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("details")]
        public string Details { get; set; }

    }

}