using System.Collections.Generic;
using System.Threading.Tasks;

namespace PoweredSoft.Docker.MongoBackup.Notifications
{
    public interface INotifyService
    {
        Task SendNotification(string title, string message, Dictionary<string, string> facts = null, string color = null);
    }
}
