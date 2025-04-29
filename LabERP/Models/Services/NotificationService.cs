using LabERP.Interface;
using LabERP.Models.Core;

public class NotificationService : INotificationService
{
    public bool NotifyNewMember(Student student, object credentials)
    {
        Console.WriteLine($"通知已發送給: {student.Email}");
        Console.WriteLine($"內容: 您已被加入實驗室。您的初始密碼是: {student.Password}");
        return true;
    }

    public bool SendCredentials(object credentials)
    {
        // 模擬發送憑證
        Console.WriteLine("憑證已發送");
        return true;
    }
}