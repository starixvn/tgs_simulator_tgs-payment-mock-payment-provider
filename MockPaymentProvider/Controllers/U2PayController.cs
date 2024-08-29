using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using MockPaymentProvider.Utilities;

namespace MockPaymentProvider.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class U2PayController(ILogger<U2PayController> logger, IConfiguration configuration, U2PayHasher hasher): ControllerBase
    {
        [HttpPost("order/create")]
        public async Task<IResult> CreateOrderAsync()
        {
            var raw = await Request.ReadFormAsync();
            logger.LogInformation("Request from U2Pay CreateOrder: {raw}", raw);

            var result = new 
            {
                code = 0,
                amount = long.Parse(raw["amount"]!.ToString()),
                orderNo = raw["orderNo"]!.ToString(),
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
                ["extra"] = "no extra"
            };

            var excludeKeys = new string[] { "userName", "channelNo" };

            
            request["sign"] = ComputHash(excludeKeys, request);

            _ = CallBack(raw["notifyUrl"]!, request);

            return Results.Json(result);
        }

        private string ComputHash(string[] excludeKeys, Dictionary<string, string> dict)
        => hasher.ComputeHash(configuration.GetValue<string>("U2Pay:privateKey")!, dict.Where(x => !excludeKeys.Contains(x.Key))!);

        private async Task CallBack(string url, Dictionary<string,string> data)
        {
            logger.LogInformation("Waiting to callback...{url}", url);
            await Task.Delay(30000);
            var request = new FormUrlEncodedContent(data);
            logger.LogInformation("Notify Request: {data}", data);
            var _httpClient = new HttpClient
            {
                BaseAddress = new Uri(url)
            };
            var response = await _httpClient.PostAsync(new Uri(url), request);
            logger.LogInformation("Received: {response}", response);

        }

        [HttpPost("payout/create")]
        public async Task<IResult> CreatePayoutAsync()
        {
            var raw = await Request.ReadFormAsync();
            logger.LogInformation("Request from U2Pay CreatePayout: {raw}", raw);

            var result = new
            {
                code = 0,
                amount = decimal.Parse(raw["amount"]!.ToString()),
                orderNo = raw["orderNo"]!.ToString(),
                tradeNo = "123",
            };

            var request = new Dictionary<string, string>
            {
                ["status"] = "PAID",
                ["tradeNo"] = result.tradeNo,
                ["orderNo"] = raw["orderNo"]!,
                ["name"] = "eliastestu2pay",
                ["bankName"]= "eliastestu2pay1",
                ["bankAccount"] ="test",
                ["bankBranch"]="TPHCM-1",
                ["memo"]="memo01",
                ["mobile"]="023",
                ["fee"]="0",
                ["amount"] = raw["amount"]!,
                ["extra"] = "no extra",
                ["reason"] = "hello",
            };

            var excludeKeys = new string[] { "bankBranch", "memo", "reason" };

            request["sign"] = ComputHash(excludeKeys, request);

            _ = CallBack(raw["notifyUrl"]!, request);

            return Results.Json(result);
        }
    }
}
