using System;
using System.Collections.Generic;

namespace Acadify.Models.Db;

public partial class MeetingForm
{
    // المعرف الأساسي للنموذج (مرتبط بجدول Form)
    public int FormId { get; set; }

    // المعرف الخاص بالاجتماع (تمت إضافته لربط النموذج بجلسة محددة)
    public int? MeetingId { get; set; }

    public DateTime? MeetingStart { get; set; }

    public DateTime? MeetingEnd { get; set; }

    public string? MeetingPurpose { get; set; }

    public string? MeetingNotes { get; set; }

    public string? ReferralReason { get; set; }

    public string? ReferredTo { get; set; }

    public string? StudentActions { get; set; }

    public string? AdvisorActions { get; set; }

    // العلاقة الملاحية مع النموذج العام
    public virtual Form Form { get; set; } = null!;
}