using Microsoft.EntityFrameworkCore;
using FitnessFormula.Models;

namespace FitnessFormula.Data
{
    public class FitnessDbContext : DbContext
    {
        public FitnessDbContext(DbContextOptions<FitnessDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<TrainerSkills> TrainerSkills { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Workout> Workouts { get; set; }
        public DbSet<WorkoutRegistration> WorkoutRegistrations { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TrainerSkills>()
                .HasKey(ts => new { ts.TrainerId, ts.SkillId }); // Определение составного ключа

            base.OnModelCreating(modelBuilder);
        }
    }
}
