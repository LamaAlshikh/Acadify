using System.Text.RegularExpressions;

namespace Acadify.Services.AcademicCalendar
{
    public static class OcrCleaner
    {
        public static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var text = raw;

            // توحيد ODUS PLUS
            text = Regex.Replace(
                text,
                @"ODUS\s*PLUS|PLUS\s*ODUS|ODUSPLUS|ODUS\s+PLUS|ODUS",
                "ODUS PLUS",
                RegexOptions.IgnoreCase);

            // حذف أكواد مثل (W1) (W2)
            text = Regex.Replace(text, @"\([A-Za-z]\d+\)", "", RegexOptions.IgnoreCase);

            // حذف بعض الرموز الغريبة
            text = Regex.Replace(text, @"[■◆●•]+", " ");

            // توحيد الأسطر
            text = text.Replace("\r\n", "\n");
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            // توحيد المسافات
            text = Regex.Replace(text, @"[ \t]{2,}", " ");

            return text.Trim();
        }
    }
}