using CompetenceProfilingDomain.Contracts.Infrastructure;
using CompetenceProfilingDomain.Contracts.ModelsDatabase;
using Microsoft.EntityFrameworkCore;

namespace CompetenceProfilingInfrastructure.Data
{


    public class DatabaseContext : DbContext, IRepository
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {
#if DEBUG
            base.Database.Migrate();
#endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TreeRootCanvasDto>().HasKey(k => k.Id);
            modelBuilder.Entity<OutcomesCanvasDto>().HasKey(k => k.LmsId);


            //https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many
            modelBuilder.Entity<TreeRootCanvasDto>()
                .HasMany(e => e.OutcomesCanvas)
                .WithMany(e => e.TreeRootsCanvas)
                .UsingEntity(
                    "TreeRootOutcome",
                    l => l.HasOne(typeof(OutcomesCanvasDto)).WithMany().HasForeignKey("OutcomesLmsId")
                        .HasPrincipalKey(nameof(OutcomesCanvasDto.LmsId)),
                    r => r.HasOne(typeof(TreeRootCanvasDto)).WithMany().HasForeignKey("TreeRootId")
                        .HasPrincipalKey(nameof(TreeRootCanvasDto.Id)),
                    j => j.HasKey("OutcomesLmsId", "TreeRootId"));
        }

        public DbSet<StudentAdviceDto> StudentAdvices { get; set; } = null!;
        public DbSet<StudentKpiDto> StudentKpi { get; set; } = null!;

        public DbSet<TreeRootCanvasDto> TreeRootCanvas { get; set; }
        public DbSet<OutcomesCanvasDto> OutcomesCanvas { get; set; }



        void IRepository.Add<TEntity>(TEntity entity)
        {
            Add<TEntity>(entity);
        }

        void IRepository.Update<TEntity>(TEntity entity)
        {
            Update<TEntity>(entity);
        }

        void IRepository.Remove<TEntity>(TEntity entity)
        {
            Remove<TEntity>(entity);
        }

        IQueryable<TEntity> IRepository.Query<TEntity>()
        {
            return Set<TEntity>();
        }

        TEntity IRepository.GetById<TEntity, TId>(TId id)
        {
            var entity = Set<TEntity>().Find(id);
            if (entity == null)
                throw new InvalidOperationException($"No Entity {typeof(TEntity).Name} found for id {id}");
            return entity;
        }

        int IRepository.SaveChanges()
        {
            return SaveChanges();
        }
    }
}
