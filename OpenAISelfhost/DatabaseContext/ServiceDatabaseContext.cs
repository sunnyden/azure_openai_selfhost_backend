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
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<ChatModel> ChatModels { get; set; }
        public DbSet<UserModelAssignment> UserModelAssignments { get; set; }
    }
}
