using CreditCardAnalyzer.Services;
using CreditCardAnalyzer.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<AnalysisOptions>(builder.Configuration);

// Discover CSV files in Data, omitting any configured exclusions.
builder.Services.AddSingleton(_ =>
    new TransactionService(
        TransactionService.DiscoverTransactionFiles(
            Path.Combine(builder.Environment.ContentRootPath, "Data"),
            builder.Configuration.GetSection("ExcludedTransactionFiles").Get<string[]>()))
);
builder.Services.AddScoped<AnalysisDataService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
