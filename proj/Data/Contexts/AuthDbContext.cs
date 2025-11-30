using Microsoft.EntityFrameworkCore;
using University.Data.Models;

namespace University.Data.Contexts;

public class AuthDbContext : DbContext
{
    public DbSet<Student> Students { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<StudentCourse> StudentCourses { get; set; }

    public AuthDbContext(DbContextOptions<AuthDbContext> ops) : base(ops)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudentCourse>()
            .HasKey(s=>s.StudentId);
        modelBuilder.Entity<StudentCourse>()
            .HasKey(s=>s.CourseId);
        modelBuilder.Entity<Student>()
            .HasKey(s=>s.Id);
        modelBuilder.Entity<Student>()
            .Property(s => s.Name)
            .HasColumnType("nvarchar(50)")
            .IsRequired();
        modelBuilder.Entity<Student>()
            .Property(s => s.Email)
            .HasColumnType("nvarchar(50)")
            .IsRequired();
        modelBuilder.Entity<Student>()
            .Property(s => s.BirthDate)
            .HasColumnType("DateTime")
            .IsRequired();
    }
    
}