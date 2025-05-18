using FitnessFormula.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FitnessFormula.Services
{
    public class WorkoutReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public WorkoutReminderService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<FitnessDbContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    var now = DateTime.UtcNow;
                    var reminderTime = now.AddHours(1);

                    var upcomingWorkouts = await context.Workouts
                        .Where(w => w.StartTime >= now && w.StartTime <= reminderTime)
                        .Include(w => w.WorkoutRegistrations)
                        .ThenInclude(wr => wr.User)
                        .ToListAsync();

                    foreach (var workout in upcomingWorkouts)
                    {
                        foreach (var registration in workout.WorkoutRegistrations)
                        {
                            await notificationService.SendNotificationAsync(
                                registration.UserId,
                                "Напоминание о тренировке",
                                $"Ваша тренировка '{workout.Title}' начнётся через час в {workout.StartTime:HH:mm}.",
                                "Reminder",
                                workout.WorkoutId
                            );
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Проверять каждые 5 минут
            }
        }
    }
}