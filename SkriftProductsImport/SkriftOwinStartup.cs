using System.Web;
using Hangfire;
using Hangfire.Annotations;
using Hangfire.Console;
using Hangfire.Dashboard;
using Hangfire.Server;
using Microsoft.Owin;
using Owin;
using SkriftProductsImport;
using SkriftProductsImport.Models.Products;
using Umbraco.Core.Security;
using Umbraco.Web;

[assembly: OwinStartup(typeof(SkriftOwinStartup))]
namespace SkriftProductsImport {
    
    public class SkriftOwinStartup : UmbracoDefaultOwinStartup {

        public override void Configuration(IAppBuilder app) {

            base.Configuration(app);

            // Hangfire isn't supported by SQL CE, so the rest of the code is disabled for now
            return;

            // Configure Hangfire (step 1)
            GlobalConfiguration.Configuration
                .UseSqlServerStorage(Umbraco.Core.ApplicationContext.Current.DatabaseContext.ConnectionString)
                .UseConsole();

            // Make sure we only allow backoffice users access to the Hangfire dashboard
            DashboardOptions dashboardOptions = new DashboardOptions {
                Authorization = new[] {
                    new UmbracoAuthorizationFilter()
                }
            };

            // Configure Hangfire (step 2)
            app.UseHangfireDashboard("/hangfire", dashboardOptions);
            app.UseHangfireServer();

            // Add a new job for import products
            RecurringJob.AddOrUpdate(() => new HangfireJobs().ImportProducts(null), Cron.MinuteInterval(15));

        }

    }

    public class HangfireJobs {

        public void ImportProducts(PerformContext hangfire) {
            
            ProductsService service = new ProductsService();

            service.ImportProducts(hangfire);

        }

    }

    public class UmbracoAuthorizationFilter : IDashboardAuthorizationFilter {

        public bool Authorize([NotNull] DashboardContext context) {
            return new HttpContextWrapper(HttpContext.Current).GetUmbracoAuthTicket() != null;
        }

    }

}