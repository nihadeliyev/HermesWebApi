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
    }
}
