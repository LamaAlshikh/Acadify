<<<<<<< HEAD
<<<<<<< HEAD
<<<<<<< HEAD
=======
>>>>>>> origin_second/لما2
﻿using Acadify.Models;
using Acadify.Services.AcademicCalendar;
using Acadify.Services.AcademicCalendar.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

<<<<<<< HEAD
=======
using Acadify.Models.Db;
using Microsoft.EntityFrameworkCore;
using Acadify.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddHttpClient<AiSummaryService>();
builder.Services.AddScoped<ITranscriptParserService, TranscriptParserService>();
builder.Services.AddScoped<IRecommendationEngineService, RecommendationEngineService>();
builder.Services.AddScoped<ITranscriptAiParserService, TranscriptAiParserService>();


// Add services to the container.
>>>>>>> origin_second/rahafgh
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.AddDistributedMemoryCache();
=======
﻿using Acadify.Models.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Acadify.Services;



var builder = WebApplication.CreateBuilder(args);

// MVC
=======
>>>>>>> origin_second/لما2
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Session
builder.Services.AddDistributedMemoryCache();

>>>>>>> origin_second/linaLMversion
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

<<<<<<< HEAD
// Register DbContext
builder.Services.AddDbContext<Acadify.Models.Db.AcadifyDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            sql.CommandTimeout(120);
            sql.EnableRetryOnFailure();
        }));



builder.Services.AddScoped<AiSummaryService>();

builder.Services.AddHttpClient();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

=======


// ✅ Register DbContext
>>>>>>> origin_second/linaLMversion
builder.Services.AddDbContext<AcadifyDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("AcadifyDb"),
        sql =>
        {
            sql.CommandTimeout(120);
            sql.EnableRetryOnFailure();
        }));

<<<<<<< HEAD
<<<<<<< HEAD
builder.Services.AddScoped<OpenAiVisionClient>();
builder.Services.AddScoped<IAcademicCalendarAiExtractor, AcademicCalendarFixedExtractor>();
=======
builder.Services.AddHttpClient<AiSummaryService>();



>>>>>>> origin_second/linaLMversion
=======
builder.Services.AddScoped<IPdfOcrService, PdfOcrService>();
builder.Services.AddScoped<IPdfTextExtractor, PdfPigTextExtractor>();
builder.Services.AddScoped<OpenAiVisionClient>();
builder.Services.AddScoped<IAcademicCalendarAiExtractor, AcademicCalendarAiExtractor>();
>>>>>>> origin_second/لما2

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
<<<<<<< HEAD

app.UseRouting();
<<<<<<< HEAD
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
<<<<<<< HEAD
    pattern: "{controller=Home}/{action=Welcome}/{id?}");

app.Run();
=======
    pattern: "{controller=Welcome}/{action=Index}/{id?}");

app.Run();

>>>>>>> origin_second/rahafgh
=======
=======

app.UseRouting();
>>>>>>> origin_second/لما2

app.UseSession();

app.UseAuthorization();
<<<<<<< HEAD


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Welcome}/{id?}");



app.Run();
>>>>>>> origin_second/linaLMversion
=======

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
>>>>>>> origin_second/لما2
