using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PayPal.Api;
using PayPalGateway.Models;
using PayPalGateway.Services;

namespace PayPalGateway.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public HomeController(ILogger<HomeController> logger, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    //payment with paypal
    public ActionResult PaymentWithPaypal(string Cancel = null)
    {
        //getting the apiContext as earlier
        APIContext apiContext = PaypalConfiguration.GetAPIContext();

        try
        {
            string payerId = Request.Query["PayerID"].ToString();

            if (string.IsNullOrEmpty(payerId))
            {
                //this section will be executed first because PayerID doesn't exist
                //it is returned by the create function call of the payment class

                // Creating a payment
                // baseURL is the url on which paypal sendsback the data.
                string baseURI = Request.Scheme + "://" + Request.Host + "/Home/PaymentWithPayPal?";
                //here we are generating guid for storing the paymentID received in session
                //which will be used in the payment execution
                var guid = Convert.ToString((new Random()).Next(100000));

                //CreatePayment function gives us the payment approval url
                //on which payer is redirected for paypal account payment
                var createdPayment = this.CreatePayment(apiContext, baseURI + "guid=" + guid);

                //get links returned from paypal in response to Create function call

                var links = createdPayment.links.GetEnumerator();

                string paypalRedirectUrl = null;

                while (links.MoveNext())
                {
                    Links lnk = links.Current;

                    if (lnk.rel.ToLower().Trim().Equals("approval_url"))
                    {
                        //saving the payapalredirect URL to which user will be redirected for payment
                        paypalRedirectUrl = lnk.href;
                    }
                }

                // saving the paymentID in the key guid
                HttpContext.Session.SetString("payment", createdPayment.id);
                payment_id = createdPayment.id;

                return Redirect(paypalRedirectUrl);
            }
            else
            {
                // This function exectues after receving all parameters for the payment

                // var guid = Request.Query["guid"];

                var executedPayment = ExecutePayment(apiContext, payerId, HttpContext.Session.GetString("payment"));

                if (executedPayment.state.ToLower() != "approved")
                {
                    return View("FailureView");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error while processing payment", ex.ToString());

            if (ex is PayPal.PaymentsException)
            {
                var payPalException = ex as PayPal.PaymentsException;
                _logger.LogError("PayPal Error" + payPalException.Details.name);
                _logger.LogError("PayPal Error" + payPalException.Details.message);
                _logger.LogError("PayPal Error" + payPalException.Details.information_link);
                _logger.LogError("PayPal Error" + payPalException.Details.debug_id);
            }

            return View("FailureView");
        }

        return View("SuccessView");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    //create payment method

    private PayPal.Api.Payment payment;
    private string payment_id;

    private Payment CreatePayment(APIContext apiContext, string redirectUrl)
    {
        //similar to credit card create itemlist and add item objects to it
        var itemList = new ItemList() { items = new List<Item>() };

        //Adding Item Details like name, currency, price etc
        itemList.items.Add(new Item()
        {
            name = "Item Name comes here",
            currency = "USD",
            price = "20",
            quantity = "2",
            sku = "sku of item"
        });

        var payer = new Payer() { payment_method = "paypal" };

        // Configure Redirect Urls here with RedirectUrls object
        var redirUrls = new RedirectUrls()
        {
            cancel_url = redirectUrl + "&Cancel=true",
            return_url = redirectUrl
        };

        // similar as we did for credit card, do here and create details object
        // var details = new Details()
        // {
        //     tax = "5.00",
        //     shipping = "0",
        //     subtotal = "123"
        // };

        // similar as we did for credit card, do here and create amount object
        var amount = new Amount()
        {
            currency = "USD",
            total = "40",
            // details = details
        };

        var transactionList = new List<Transaction>();

        // Adding description about the transaction
        transactionList.Add(new Transaction()
        {
            description = "Transaction description",
            invoice_number = Guid.NewGuid().ToString(), //Generate an Invoice No
            amount = amount,
            item_list = itemList
        });

        this.payment = new Payment()
        {
            intent = "sale",
            payer = payer,
            transactions = transactionList,
            redirect_urls = redirUrls
        };

        // Create a payment using a APIContext
        return this.payment.Create(apiContext);
    }


    //execute payment method

    private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
    {
        var paymentExecution = new PaymentExecution() { payer_id = payerId };
        this.payment = new Payment() { id = paymentId };
        return this.payment.Execute(apiContext, paymentExecution);
    }

}
