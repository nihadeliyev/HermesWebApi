namespace HermesWebApi.Models
{
    public class TrainingPlan : Parameter
    {
        public int PlanID { get; set; }
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
        public int Status { get; set; }
        public int? ProgramID { get; set; }
        public string ProgramName { get; set; }
        public int? PlannedParticipantCount { get; set; }
        public int? ActualParticipantCount { get; set; }
        public int Days { get { return Dates.Count; } }
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
}
