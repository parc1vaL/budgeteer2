using Budgeteer.Server.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budgeteer.Server.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.HasKey(e => new { e.CategoryId, e.Month });
        builder.HasOne(e => e.Category).WithMany(e => e.Budgets).IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.Property(e => e.Month).IsRequired().HasColumnType("date");
        builder.Property(e => e.Amount).IsRequired();
    }
}
