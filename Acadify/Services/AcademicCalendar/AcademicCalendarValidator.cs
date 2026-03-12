using Acadify.Models.AdminPages;
using System.Text.RegularExpressions;

namespace Acadify.Services.AcademicCalendar
{
    public static class AcademicCalendarValidator
    {
        private static readonly HashSet<string> AllowedEvents = new()
        {
            "بداية فترة تسجيل المقررات للطالب والطالبات على ODUS PLUS",
            "نهاية فترة تسجيل المقررات للطالب والطالبات على ODUS PLUS",
            "بداية فترة تسجيل المقررات للمرشدين الأكاديميين على ODUS PLUS وللشؤون التعليمية والوكلاء والوكيلات بالكليات",
            "نهاية فترة التسجيل للمرشدين الأكاديميين",
            "بداية تقديم طلبات سحب مقرر للطالب والطالبات في الفصل الدراسي الحالي",
            "نهاية فترة تقديم طلب سحب مقرر للفصل الدراسي الحالي",
            "بداية تقديم طلبات التأجيل",
            "نهاية تقديم طلبات التأجيل",
            "بداية تقديم طلبات الاعتذار",
            "نهاية فترة تقديم طلبات الاعتذار"
        };

        private static bool IsValidHijri(string? s) =>
            s != null && Regex.IsMatch(s, @"^\d{2}/\d{2}/\d{4}هـ$");

        private static bool IsValidGregorian(string? s) =>
            s != null && Regex.IsMatch(s, @"^\d{2}/\d{2}/\d{4}م$");

        private static bool IsValidDay(string? s) =>
            s == null || new[]
            {
                "الأحد","الاثنين","الثلاثاء","الأربعاء","الخميس","الجمعة","السبت"
            }.Contains(s.Trim());

        public static void ValidateOrThrow(AcademicCalendarAiResult result)
        {
            if (result?.Events == null)
                throw new InvalidOperationException("AI result is null.");

            if (result.Events.Count != 10)
                throw new InvalidOperationException($"Expected 10 events, got {result.Events.Count}.");

            foreach (var e in result.Events)
            {
                if (!AllowedEvents.Contains(e.Event))
                    throw new InvalidOperationException($"Unexpected event: {e.Event}");

                if (!IsValidDay(e.Day_Ar))
                    throw new InvalidOperationException($"Invalid day_ar: {e.Day_Ar}");

                if (e.Hijri_Date != null && !IsValidHijri(e.Hijri_Date))
                    throw new InvalidOperationException($"Invalid hijri_date format: {e.Hijri_Date}");

                if (e.Gregorian_Date != null && !IsValidGregorian(e.Gregorian_Date))
                    throw new InvalidOperationException($"Invalid gregorian_date format: {e.Gregorian_Date}");
            }
        }
    }
}