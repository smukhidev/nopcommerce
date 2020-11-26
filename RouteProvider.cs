using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.ShurjoPay
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //Merchant return
            routes.MapRoute("Plugin.Payments.ShurjoPay.MerchantReturn",
                 "Plugins/PaymentShurjoPay/MerchantReturn",
                 new { controller = "PaymentShurjoPay", action = "MerchantReturn" },
                 new[] { "Nop.Plugin.Payments.ShurjoPay.Controllers" }
            );

            //For payment
            routes.MapRoute("Plugin.Payments.ShurjoPay.Mypayment",
                 "Plugins/PaymentShurjoPay/Mypayment",
                 new { controller = "PaymentShurjoPay", action = "Mypayment" },
                 new[] { "Nop.Plugin.Payments.ShurjoPay.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
