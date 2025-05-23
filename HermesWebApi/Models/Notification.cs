namespace HermesWebApi.Models
{
    public class Notification : Parameter
    {
        public string Details { get; set; }
        public int PlanID { get; set; }
        public int NotType { get; set; }
        public bool Seen { get; set; }
        public DateTime? SeenDate { get; set; }

    }
}
