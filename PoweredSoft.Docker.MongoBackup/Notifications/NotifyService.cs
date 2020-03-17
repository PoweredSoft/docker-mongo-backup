using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MongoBackup.Notifications
{
    public class NotifyService : INotifyService
    {
        private readonly IEnumerable<INotificationService> notificationServices;

        public NotifyService(IEnumerable<INotificationService> notificationServices)
        {
            this.notificationServices = notificationServices;
        }

        public async Task SendNotification(string title, string message, Dictionary<string, string> facts = null, string color = null)
        {
            Console.WriteLine($"---- Notification Start ---");
            Console.WriteLine($"title: {title}");
            Console.WriteLine(message);
            Console.WriteLine($"Facts:");
            
            if (facts != null)
            {
                foreach(var fact in facts)
                    Console.WriteLine($"{fact.Key}: {fact.Value}");
            }
            Console.WriteLine($"---- Notification End ---");

            foreach(var notificationService in notificationServices)
                await notificationService.SendNotification(title, message, facts, color);
        }
    }
}
