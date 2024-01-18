using System.Reflection;
using Budgeteer.Server.Features.Accounts;
using Budgeteer.Server.Features.Budgets;
using Budgeteer.Server.Features.Categories;
using Budgeteer.Server.Features.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server;

public class BudgetContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    
    public DbSet<Category> Categories => Set<Category>();
    
    public DbSet<Transaction> Transactions => Set<Transaction>();
    
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
