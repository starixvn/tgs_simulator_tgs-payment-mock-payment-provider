using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using MockS88Pay.Utilities;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Transactions;

namespace MockS88Pay.Controllers
{
    [ApiController]
    [Route("api")]
    public class HomeController(ILogger<HomeController> logger, IConfiguration configuration) : ControllerBase
    {

        [HttpPost("{merchantCode}/v3/dopayment")]
        public async Task<IResult> DoPaymentAsync([FromServices] S88PayCryptor cryptor, [FromServices] HttpClient httpClient)
        {
            var apiKey = configuration.GetValue<string>("Crypto:apiKey");
            var secretKey = configuration.GetValue<string>("Crypto:secretKey");
            var decryptString = await getDecryptData(cryptor, apiKey, secretKey);
            logger.LogInformation("Request Body: {decryptString}", decryptString);

            var dict = parseDecryptString(decryptString);
            dict["transaction_actual_amount"] = dict["transaction_amount"];
            dict["transaction_no"] = "9888";
            dict["transaction_status"] = "2";
            dict["transaction_fee"] = string.Empty;
            var source = string.Join(
                    "&",
                    dict.Select(p => $"{p.Key}={p.Value}"));
            var encryptString = cryptor.AesEncrypt(source, apiKey!, secretKey!);
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

        private async Task<string> getDecryptData(S88PayCryptor cryptor, string? apiKey, string? secretKey)
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            var node = JsonNode.Parse(body);
            var key = node!["key"]!.GetValue<string>();
            var decryptString = cryptor.AesDecrypt(key, apiKey!, secretKey!);

            return decryptString;
        }

        private Dictionary<string, string> parseDecryptString(string decryptString)
        {
            var decryptArray = decryptString.Split("&");
            var dict = new Dictionary<string, string>();

            foreach (var item in decryptArray)
            {
                var kv = item.Split("=");

                dict[kv[0]] = kv[1];
            }

            return dict;
        }

        private async Task NotifyDepositedAsync(string encryptedKey, Dictionary<string, string> content)
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
            logger.LogInformation("Response is: {respone}", respone);
            return;
        }

        [HttpPost("v1/payout/{merchantCode}")]
        public async Task<IResult> Payout([FromServices] S88PayCryptor cryptor)
        {
            var apiKey = configuration.GetValue<string>("Crypto:apiKey");
            var secretKey = configuration.GetValue<string>("Crypto:secretKey");
            var decryptString = await getDecryptData(cryptor, apiKey, secretKey);
            logger.LogInformation("Request Body: {decryptString}", decryptString);

            var dict = parseDecryptString(decryptString);
            _ = NotifyWithdrawedAsync(cryptor, dict, apiKey!, secretKey!);
            return Results.Json(new { success = true });
        }

        private async Task NotifyWithdrawedAsync(S88PayCryptor cryptor, Dictionary<string, string> content, string apiKey, string secretKey)
        {
            await Task.Delay(30000);
            var httpClient = new HttpClient { BaseAddress = new Uri(content["callback_url"]) };
            httpClient.BaseAddress = new Uri(content["callback_url"]);
            var data = new
            {
                transaction_no = "123aa",
                transaction_code = content["transaction_code"],
                transaction_status = 2,
                transaction_amount = content["transaction_amount"],
                transaction_fee = "0",
                currency_code = "VND_Test",
                transaction_ref = "eliastestwithdraw",
                transaction_ref2 = "eliastestwithdraw1"
            };
            var rawData = JsonSerializer.Serialize(data);
            var encryptString = cryptor.AesEncrypt(rawData, apiKey!, secretKey!);
            var respone = await httpClient
                .PostAsJsonAsync(string.Empty, new
                {
                    key = encryptString,
                    data.transaction_code,
                    data.transaction_no,
                    user_id = content["user_id"],
                    note = "good"
                }, CancellationToken.None)
                .ConfigureAwait(false);
            logger.LogInformation("Response after callback withdraw is: {respone}", respone);
        }

        [HttpPost("v1/{merchantCode}/transactions/status")]
        public async Task<IResult> GetStatus([FromServices] S88PayCryptor cryptor)
        {
            var apiKey = configuration.GetValue<string>("Crypto:apiKey");
            var secretKey = configuration.GetValue<string>("Crypto:secretKey");
            var decryptString = await getDecryptData(cryptor, apiKey, secretKey);
            logger.LogInformation("Request Body: {decryptString}", decryptString);

            var dict = parseDecryptString(decryptString);
            dict["transaction_no"] = "123aa";
            dict["traction_status"] = "true";
            dict["type"] = "quite good";
            var transactionData = new List<Dictionary<string, string>> { dict };

            return Results.Json(new { success = true, data = transactionData });
        }
    }
}
