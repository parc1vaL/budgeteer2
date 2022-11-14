using System.Reflection;
using Budgeteer.Server.Entities;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Server;

public class BudgetContext : DbContext
{
    public BudgetContext(DbContextOptions options) : base(options)
    { }
    
    public DbSet<Account> Accounts => Set<Account>();
    
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (modelBuilder is null) throw new ArgumentNullException(nameof(modelBuilder));

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}