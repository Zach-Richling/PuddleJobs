using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PuddleJobs.ApiService.Models;

namespace PuddleJobs.ApiService.Data;

public class JobSchedulerDbContext : DbContext
{
    public JobSchedulerDbContext(DbContextOptions<JobSchedulerDbContext> options) : base(options)
    {
    }

    public DbSet<Assembly> Assemblies { get; set; }
    public DbSet<AssemblyVersion> AssemblyVersions { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<JobSchedule> JobSchedules { get; set; }
    public DbSet<ExecutionLog> ExecutionLogs { get; set; }
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
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Assembly>()
            .HasMany(a => a.Jobs)
            .WithOne(j => j.Assembly)
            .HasForeignKey(j => j.AssemblyId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Job>()
            .HasMany(j => j.JobSchedules)
            .WithOne(js => js.Job)
            .HasForeignKey(js => js.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Job>()
            .HasMany(j => j.ExecutionLogs)
            .WithOne(el => el.Job)
            .HasForeignKey(el => el.JobId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Job>()
            .HasMany(j => j.Parameters)
            .WithOne(p => p.Job)
            .HasForeignKey(p => p.JobId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssemblyVersion>()
            .HasMany(av => av.ParameterDefinitions)
            .WithOne(pd => pd.AssemblyVersion)
            .HasForeignKey(pd => pd.AssemblyVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Schedule>()
            .HasMany(s => s.JobSchedules)
            .WithOne(js => js.Schedule)
            .HasForeignKey(js => js.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExecutionLog>()
            .HasOne(el => el.Schedule)
            .WithMany()
            .HasForeignKey(el => el.ScheduleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure indexes
        modelBuilder.Entity<Assembly>()
            .HasIndex(a => a.Name)
            .IsUnique();

        modelBuilder.Entity<AssemblyVersion>()
            .HasIndex(av => new { av.AssemblyId, av.Version })
            .IsUnique();

        modelBuilder.Entity<AssemblyVersion>()
            .HasIndex(av => new { av.AssemblyId, av.IsActive })
            .IsUnique()
            .HasFilter("[IsActive] = 1"); // Only one active version per assembly

        modelBuilder.Entity<Job>()
            .HasIndex(j => j.Name)
            .IsUnique();

        modelBuilder.Entity<Schedule>()
            .HasIndex(s => s.Name)
            .IsUnique();

        modelBuilder.Entity<ExecutionLog>()
            .HasIndex(el => el.StartTime);

        modelBuilder.Entity<ExecutionLog>()
            .HasIndex(el => new { el.JobId, el.StartTime });

        modelBuilder.Entity<JobParameter>()
            .HasIndex(p => new { p.JobId, p.Name })
            .IsUnique();

        modelBuilder.Entity<AssemblyParameterDefinition>()
            .HasIndex(pd => new { pd.AssemblyVersionId, pd.Name })
            .IsUnique();
    }
} 