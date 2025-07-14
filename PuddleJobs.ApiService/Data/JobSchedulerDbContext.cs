using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PuddleJobs.ApiService.Models;

namespace PuddleJobs.ApiService.Data;

public class JobSchedulerDbContext(DbContextOptions<JobSchedulerDbContext> options) : DbContext(options)
{
    public DbSet<Assembly> Assemblies { get; set; }
    public DbSet<AssemblyVersion> AssemblyVersions { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<JobSchedule> JobSchedules { get; set; }
    public DbSet<JobParameter> JobParameters { get; set; }
    public DbSet<AssemblyParameterDefinition> AssemblyParameterDefinitions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global query filters for soft delete
        modelBuilder.Entity<Assembly>().HasQueryFilter(a => !a.IsDeleted);
        modelBuilder.Entity<AssemblyVersion>().HasQueryFilter(av => !av.IsDeleted);
        modelBuilder.Entity<Job>().HasQueryFilter(j => !j.IsDeleted);
        modelBuilder.Entity<Schedule>().HasQueryFilter(s => !s.IsDeleted);
        
        // Configure relationships
        modelBuilder.Entity<Assembly>()
            .HasMany(a => a.Versions)
            .WithOne(v => v.Assembly)
            .HasForeignKey(v => v.AssemblyId)
            .IsRequired();

        modelBuilder.Entity<Assembly>()
            .HasMany(a => a.Jobs)
            .WithOne(j => j.Assembly)
            .HasForeignKey(j => j.AssemblyId)
            .IsRequired();

        modelBuilder.Entity<Job>()
            .HasMany(j => j.JobSchedules)
            .WithOne(js => js.Job)
            .HasForeignKey(js => js.JobId);

        modelBuilder.Entity<Job>()
            .HasMany(j => j.Parameters)
            .WithOne(p => p.Job)
            .HasForeignKey(p => p.JobId)
            .IsRequired(false);

        modelBuilder.Entity<AssemblyVersion>()
            .HasMany(av => av.ParameterDefinitions)
            .WithOne(pd => pd.AssemblyVersion)
            .HasForeignKey(pd => pd.AssemblyVersionId);

        modelBuilder.Entity<Schedule>()
            .HasMany(s => s.JobSchedules)
            .WithOne(js => js.Schedule)
            .HasForeignKey(js => js.ScheduleId);

        // Configure indexes
        modelBuilder.Entity<Assembly>()
            .HasIndex(a => a.Name)
            .IsUnique();

        modelBuilder.Entity<AssemblyVersion>()
            .HasIndex(av => new { av.AssemblyId, av.Version })
            .IsUnique();

        modelBuilder.Entity<Job>()
            .HasIndex(j => j.Name)
            .IsUnique();

        modelBuilder.Entity<Schedule>()
            .HasIndex(s => s.Name)
            .IsUnique();

        modelBuilder.Entity<JobParameter>()
            .HasIndex(p => new { p.JobId, p.Name })
            .IsUnique();

        modelBuilder.Entity<AssemblyParameterDefinition>()
            .HasIndex(pd => new { pd.AssemblyVersionId, pd.Name })
            .IsUnique();
    }
} 