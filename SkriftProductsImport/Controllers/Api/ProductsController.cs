using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Http;
using SkriftProductsImport.Models.Products;
using SkriftProductsImport.Models.Products.Import;
using Skybrud.Essentials.Strings.Extensions;
using Umbraco.Web.Editors;

namespace SkriftProductsImport.Controllers.Api {

    public class ProductsController : UmbracoAuthorizedJsonController {

        [HttpGet]
        public object Import() {
            
            // Simulates that the import takes more than a few ms
            Thread.Sleep(5000);

            ProductsService service = new ProductsService();

            List<ImportProductResult> result = service.ImportProducts();

            return new {
                total = result.Count,
                items = (
                    from item in result
                    select new {
                        id = item.ProductId,
                        name = item.Content.Name,
                        status = item.Status.ToUnderscore().Replace("_", " ").FirstCharToUpper(),
                        product = item.Product,
                        contentId = item.Content?.Id
                    }
                )
            };

        }

    }

}