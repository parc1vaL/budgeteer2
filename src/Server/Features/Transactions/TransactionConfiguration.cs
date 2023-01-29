using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Budgeteer.Server.Features.Transactions
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.HasKey(e => e.Id);
            builder.HasIndex(e => e.Payee);

            builder.HasOne(e => e.Account).WithMany(e => e.Transactions).IsRequired().OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(e => e.Category).WithMany(e => e.Transactions).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(e => e.TransferTransaction).WithOne().OnDelete(DeleteBehavior.Cascade);

            builder.Property(e => e.Date).HasColumnType("date");
            builder.Property(e => e.Payee).HasMaxLength(200);
            builder.Property(e => e.TransactionType).IsRequired();
            builder.Property(e => e.IncomeType).IsRequired();
            builder.Property(e => e.IsCleared).IsRequired();
            builder.Property(e => e.Amount).IsRequired();
        }
    }
}
