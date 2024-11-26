namespace HermesWebApi.Models
{
    public class Training : Parameter
    {
        public string TrainingCode { get; set; }
        public int CategoryID { get; set; }
        public string? CategoryName { get; set; }
        public int TypeID { get; set; }
        public string? TypeName { get; set; }
        public int? ExpireDays { get; set; }
        public bool? Certificate { get; set; }
        public float? Passpoint { get; set; }
        public string? Notes { get; set; }
        public bool? Status { get; set; }

    }
}
