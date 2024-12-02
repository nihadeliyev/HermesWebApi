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

        public MainReport()
        {
            CategoryStatistics=new List<CategoryStatistic>();
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
}
