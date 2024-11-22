using Microsoft.Extensions.Configuration.UserSecrets;

namespace HermesWebApi.Models
{
    public class User
    {
        public string? UserID { get; set; }
        public  string UserName { get; set; }
        public string UserCode { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string? Mobile { get; set; }
        public string CompanyID { get; set; }
        public string? CompanyName { get; set; }
        public string? ADAccount { get; set; }
        public bool Status { get; set; }
        public string RoleID { get; set; }
        public string? RoleName { get; set; }
        public string PositionID {  get; set; }
        public string? PositionName { get; set; }
        public List<UserFrameRight>? FrameRights { get; set; }

    }
    public class UserFrameRight
    {
        public int MenuID { get; set; }
        public required string MenuName { get; set; }
        public string Url { get; set; }
        public bool RNew { get; set; }
        public bool RUpd { get; set; }
        public bool RDel { get; set; }
    }
}
