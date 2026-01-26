using Microsoft.EntityFrameworkCore;
using OpenAISelfhost.DataContracts.DataTables;

namespace OpenAISelfhost.DatabaseContext
{
    public class ServiceDatabaseContext : DbContext
    {
        public ServiceDatabaseContext(DbContextOptions<ServiceDatabaseContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserModelAssignment>()
                .HasKey(uma => new { uma.UserId, uma.ModelIdentifier });

            modelBuilder.Entity<ChatHistory>()
                .HasKey(ch => new { ch.Id, ch.UserId });
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<ChatModel> ChatModels { get; set; }
        public DbSet<UserModelAssignment> UserModelAssignments { get; set; }
        public DbSet<ChatHistory> ChatHistories { get; set; }
    }
}
