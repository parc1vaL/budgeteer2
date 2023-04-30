using System.Reflection;
using Budgeteer.Server;
using Budgeteer.Server.Features.Accounts;
using Budgeteer.Server.Features.Budgets;
using Budgeteer.Server.Features.Categories;
using Budgeteer.Server.Features.Transactions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = builder.Environment.ApplicationName, Version = "v1" });

    c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddDbContext<BudgetContext>(settings =>
{
    settings
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .EnableDetailedErrors(builder.Environment.IsDevelopment())
        .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention();
});

builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.yaml", $"{builder.Environment.ApplicationName} v1"));

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
app.UseRouting();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(application => application.Run(context => Results.Problem().ExecuteAsync(context)));
}

app.MapAccounts();
app.MapCategories();
app.MapTransactions();
app.MapBudgets();

app.MapFallbackToFile("index.html");

app.Run();
