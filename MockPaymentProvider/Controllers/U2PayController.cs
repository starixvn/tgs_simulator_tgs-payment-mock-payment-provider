using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace MockPaymentProvider.Controllers
{
    [ApiController]
    [Route("[controller]/api")]
    public class U2PayController(ILogger<U2PayController> logger, HttpClient httpClient): ControllerBase
    {
        [HttpPost("order/create")]
        public async Task<IResult> CreateOrderAsync()
        {
            var raw = await Request.ReadFormAsync();
            logger.LogInformation("Request from U2Pay CreateOrder: {raw}", raw.ToString());

            var result = new
            {
                code = "123",
                amount = raw["amount"]!,
                orderNo = raw["orderNo"]!,
                tradeNo = "123",
                targetUrl = "https://www.google.com/",
            };

            var request = new Dictionary<string, string>
            {
                ["status"] = "PAID",
                ["tradeNo"] = result.tradeNo,
                ["orderNo"] = raw["orderNo"]!,
                ["userNo"] = "6969",
                ["userName"] = "eliastestu2pay",
                ["channelNo"] = raw["channelNo"]!,
                ["amount"] = raw["amount"]!,
                ["discount"] = "0",
                ["lucky"] = "0",
                ["paid"] = raw["amount"]!,
                ["extra"] = "no extra",
                ["sign"] = raw["sign"]!
            };

            _ = CallBack(raw["notifyUrl"]!, request);

            return Results.Json(result);
        }

        private async Task CallBack(string url, Dictionary<string,string> data)
        {
            await Task.Delay(30000);
            var request = new FormUrlEncodedContent(data);
            logger.LogInformation("NotifyDeposited Request: {data}", data);
            var response = await httpClient.PostAsync(new Uri(url), request);
            logger.LogInformation("Received: {response}", response);

        }

        [HttpPost("payout/create")]
        public async Task<IResult> CreatePayoutAsync()
        {
            var raw = await Request.ReadFormAsync();
            logger.LogInformation("Request from U2Pay CreatePayout: {raw}", raw.ToString());

            var result = new
            {
                code = "999",
                amount = raw["amount"]!,
                orderNo = raw["orderNo"]!,
                tradeNo = "123",
            };

            var request = new Dictionary<string, string>
            {
                ["status"] = "PAID",
                ["tradeNo"] = result.tradeNo,
                ["orderNo"] = raw["orderNo"]!,
                ["name"] = "eliastestu2pay",
                ["bankName"]= "eliastestu2pay1",
                ["bankAccount"]="",
                ["bankBranch"]="",
                ["memo"]="",
                ["mobile"]="",
                ["fee"]="",
                ["amount"] = raw["amount"]!,
                ["paid"] = raw["amount"]!,
                ["extra"] = "no extra",
                ["sign"] = raw["sign"]!,
                ["reason"] = "",
            };

            _ = CallBack(raw["notifyUrl"]!, request);

            return Results.Json(result);
        }
    }
}
