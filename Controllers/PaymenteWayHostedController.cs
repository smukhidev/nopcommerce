using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.ShurjoPay.Models;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using System.Xml;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;
using System.Net.Http;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;



namespace Nop.Plugin.Payments.ShurjoPay.Controllers
{
    public class PaymentShurjoPayController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ShurjoPayPaymentSettings _ShurjoPayPaymentSettings;
        private readonly PaymentSettings _paymentSettings;


        public PaymentShurjoPayController(ISettingService settingService,
            IPaymentService paymentService, IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            ShurjoPayPaymentSettings ShurjoPayPaymentSettings,
            PaymentSettings paymentSettings)
        {
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._ShurjoPayPaymentSettings = ShurjoPayPaymentSettings;
            this._paymentSettings = paymentSettings;
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult okay()
        {
            return null;
        }


        private readonly HttpClient _client = new HttpClient();

        

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new ConfigurationModel
            {
                
                Username = _ShurjoPayPaymentSettings.Username,
                Password = _ShurjoPayPaymentSettings.Password,
                UserPrefixe = _ShurjoPayPaymentSettings.UserPrefixe,
                ReturnPage = _ShurjoPayPaymentSettings.ReturnPage,
                AdditionalFee = _ShurjoPayPaymentSettings.AdditionalFee
            };

            return View("~/Plugins/Payments.ShurjoPay/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //save settings
            
            _ShurjoPayPaymentSettings.Username = model.Username;
            _ShurjoPayPaymentSettings.Password = model.Password;
            _ShurjoPayPaymentSettings.UserPrefixe = model.UserPrefixe;
            _ShurjoPayPaymentSettings.ReturnPage = model.ReturnPage;
            _ShurjoPayPaymentSettings.AdditionalFee = model.AdditionalFee;
            _settingService.SaveSetting(_ShurjoPayPaymentSettings);

            return Configure();
        }

        //private readonly HttpClient _client = new HttpClient();

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            return View("~/Plugins/Payments.ShurjoPay/Views/PaymentInfo.cshtml");
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }


        [ValidateInput(false)]
        public async Task<ActionResult> Mypayment(ProcessPaymentRequest processPaymentRequest)
        {

            var postProcessPaymentRequest = (PostProcessPaymentRequest)Session["MyPostProcessPaymentRequest"];
            Random random = new Random();
            string uniqueId = _ShurjoPayPaymentSettings.UserPrefixe;
            string url = _ShurjoPayPaymentSettings.ReturnPage;
            string txId = uniqueId +"_"+ new DateTimeOffset(DateTime.Now).ToString("yyMMddHHmmss")+ random.Next(0,2).ToString();
            string user = _ShurjoPayPaymentSettings.Username;
            string password = _ShurjoPayPaymentSettings.Password;
            //int payAmount = (int)mp.TxnAmount;
            var spdata = "<?xml version=\"1.0\" encoding=\"utf-8\"?><shurjoPay>";
            spdata += $"<merchantName>{user}</merchantName>";
            spdata += $"<merchantPass>{password}</merchantPass>";
            spdata += $"<userIP>103.108.144.135</userIP>";
            spdata += $"<uniqID>{txId}</uniqID>";
            spdata += $"<txid>{postProcessPaymentRequest.Order.Id}</txid>";
            
            spdata += $"<totalAmount>{postProcessPaymentRequest.Order.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture)}</totalAmount>";
            spdata += $"<paymentOption>bkash</paymentOption>";
            //spdata += $"<returnURL>{"http://localhost:15536/Plugins/PaymentShurjoPay/MerchantReturn"}</returnURL></shurjoPay>";
            spdata += $"<returnURL>{Url.Action("MerchantReturn", "PaymentShurjoPay", null, "http")}</returnURL></shurjoPay>";
            var data = new Dictionary<string, string> { { "spdata", spdata } };
            var response = await _client.PostAsync(url, new FormUrlEncodedContent(data));
            var form1 = await response.Content.ReadAsStringAsync();
            Session["Url"] = form1;



            return View("~/Plugins/Payments.ShurjoPay/Views/Mypayment.cshtml");
        }

        [ValidateInput(false)]
        public async Task<ActionResult> MerchantReturn()
        {
            
            string spData = Request["spdata"];
            string url = string.Format("https://shurjopay.com/merchant/decrypt.php?data={0}", spData);
            var response = await _client.GetAsync(url);
            var xmlResponse = await response.Content.ReadAsStringAsync();
            XDocument reader = XDocument.Parse(xmlResponse);

            var xmlElements = reader.Root;
            var txId = xmlElements.Descendants("txID").First().Value;
            var bankTxStatus = xmlElements.Descendants("bankTxStatus").First().Value;
            
            if (bankTxStatus== "SUCCESS")
            {
                //SUCCESS
                var postProcessPaymentRequest = (PostProcessPaymentRequest)Session["MyPostProcessPaymentRequest"];
                var orderId = Convert.ToInt32(postProcessPaymentRequest.Order.Id);
                var order = _orderService.GetOrderById(orderId);
                if (order == null) return RedirectToAction("Index", "Home", new { area = "" });

                order.AuthorizationTransactionId = txId;
                _orderService.UpdateOrder(order);

                if (_orderProcessingService.CanMarkOrderAsPaid(order))
                {
                    _orderProcessingService.MarkOrderAsPaid(order);
                }

                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });

            }
            else
                return RedirectToAction("Index", "Home", new { area = "" });

            

        }

        private TransactionRequestResult ParseRequestResults(string xml)
        {
            var sr = new StringReader(xml);
            var xtr = new XmlTextReader(sr)
            {
                XmlResolver = null,
                WhitespaceHandling = WhitespaceHandling.None
            };

            // get the root node
            xtr.Read();

            var res = new TransactionRequestResult();

            if ((xtr.NodeType != XmlNodeType.Element) || (xtr.Name != "TransactionRequest"))
                return res;

            while (xtr.Read())
            {
                if ((xtr.NodeType != XmlNodeType.Element) || xtr.IsEmptyElement)
                    continue;

                var currentNode = xtr.Name;
                xtr.Read();
                if (xtr.NodeType != XmlNodeType.Text)
                    continue;

                switch (currentNode)
                {
                    case "Result":
                        res.Result = bool.Parse(xtr.Value);
                        break;

                    case "URI":
                        res.Uri = xtr.Value;
                        break;

                    case "Error":
                        res.Error = xtr.Value;
                        break;
                }
            }

            return res;
        }
    }
}