namespace HermesWebApi.Models
{
    public class MainReport
    {
        public List<CategoryStatistic> CategoryStatistics { get; set; }
        public int CompletedTrainingCount { get; set; }
        public int UpcomingTraining { get; set; }
        public int TotalTrainingCount
        {
            get
            {
                return UpcomingTraining + CompletedTrainingCount;
            }
        }
        public int CompletedTrainingHours { get; set; }
        public int UpcomingTrainingHours { get; set; }
        public int TotalTrainingHours
        {
            get
            {
                return CompletedTrainingHours + UpcomingTrainingHours;
            }
        }
        public int TotalPlannedParticipants { get; set; }
        public int TotalParticipated { get; set; }
        public int TotalPassed { get; set; }
        public int TotalFailed { get; set; }
        public List<TrainingStatistic> TrainingStatistics { get; set; }
        public List<CompanyParticipant> ParticipantsByCompany { get; set; } = new List<CompanyParticipant>();
        public Dictionary<string, List<int>> DashboardData { get; set; }
        public MainReport()
        {
            CategoryStatistics = new List<CategoryStatistic>();
            TrainingStatistics = new List<TrainingStatistic>();
        }

    }
    public class CategoryStatistic
    {
        public TrainingCategory Category { get; set; }
        public int TrainingCount { get; set; }
        public bool Done { get; set; }
        public int TotalDays { get; set; }
        public int TotalHours { get; set; }

    }
    public class TrainingStatistic
    {
        public int PlanID { get; set; }
        public string Training { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int PlannedParticipants { get; set; }
        public int Participated { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public float AvaragePoint { get; set; }
        public string Location { get; set; }
    }
    public class TrainingPlanDetail
    {
        public int PlanID { get; set; }
        public string TrainingName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Category { get; set; }
        public string Cordinator { get; set; }
        public string Room { get; set; }
        public string Note { get; set; }
        public string Trainer1 { get; set; }
        public string Trainer2 { get; set; }
        public string Trainer3 { get; set; }
        public int Passpoint { get; set; }
        public List<Quota> Quotas { get; set; } = new List<Quota>();
        public List<ParticipantActivity> Participants { get; set; } = new List<ParticipantActivity>();



    }
    public class Quota
    {
        public int? CompanyID { get; set; }
        public string CompanyName { get; set; }
        public int ParticipantsCount { get; set; }
    }
    public class ParticipantActivity
    {
        public int? EmployeID { get; set; }
        public string EmployeName { get; set; }
        public bool Participated { get; set; }
        public float? ExamPoint { get; set; }
        public bool? Passed { get; set; }
    }
    public class CompanyParticipant { 
        public string Company { get; set; }
        public int Planned { get; set; }
        public int Participated { get; set; }
    }
    public class GraphData
    {
        public Dictionary<string, List<int>> Values = new Dictionary<string, List<int>>();
    }
}
