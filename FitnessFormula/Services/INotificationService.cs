using FitnessFormula.Data;
using FitnessFormula.Models;

namespace FitnessFormula.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(int userId, string title, string message, string notificationType, int? workoutId = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly FitnessDbContext _context;

        public NotificationService(FitnessDbContext context)
        {
            _context = context;
        }

        public async Task SendNotificationAsync(int userId, string title, string message, string notificationType, int? workoutId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                NotificationType = notificationType,
                WorkoutId = workoutId,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Здесь можно добавить отправку push-уведомления через FCM, OneSignal или другой сервис
            // Например: await _fcmClient.SendAsync(user.DeviceToken, title, message);
        }
    }
}