<<<<<<< HEAD
﻿using Acadify.Models;
using Acadify.Services.AcademicCalendar;
using Acadify.Services.AcademicCalendar.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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

builder.Services.AddDbContext<AcadifyDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("AcadifyDb"),
        sql =>
        {
            sql.CommandTimeout(120);
            sql.EnableRetryOnFailure();
        }));

builder.Services.AddScoped<OpenAiVisionClient>();
builder.Services.AddScoped<IAcademicCalendarAiExtractor, AcademicCalendarFixedExtractor>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
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
