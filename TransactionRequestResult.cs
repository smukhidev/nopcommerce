namespace Nop.Plugin.Payments.ShurjoPay
{
    public class TransactionRequestResult
    {
        public bool Result { get; set; }
        public string Uri { get; set; }
        public string Error { get; set; }
    }
}
