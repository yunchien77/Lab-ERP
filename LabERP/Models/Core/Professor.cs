namespace LabERP.Models.Core
{
    public class Professor : User
    {
        public List<Laboratory> Laboratories { get; set; }

        public Professor()
        {
            Role = "Professor";
            Laboratories = new List<Laboratory>();
        }
    }
}
