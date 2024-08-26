using Microsoft.AspNetCore.Mvc;
using MockS88Pay.Utilities;
using System.Text.Json.Nodes;

namespace MockS88Pay.Controllers
{
    [ApiController]
    [Route("api")]
    public class HomeController(ILogger<HomeController> logger, IConfiguration configuration) : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [HttpPost("{merchantCode}/v3/dopayment")]
        public async Task<IResult> DoPaymentAsync([FromServices] S88PayCryptor cryptor, [FromServices] HttpClient httpClient)
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var apiKey = configuration.GetValue<string>("Crypto:apiKey");
            var secretKey = configuration.GetValue<string>("Crypto:secretKey");
            var node = JsonNode.Parse(body);
            var key = node!["key"]!.GetValue<string>();
            var decryptString = cryptor.AesDecrypt(key, apiKey!, secretKey!);
            Console.WriteLine($"Request Body: {decryptString}");

            var decryptArray = decryptString.Split("&");

            var dict = new Dictionary<string, string>();
            foreach (var item in decryptArray)
            {
                var kv = item.Split("=");

                dict[kv[0]] = kv[1];
            }
            dict["transaction_actual_amount"] = dict["transaction_amount"];
            dict["transaction_no"] = "9888";
            dict["transaction_status"] = "2";
            dict["transaction_fee"] = string.Empty;
            var source = string.Join(
                    "&",
                    dict.Select(p => $"{p.Key}={p.Value}"));
            var encryptString = cryptor.AesEncrypt(source, apiKey!, secretKey);
            _ = NotifyDepositedAsync(encryptString, dict);

            return Results.Json(new
            {
                status = "success",
                wallet_qr = "https://blog.tcea.org/wp-content/uploads/2022/05/qrcode_tcea.org-1.png",
                account_qr = "https://blog.tcea.org/wp-content/uploads/2022/05/qrcode_tcea.org-1.png",
                wallet_number = "123",
                account_number = "456",
                wallet_name = "ttt",
                account_name = "aaao",
                amount = "969",
                transaction_code = dict["transaction_code"],
                transaction_no = "9888",
                expired_at = DateTimeOffset.Now.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss"),
                expired_timezone = "GMT+07:00",
                match_code = "match_code_123",
                bank_name = "Ngan hang nha nuoc Viet Nam"
            });
        }

        private static async Task NotifyDepositedAsync(string encryptedKey, Dictionary<string, string> content)
        {
            await Task.Delay(30000);
            var httpClient = new HttpClient { BaseAddress = new Uri(content["callback_url"]) };
            httpClient.BaseAddress = new Uri(content["callback_url"]);
            var respone = await httpClient
                .PostAsJsonAsync(string.Empty, new
                {
                    key = encryptedKey,
                    transaction_code = content["transaction_code"],
                    transaction_no = "9888",
                    user_id = content["user_id"],
                    note = "good"
                }, CancellationToken.None)
                .ConfigureAwait(false);
            Console.WriteLine($"Response is: {respone.StatusCode}");
            return;
        }
    }
}
