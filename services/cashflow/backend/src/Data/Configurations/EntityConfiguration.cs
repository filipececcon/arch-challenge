using ArchChallenge.CashFlow.Domain.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchChallenge.CashFlow.Data.Configurations;

public abstract class EntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : Entity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder
            .Property(x => x.Id)
            .HasColumnName("ID")
            .HasColumnOrder(0)
            .ValueGeneratedOnAdd();

        builder
            .Property(x => x.CreatedAt)
            .HasColumnName("DT_CREATED_AT")
            .IsRequired();

        builder
            .Property(x => x.UpdatedAt)
            .HasColumnName("DT_UPDATED_AT");

        builder
            .Property(x => x.Active)
            .HasColumnName("ST_ACTIVE")
            .IsRequired();
        
        builder.Ignore(t => t.Notifications);
    }
}
