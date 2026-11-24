using EventApi.Models.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventApi.DataAccess.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("bookings", "core");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id)
                .ValueGeneratedNever();

            builder.Property(b => b.EventId)
                .IsRequired();

            builder.Property(b => b.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(b => b.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            builder.HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
