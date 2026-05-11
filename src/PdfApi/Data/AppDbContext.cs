using Microsoft.EntityFrameworkCore;
using PdfApi.Models;

namespace PdfApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Document> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Document>()
                .Property(d => d.Status)
                .HasMaxLength(50);
        }
    }
}
