using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budgeteer.Server.Features.Categories;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
    }
}
