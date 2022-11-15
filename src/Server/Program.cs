using Budgeteer.Server.Endpoints;
using Budgeteer.Server;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;
using Budgeteer.Server.Logic.Accounts;
using Budgeteer.Server.Logic.Transactions;
using Microsoft.OpenApi.Models;
using Budgeteer.Server.Logic.Categories;
using Budgeteer.Server.Logic.Budgets;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration().CreateBootstrapLogger();

// Add services to the container.
builder.Services.AddTransient<AccountService>();
builder.Services.AddTransient<CategoryService>();
builder.Services.AddTransient<TransactionService>();
builder.Services.AddTransient<BudgetService>();

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = builder.Environment.ApplicationName, Version = "v1" });

    c.MapType<DateOnly>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "date"
    });
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDbContext<BudgetContext>(settings => 
{
    settings
        .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention();
});

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{builder.Environment.ApplicationName} v1"));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

app.UseSerilogRequestLogging();

app.MapAccounts();
app.MapCategories();
app.MapTransactions();
app.MapBudgets();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
