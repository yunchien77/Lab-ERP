namespace WebApplication1
{
    public class StudentProfileDto
    {
        public string StudentID { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class LaboratoryCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Website { get; set; }
        public string ContactInfo { get; set; }
    }

    public class LaboratoryUpdateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Website { get; set; }
        public string ContactInfo { get; set; }
    }

    public class MemberCreateDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
    }

    public class CreateEquipmentDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int TotalQuantity { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string LaboratoryID { get; set; }
    }

    public class BorrowEquipmentDto
    {
        public string EquipmentID { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; }
    }
}
