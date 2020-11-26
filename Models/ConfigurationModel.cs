using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.ShurjoPay.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.ShurjoPay.Password")]
        public string Password { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ShurjoPay.Username")]
        public string Username { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ShurjoPay.UserPrefixe")]
        public string UserPrefixe { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ShurjoPay.ReturnPage")]
        public string ReturnPage { get; set; }

        [NopResourceDisplayName("Plugins.Payments.ShurjoPay.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
    }
}