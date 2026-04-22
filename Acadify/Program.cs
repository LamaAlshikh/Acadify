using Acadify.Models.Db;
using Microsoft.EntityFrameworkCore;
using Acadify.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddHttpClient<AiSummaryService>();
builder.Services.AddScoped<ITranscriptParserService, TranscriptParserService>();
builder.Services.AddScoped<IRecommendationEngineService, RecommendationEngineService>();
builder.Services.AddScoped<ITranscriptAiParserService, TranscriptAiParserService>();


// Add services to the container.
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

var app = builder.Build();

// Configure the HTTP request pipeline.
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
    pattern: "{controller=Welcome}/{action=Index}/{id?}");

app.Run();

