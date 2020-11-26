using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.ShurjoPay
{
    public class ShurjoPayPaymentSettings : ISettings
    {

        public string Username { get; set; }
        public string Password { get; set; }
        public string UserPrefixe { get; set; }
        public string ReturnPage { get; set; }
        public decimal AdditionalFee { get; set; }
    }
}
