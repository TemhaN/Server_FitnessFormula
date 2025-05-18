using Microsoft.EntityFrameworkCore;
using FitnessFormula.Models;

namespace FitnessFormula.Data
{
    public class FitnessDbContext : DbContext
    {
        public FitnessDbContext(DbContextOptions<FitnessDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Workout> Workouts { get; set; }
        public DbSet<WorkoutRegistration> WorkoutRegistrations { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<TrainerSkills> TrainerSkills { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Gym> Gyms { get; set; }
        public DbSet<WorkoutComment> WorkoutComments { get; set; }
        public DbSet<WorkoutAttendance> WorkoutAttendance { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserInterest> UserInterests { get; set; }
        public DbSet<WeeklyChallenge> WeeklyChallenges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<WeeklyChallenge>()
                .HasKey(wc => new { wc.UserId, wc.WeekNumber, wc.Year });

            modelBuilder.Entity<WeeklyChallenge>()
                .Property(wc => wc.UserId)
                .HasColumnName("UserId");

            modelBuilder.Entity<WeeklyChallenge>()
                .Property(wc => wc.WorkoutId)
                .HasColumnName("WorkoutId");

            modelBuilder.Entity<WeeklyChallenge>()
                .Property(wc => wc.WeekNumber)
                .HasColumnName("weeknumber");

            modelBuilder.Entity<WeeklyChallenge>()
                .Property(wc => wc.Year)
                .HasColumnName("year");

            modelBuilder.Entity<WeeklyChallenge>()
                .HasOne(wc => wc.User)
                .WithMany()
                .HasForeignKey(wc => wc.UserId)
                .HasConstraintName("fk_weeklychallenges_users_userid");

            modelBuilder.Entity<WeeklyChallenge>()
                .HasOne(wc => wc.Workout)
                .WithMany()
                .HasForeignKey(wc => wc.WorkoutId)
                .HasConstraintName("fk_weeklychallenges_workouts_workoutid");

            // Workout
            modelBuilder.Entity<Workout>()
                .HasOne(w => w.Gym)
                .WithMany(g => g.Workouts)
                .HasForeignKey(w => w.GymId);

            modelBuilder.Entity<Workout>()
                .Property(w => w.GymId)
                .HasColumnName("GymId");

            modelBuilder.Entity<Workout>()
                .Property(w => w.MaxParticipants)
                .HasColumnName("maxparticipants"); // Маппинг на столбец maxparticipants

            modelBuilder.Entity<WorkoutAttendance>()
                .ToTable("workoutattendance");
            modelBuilder.Entity<WorkoutAttendance>()
                .Property(wa => wa.AttendanceId)
                .HasColumnName("attendanceid");

            modelBuilder.Entity<WorkoutAttendance>()
                .Property(wa => wa.WorkoutId)
                .HasColumnName("workoutid");

            modelBuilder.Entity<WorkoutAttendance>()
                .Property(wa => wa.UserId)
                .HasColumnName("userid");

            modelBuilder.Entity<WorkoutAttendance>()
                .Property(wa => wa.AttendanceDate)
                .HasColumnName("attendancedate");

            modelBuilder.Entity<UserInterest>()
                .ToTable("userinterests"); // Маппинг на таблицу userinterests

            modelBuilder.Entity<UserInterest>()
                .Property(ui => ui.UserId)
                .HasColumnName("userid");

            modelBuilder.Entity<UserInterest>()
                .Property(ui => ui.SkillId)
                .HasColumnName("skillid");

            // WorkoutComment
            modelBuilder.Entity<WorkoutComment>()
                .ToTable("workoutcomments");

            modelBuilder.Entity<WorkoutComment>()
                .Property(wc => wc.CommentId)
                .HasColumnName("commentid")
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<WorkoutComment>()
                .Property(wc => wc.WorkoutId)
                .HasColumnName("workoutid");

            modelBuilder.Entity<WorkoutComment>()
                .Property(wc => wc.UserId)
                .HasColumnName("userid");

            modelBuilder.Entity<WorkoutComment>()
                .Property(wc => wc.CommentText)
                .HasColumnName("commenttext");

            modelBuilder.Entity<WorkoutComment>()
                .Property(wc => wc.CommentDate)
                .HasColumnName("commentdate");

            modelBuilder.Entity<WorkoutComment>()
                .HasOne(wc => wc.Workout)
                .WithMany(w => w.WorkoutComments)
                .HasForeignKey(wc => wc.WorkoutId);

            modelBuilder.Entity<WorkoutComment>()
                .HasOne(wc => wc.User)
                .WithMany()
                .HasForeignKey(wc => wc.UserId);

            // WorkoutAttendance
            modelBuilder.Entity<WorkoutAttendance>()
                .HasOne(wa => wa.Workout)
                .WithMany(w => w.WorkoutAttendances)
                .HasForeignKey(wa => wa.WorkoutId);

            modelBuilder.Entity<WorkoutAttendance>()
                .HasOne(wa => wa.User)
                .WithMany()
                .HasForeignKey(wa => wa.UserId);

            modelBuilder.Entity<WorkoutAttendance>()
                .HasIndex(wa => new { wa.WorkoutId, wa.UserId })
                .IsUnique();

            // Notification

            modelBuilder.Entity<Notification>()
                .ToTable("notifications");

            modelBuilder.Entity<Notification>()
                .Property(n => n.NotificationId)
                .HasColumnName("notificationid");

            modelBuilder.Entity<Notification>()
                .Property(n => n.UserId)
                .HasColumnName("userid");

            modelBuilder.Entity<Notification>()
                .Property(n => n.Title)
                .HasColumnName("title");

            modelBuilder.Entity<Notification>()
                .Property(n => n.Message)
                .HasColumnName("message");

            modelBuilder.Entity<Notification>()
                .Property(n => n.NotificationType)
                .HasColumnName("notificationtype");

            modelBuilder.Entity<Notification>()
                .Property(n => n.SentAt)
                .HasColumnName("sentat");

            modelBuilder.Entity<Notification>()
                .Property(n => n.IsRead)
                .HasColumnName("isread");

            modelBuilder.Entity<Notification>()
                .Property(n => n.WorkoutId)
                .HasColumnName("workoutid");


            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Workout)
                .WithMany()
                .HasForeignKey(n => n.WorkoutId);

            // UserInterest
            modelBuilder.Entity<UserInterest>()
                .HasKey(ui => new { ui.UserId, ui.SkillId });

            modelBuilder.Entity<UserInterest>()
                .HasOne(ui => ui.User)
                .WithMany()
                .HasForeignKey(ui => ui.UserId);

            modelBuilder.Entity<UserInterest>()
                .HasOne(ui => ui.Skill)
                .WithMany()
                .HasForeignKey(ui => ui.SkillId);

            // Review
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Trainer)
                .WithMany(t => t.Reviews)
                .HasForeignKey(r => r.TrainerId);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId);

            // TrainerSkills
            modelBuilder.Entity<TrainerSkills>()
                .HasKey(ts => new { ts.TrainerId, ts.SkillId });

            modelBuilder.Entity<TrainerSkills>()
                .HasOne(ts => ts.Trainer)
                .WithMany(t => t.TrainerSkills)
                .HasForeignKey(ts => ts.TrainerId);

            modelBuilder.Entity<TrainerSkills>()
                .HasOne(ts => ts.Skill)
                .WithMany()
                .HasForeignKey(ts => ts.SkillId);

            // Gym
            modelBuilder.Entity<Gym>()
                .ToTable("gyms");

            modelBuilder.Entity<Gym>()
                .Property(g => g.GymId)
                .HasColumnName("gymid")
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Gym>()
                .Property(g => g.GymName)
                .HasColumnName("gymname");

            modelBuilder.Entity<Gym>()
                .Property(g => g.Address)
                .HasColumnName("address");

        }
    }
}