namespace HermesWebApi.Models
{
    public class TrainingPlan : Parameter
    {
        //public int PlanID { get; set; }
        public int TrainingID { get; set; }
        public string TrainingName { get; set; }
        public int RoomID { get; set; }
        public string RoomName { get; set; }
        public int TrainerID { get; set; }
        public string TrainerName { get; set; }
        public int? TrainerID2 { get; set; }
        public string TrainerName2 { get; set; }
        public int? TrainerID3 { get; set; }
        public string TrainerName3 { get; set; }
        public DateTime Date { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<DateTime> Dates { get; set; }
        public string Organizer { get; set; }
        public string Notes { get; set; }
        public string Organizator { get; set; }
        public int StatusID { get; set; }
        public string StatusName { get; set; }
        public int? ProgramID { get; set; }
        public string ProgramName { get; set; }
        public int MaxCapacity { get; set; }
        public int? PlannedParticipantCount { get; set; }
        public int? ActualParticipantCount { get; set; }
        public int Days { get { return Dates.Count; } }
        public List<TrainingQuota> Quotas { get; set; } = new List<TrainingQuota>();
        public List<ParticipantInfo> PlannedParticipants { get; set; } = new List<ParticipantInfo>();
        public List<ParticipatedInfo> Participants { get; set; } = new List<ParticipatedInfo>();
        public double Hours
        {
            get
            {
                return (EndTime - StartTime).TotalMinutes * Days / 60;
            }
        }
        public double HoursPerDay
        {
            get
            {
                return (EndTime - StartTime).TotalMinutes / 60;
            }
        }
    }
    public class TrainingQuota
    {
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public int? MaxParticipant { get; set; }
    }
    public class ParticipatedInfo : ParticipantInfo
    {
        public DateTime Date { get; set; }
    }
    public class ParticipantInfo
    {
        public int EmpID { get; set; }
        public string EmpName { get; set; }
        public string Company { get; set; }
    }
}
