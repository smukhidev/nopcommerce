using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Routing;
using System.Xml;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.ShurjoPay.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.ShurjoPay
{
    /// <summary>
    /// ShurjoPay payment processor
    /// </summary>
    public class ShurjoPayPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        
        private readonly ShurjoPayPaymentSettings _ShurjoPayPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public ShurjoPayPaymentProcessor(ShurjoPayPaymentSettings ShurjoPayPaymentSettings, 
            ISettingService settingService, ICurrencyService currencyService,
            CurrencySettings currencySettings, IWebHelper webHelper,
            ILocalizationService localizationService)
        {
            this._ShurjoPayPaymentSettings = ShurjoPayPaymentSettings;
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._webHelper = webHelper;
            this._localizationService = localizationService;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Format the string needed to post to the Generate page
        /// </summary>
        /// <param name="fieldName">shurjo Parameter Name</param>
        /// <param name="value">Value of Parameter</param>
        /// <returns>Formated value for the URL</returns>
        private string Format(string fieldName, string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : string.Format("&{0}={1}", fieldName, value);
        }

        /// <summary>
        /// Parse the result of the transaction request and save the appropriate fields in an object to be used later
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Parse the XML Returned and save all the values to be displayed to user
        /// </summary>
        /// <param name="resultXml">XML of the transaction result</param>
        private ValdiationRequestResult ParseXmlResult(string resultXml)
        {
            var result = new ValdiationRequestResult();
            string currentNode;
            var sr = new StringReader(resultXml);
            var xtr = new XmlTextReader(sr)
            {
                XmlResolver = null,
                WhitespaceHandling = WhitespaceHandling.None
            };

            // get the root node
            xtr.Read();

            if ((xtr.NodeType == XmlNodeType.Element) && (xtr.Name == "TransactionResponse"))
            {
                while (xtr.Read())
                {
                    if ((xtr.NodeType != XmlNodeType.Element) || xtr.IsEmptyElement)
                        continue;

                    currentNode = xtr.Name;
                    xtr.Read();
                    if (xtr.NodeType != XmlNodeType.Text)
                        continue;

                    switch (currentNode)
                    {
                        case "AuthCode":
                            result.AuthCode = xtr.Value;
                            break;
                        case "ResponseCode":
                            result.ResponseCode = xtr.Value;
                            break;
                        case "ReturnAmount":
                            result.ReturnAmount = xtr.Value;
                            break;
                        case "TrxnStatus":
                            result.TrxnStatus = xtr.Value;
                            break;
                        case "TrxnNumber":
                            result.TrxnNumber = xtr.Value;
                            break;
                        case "MerchantOption1":
                            result.MerchnatOption1 = xtr.Value;
                            break;
                        case "MerchantOption2":
                            result.MerchnatOption2 = xtr.Value;
                            break;
                        case "MerchantOption3":
                            result.MerchnatOption3 = xtr.Value;
                            break;
                        case "MerchantInvoice":
                            result.ReferenceInvoice = xtr.Value;
                            break;
                        case "MerchantReference":
                            result.ReferenceNumber = xtr.Value;
                            break;
                        case "TrxnResponseMessage":
                            result.TrxnResponseMessage = xtr.Value;
                            break;
                        case "ErrorMessage":
                            result.ErrorMessage = xtr.Value;
                            break;
                    }
                }
            }
            else if ((xtr.NodeType == XmlNodeType.Element) && (xtr.Name == "TransactionRequest"))
            {
                while (xtr.Read())
                {
                    if ((xtr.NodeType != XmlNodeType.Element) || xtr.IsEmptyElement)
                        continue;

                    currentNode = xtr.Name;
                    xtr.Read();
                    if (xtr.NodeType != XmlNodeType.Text)
                        continue;

                    if (currentNode == "Error")
                    {
                        result.ErrorMessage = xtr.Value;
                    }
                }
            }
            return result;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult { NewPaymentStatus = PaymentStatus.Pending };
            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gatshurjos that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// 

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            if (_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode != "BDT")
                throw new NopException("send amounts to the generator in Bangladeshi Taka (BDT) ");

            
            HttpContext.Current.Session.Add("MyPostProcessPaymentRequest", postProcessPaymentRequest);
            HttpContext.Current.Response.Redirect(_webHelper.GetStoreLocation() + "/Plugins/PaymentShurjoPay/Mypayment");
           
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return _ShurjoPayPaymentSettings.AdditionalFee;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //ShurjoPay is the redirection payment method
            //It also validates whether order is also paid (after redirection) so customers will not be able to pay twice
            
            //payment status should be Pending
            if (order.PaymentStatus != PaymentStatus.Pending)
                return false;

            //let's ensure that at least 1 minute passed after order is placed
            return !((DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes < 1);
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentShurjoPay";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.ShurjoPay.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentShurjoPay";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.ShurjoPay.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentShurjoPayController);
        }

        public override void Install()
        {
            var settings = new ShurjoPayPaymentSettings()
            {
                Password = "JehPNXF58rXs",
                Username = "TestAccount",
                ReturnPage = "https://payment.shurjogatshurjo.com/",
                AdditionalFee = 0
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.ShurjoPay.RedirectionTip", "You will be redirected to shurjoPay site to complete the order.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.ShurjoPay.Password", "Password");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.ShurjoPay.Password.Hint", "Enter Password.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.ShurjoPay.Username", "Username");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.ShurjoPay.Username.Hint", "Enter username.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.ShurjoPay.UserPrefixe", "UserPrefixe");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.ShurjoPay.UserPrefixe.Hint", "Enter User Prefixe.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.ShurjoPay.ReturnPage", "Return Page");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.ShurjoPay.ReturnPage.Hint", "Enter Return Page.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.ShurjoPay.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.ShurjoPay.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.ShurjoPay.PaymentMethodDescription", "You will be redirected to shurjoPay site to complete the order.");
            
            base.Install();
        }

        public override void Uninstall()
        {
            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.ShurjoPay.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.ShurjoPay.Password");
            this.DeletePluginLocaleResource("Plugins.Payments.ShurjoPay.Password.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.ShurjoPay.Username");
            this.DeletePluginLocaleResource("Plugins.Payments.ShurjoPay.Username.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.ShurjoPay.UserPrefixe");
            this.DeletePluginLocaleResource("Plugins.Payments.ShurjoPay.UserPrefixe.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.ShurjoPay.ReturnPage");
            this.DeletePluginLocaleResource("Plugins.Payments.ShurjoPay.ReturnPage.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.ShurjoPay.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.ShurjoPay.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.ShurjoPay.PaymentMethodDescription");
            
            base.Uninstall();
        }

        /// <summary>
        /// Procedure to check the 64 character access payment code
        /// for security
        /// </summary>
        /// <param name="accessPaymentCode">64 char code</param>
        /// <returns>true if found; false if not found</returns>
        public ValdiationRequestResult CheckAccessCode(string accessPaymentCode)
        {
            //POST to Payment gatshurjo the access code returned
            var strPost = "Password=" + _ShurjoPayPaymentSettings.Password;
            strPost += Format("AccessPaymentCode", accessPaymentCode);
            strPost += Format("UserName", _ShurjoPayPaymentSettings.Username);

            var url = _ShurjoPayPaymentSettings.ReturnPage + "Result?" + strPost;

            var objRequest = (HttpWebRequest)WebRequest.Create(url);
            objRequest.Method = WebRequestMethods.Http.Get;
            string resultXml;

            try
            {
                var objResponse = (HttpWebResponse)objRequest.GetResponse();

                //get the response from the transaction generate page
                using (var sr = new StreamReader(objResponse.GetResponseStream()))
                {
                    resultXml = sr.ReadToEnd();
                    // Close and clean up the StreamReader
                    sr.Close();
                }
            }
            catch (Exception exc)
            {
                return new ValdiationRequestResult
                {
                    ErrorMessage = exc.Message
                };
            }

            //parse the results save the values
            return ParseXmlResult(resultXml);
        }

        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Redirection;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            get { return _localizationService.GetResource("Plugins.Payments.ShurjoPay.PaymentMethodDescription"); }
        }

        #endregion
    }
}
