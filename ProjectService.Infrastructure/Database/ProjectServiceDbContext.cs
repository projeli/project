using System.ComponentModel;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using ProjectService.Domain.Models;
using ProjectService.Infrastructure.Converters;

namespace ProjectService.Infrastructure.Database;

public class ProjectServiceDbContext(DbContextOptions<ProjectServiceDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<ProjectLink> ProjectLinks { get; set; }
    
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Ulid>()
            .HaveConversion<UlidToStringConverter>()
            .HaveConversion<UlidToGuidConverter>();
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var memberInfo = property.PropertyInfo ?? (MemberInfo?)property.FieldInfo;
                if (memberInfo == null) continue;
                var defaultValue =
                    Attribute.GetCustomAttribute(memberInfo, typeof(DefaultValueAttribute)) as DefaultValueAttribute;
                if (defaultValue == null) continue;
                property.SetDefaultValue(defaultValue.Value);
            }
        }

        builder.ApplyConfigurationsFromAssembly(typeof(ProjectServiceDbContext).Assembly);
        
        builder.Entity<ProjectMember>()
            .HasOne(x => x.Project)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<ProjectLink>()
            .HasOne(x => x.Project)
            .WithMany(x => x.Links)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}