using WebApplication1.Models.Core;

namespace WebApplication1.Models.Services
{
    public class NotificationService
    {
        // 通知新成員 (模擬發送通知)
        public bool NotifyNewMember(Student student, object credentials)
        {
            Console.WriteLine($"通知已發送給: {student.Email}");
            Console.WriteLine($"內容: 您已被加入實驗室。您的初始密碼是: {student.Password}");
            return true;
        }

        // 發送憑證
        public bool SendCredentials(object credentials)
        {
            // 模擬發送憑證
            Console.WriteLine("憑證已發送");
            return true;
        }

        // 其他通知方法...
    }
}
