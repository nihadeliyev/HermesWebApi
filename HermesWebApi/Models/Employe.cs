namespace HermesWebApi.Models
{
    public class Employe : Parameter
    {
        public string FatherName { get; set; }
        public DateTime? BirthDate { get; set; }
        public int Education { get; set; }
        public string? EducationName { get; set; }
        public string? Notes { get; set; }
        public string PhoneNumber { get; set; }
        public string EmailAddress { get; set; }
        public int CompanyID { get; set; }
        public string? CompanyName { get; set; }
        public int DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public int RoleID { get; set; }
        public string? RoleName { get; set; }
        public int Gender { get; set; }
        public string? CurrentRole { get; set; }
    }
}
