using Stockroom.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var databasePath = builder.Configuration["DATABASE"]
    ?? builder.Configuration["DatabasePath"]
    ?? "inventory.db";
builder.Services.AddSingleton(new StockroomDatabase(databasePath));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

await app.Services.GetRequiredService<StockroomDatabase>().InitializeAsync();

app.Run();

public partial class Program;
