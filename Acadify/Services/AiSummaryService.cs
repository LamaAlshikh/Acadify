using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Acadify.Services
{
    public class AiSummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AiSummaryService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> SummarizeMeetingChatAsync(string chatRecord)
        {
            if (string.IsNullOrWhiteSpace(chatRecord))
                return "";

            var prompt = $"""
لخص المحادثة التالية باللغة العربية الفصحى، بصياغة رسمية ومختصرة مناسبة تمامًا لوضعها في خانة:
"Proposed Solutions / Advise / Brief notes"

القواعد:
- اكتب الملخص في فقرة قصيرة من سطر إلى 3 أسطر.
- ركز فقط على:
  1) موضوع النقاش الأساسي
  2) التوجيه أو النصيحة التي قدمتها المرشدة
  3) القرار أو التوصية النهائية إن وجدت
- تجاهل التحية والكلام الجانبي والتكرار.
- لا تكتب "Student:" أو "Advisor:".
- لا تنقل الحوار حرفيًا إلا عند الضرورة.
- لا تستخدم تعداد نقطي.
- الناتج النهائي يكون بالعربية فقط.

المحادثة:
{chatRecord}
""";

            return await GetRawResponseAsync(
                prompt,
                "أنت مساعد متخصص في تلخيص محادثات الإرشاد الأكاديمي بصياغة عربية رسمية وواضحة.",
                250);
        }

        public async Task<string> GetRawResponseAsync(string prompt, string instructions, int maxOutputTokens = 800)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return "";

            var apiKey = _configuration["OpenAI:ApiKey"];
            var model = _configuration["OpenAI:Model"] ?? "gpt-4.1-mini";

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("OpenAI API key not found in configuration.");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var body = new
            {
                model = model,
                input = prompt,
                instructions = instructions,
                temperature = 0.1,
                max_output_tokens = maxOutputTokens
            };

            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync("https://api.openai.com/v1/responses", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"OpenAI API error: {response.StatusCode} - {responseText}");

            using var doc = JsonDocument.Parse(responseText);

            if (doc.RootElement.TryGetProperty("output", out var outputArray))
            {
                var sb = new StringBuilder();

                foreach (var outputItem in outputArray.EnumerateArray())
                {
                    if (outputItem.TryGetProperty("content", out var contentArray))
                    {
                        foreach (var contentItem in contentArray.EnumerateArray())
                        {
                            if (contentItem.TryGetProperty("type", out var typeProp) &&
                                typeProp.GetString() == "output_text" &&
                                contentItem.TryGetProperty("text", out var textProp))
                            {
                                sb.AppendLine(textProp.GetString());
                            }
                        }
                    }
                }

                var finalText = sb.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(finalText))
                    return finalText;
            }

            throw new Exception("OpenAI response did not contain output text.");
        }
    }
}