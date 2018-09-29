using Umbraco.Core.Models;

namespace SkriftProductsImport.Models.Products.Import {

    public class ImportProductResult {

        public ImportProductStatus Status { get; set; }

        public IContent Content { get; set; }

        public string ProductId { get; set; }

        public Product Product { get; set; }

    }

}