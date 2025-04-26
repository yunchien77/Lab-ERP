namespace WebApplication1.Models.Core
{
    public class Professor : User
    {
        public List<Laboratory> Laboratories { get; set; }

        public Professor()
        {
            Role = "Professor";
            Laboratories = new List<Laboratory>();
        }

        // 創建新實驗室
        public Laboratory CreateLaboratory(object labInfo)
        {
            // 從 labInfo 解包實驗室資訊
            string name = (labInfo as dynamic)?.Name;
            string description = (labInfo as dynamic)?.Description;

            // 建立新實驗室
            var laboratory = new Laboratory
            {
                Name = name,
                Description = description,
                Creator = this
            };

            // 將實驗室添加到教授的實驗室列表
            Laboratories.Add(laboratory);

            return laboratory;
        }

        // 添加成員到實驗室
        public bool AddMember(string labID, object memberInfo)
        {
            // 查找實驗室
            var laboratory = Laboratories.Find(lab => lab.LabID == labID);
            if (laboratory == null)
                return false;

            // 從 memberInfo 解包成員資訊
            string username = (memberInfo as dynamic)?.Username;
            string email = (memberInfo as dynamic)?.Email;

            // 建立新學生
            var student = new Student
            {
                Username = username,
                Email = email,
                // 生成隨機密碼
                Password = GenerateRandomPassword()
            };

            // 將學生添加到實驗室
            return laboratory.AddMember(student);
        }

        // 生成隨機密碼
        private string GenerateRandomPassword()
        {
            // 簡單的隨機密碼生成
            return Guid.NewGuid().ToString().Substring(0, 8);
        }
    }
}
