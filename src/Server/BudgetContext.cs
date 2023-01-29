using System.Reflection;
using Budgeteer.Server.Features.Accounts;
using Budgeteer.Server.Features.Budgets;
using Budgeteer.Server.Features.Categories;
using Budgeteer.Server.Features.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server;

public class BudgetContext : DbContext
{
    public BudgetContext(DbContextOptions options) : base(options)
    { }
    
    public DbSet<Account> Accounts => Set<Account>();
    
    public DbSet<Category> Categories => Set<Category>();
    
    public DbSet<Transaction> Transactions => Set<Transaction>();
    
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (modelBuilder is null) throw new ArgumentNullException(nameof(modelBuilder));

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}