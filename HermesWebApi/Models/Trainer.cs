namespace HermesWebApi.Models
{
    public class Trainer:Parameter
    {       
        public string MailAddress { get; set; }
        public string? Notes { get; set; }
        public string? MobileNumber { get; set; }
        public List<Profession> Professions { get; set;} = new List<Profession>();
    }
}
