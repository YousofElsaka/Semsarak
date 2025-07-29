using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SEMSARK.Services.Payment
{
    public class PaymobService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly int _integrationId = 5198369;

        public PaymobService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _apiKey = configuration["Paymob:ApiKey"];
        }

        public async Task<string?> GetAuthTokenAsync()
        {
            var requestBody = new
            {
                api_key = _apiKey
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://accept.paymob.com/api/auth/tokens", content);
            if (!response.IsSuccessStatusCode)
                return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            return doc.RootElement.GetProperty("token").GetString();
        }

        // تم تعديل الدالة لتقبل redirectUrl
        public async Task<int?> CreateOrderAsync(string token, int amountCents, string currency = "EGP", string? redirectUrl = null)
        {
            var requestBody = new
            {
                auth_token = token,
                delivery_needed = false,
                amount_cents = amountCents,
                currency = currency,
                items = new List<object>(), // فاضي عشان مش بنبيع منتجات
                redirect_url = redirectUrl // أضف هذا السطر
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://accept.paymob.com/api/ecommerce/orders", content);
            if (!response.IsSuccessStatusCode)
                return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            return doc.RootElement.GetProperty("id").GetInt32();
        }

        public async Task<string?> GetPaymentKeyAsync(string token, int orderId, int amountCents, string billingEmail, string billingName)
        {
            var requestBody = new
            {
                auth_token = token,
                amount_cents = amountCents,
                expiration = 3600,
                order_id = orderId,
                currency = "EGP",
                integration_id = _integrationId,
                billing_data = new
                {
                    email = billingEmail,
                    first_name = billingName,
                    last_name = "User",
                    phone_number = "01000000000",
                    apartment = "NA",
                    floor = "NA",
                    street = "NA",
                    building = "NA",
                    city = "NA",
                    country = "EG",
                    state = "NA"
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://accept.paymob.com/api/acceptance/payment_keys", content);
            if (!response.IsSuccessStatusCode)
                return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseContent);
            return doc.RootElement.GetProperty("token").GetString();
        }

        public async Task<bool> GetTransactionStatusAsync(string transactionId)
        {
            var authToken = await GetAuthTokenAsync();
            if (authToken == null)
                return false;

            var response = await _httpClient.GetAsync($"https://accept.paymob.com/api/acceptance/transactions/{transactionId}?token={authToken}");

            if (!response.IsSuccessStatusCode)
                return false;

            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);
            var success = json["success"]?.Value<bool>() ?? false;

            return success;
        }
    }
}