using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using System.Data;
using System.Data.SqlClient;

namespace HermesWebApi.Controllers
{
    public class MainReportController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;
        public MainReportController(IUserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
            gCon = new SqlConnection(_configuration["ConnectionStrings:Default"]);
        }

        [Microsoft.AspNetCore.Mvc.HttpGet("All"), Authorize]
        public IActionResult Get(int? companyID, int? categoryID, int? trainingID, DateTime? fromDate, DateTime? toDate)
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");

            string sql = @"
SELECT CASE WHEN P.Date_<=getdate() THEN 1 ELSE 0 end AS Done, C.CategoryID, C.CategoryName,  COUNT(P.ID) PlanCount, SUM(PH.TotalDays) TotalDays, SUM(PH.TotalHours) TotalHours  FROM TRPlannedTrainings P 
LEFT JOIN MDTrainings T ON P.TrainingID=T.TrainingID
LEFT JOIN MDTrainingCategories C ON T.Category=C.CategoryID
LEFT JOIN (select P.ID,COUNT(D.ID) TotalDays,  COUNT(D.ID)* convert(float, DATEDIFF(MINUTE, StartTime,EndTime))/60 TotalHours from TRPlannedTrainings P
			LEFT JOIN TRPlanDates D ON P.ID=D.PlanID GROUP BY P.ID,StartTime,EndTime) PH on P.ID=PH.ID
{0}
group by CASE WHEN P.Date_<=getdate() THEN 1 ELSE 0 end, C.CategoryID, C.CategoryName;

SELECT P.ID PlanID,
    T.TrainingID, 
    T.TrainingName, 
    cast (P.StartTime as date) StartDate, 
	cast (P.StartTime as time) StartTime, 
    cast (MAX(D.Date_) as date) AS EndDate,
	cast (P.EndTime as time) EndTime,
	COUNT(DISTINCT PL.EmpID) PlanCount,
	COUNT(DISTINCT RE.EmpID) FactCount ,
	COUNT(DISTINCT EP.EmpName) Passed,
	COUNT(DISTINCT FA.EmpName) Failed
FROM TRPlannedTrainings P
LEFT JOIN MDTrainings T ON P.TrainingID = T.TrainingID
LEFT JOIN TRPlanDates D ON P.ID = D.PlanID
LEFT JOIN TRTrainingParticipants PL ON P.ID=PL.PlanID
LEFT JOIN TRTrainingParticipated RE ON P.ID=RE.PlanID
LEFT JOIN Vw_TREmployeExams Ep ON P.ID=Ep.PlanID AND EP.TrainingResult='Passed'
LEFT JOIN Vw_TREmployeExams FA ON P.ID=FA.PlanID AND FA.TrainingResult='Failed'
{0}
GROUP BY 
	P.ID,
    T.TrainingID, 
    T.TrainingName, 
    P.StartTime, 
    P.EndTime;

";
            DataSet ds = new DataSet();
            string filter = string.Empty;
            if (categoryID is not null && categoryID > 0)
                filter += ((filter == "" ? " WHERE " : " AND ") + " T.Category=@CategoryID ");
            if (trainingID is not null && trainingID > 0)
                filter += ((filter == "" ? " WHERE " : " AND ") + " P.TrainingID=@TrainingID ");
            if (fromDate is not null)
                filter += ((filter == "" ? " WHERE " : " AND ") + " P.Date_>=@FromDate ");
            if (toDate is not null)
                filter += ((filter == "" ? " WHERE " : " AND ") + " P.Date_<=@ToDate ");
            sql = string.Format(sql, filter);
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds,
                new SqlParameter("CategoryID", categoryID ?? 0),
                new SqlParameter("TrainingID", trainingID ?? 0),
                new SqlParameter("FromDate", fromDate ?? (object)DBNull.Value),
                new SqlParameter("ToDate", toDate ?? (object)DBNull.Value)
                );
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");
            MainReport report = new MainReport();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                report.CategoryStatistics.Add(new CategoryStatistic()
                {
                    Category = new TrainingCategory()
                    {
                        ID = ds.Tables[0].Rows[i]["CategoryID"].ToString(),
                        Name = ds.Tables[0].Rows[i]["CategoryName"].ToString()
                    },
                    TrainingCount = int.Parse(ds.Tables[0].Rows[i]["PlanCount"].ToString()),
                    TotalDays = int.Parse(ds.Tables[0].Rows[i]["TotalDays"].ToString()),
                    TotalHours = int.Parse(ds.Tables[0].Rows[i]["TotalHours"].ToString()),
                    Done = ds.Tables[0].Rows[i]["Done"].ToString() == "1"
                });
            }
            report.CompletedTrainingCount = report.CategoryStatistics.Where(x => x.Done == true).Sum(k => k.TrainingCount);
            report.UpcomingTraining = report.CategoryStatistics.Where(x => x.Done == false).Sum(k => k.TrainingCount);
            report.CompletedTrainingHours = report.CategoryStatistics.Where(x => x.Done == true).Sum(k => k.TotalHours);
            report.UpcomingTrainingHours = report.CategoryStatistics.Where(x => x.Done == false).Sum(k => k.TotalHours);

            for (int i = 0; i < ds.Tables[1].Rows.Count; i++)
            {
                report.TrainingStatistics.Add(
                    new TrainingStatistic()
                    {
                        PlanID = int.Parse(ds.Tables[1].Rows[i]["PlanID"].ToString()),
                        Training = ds.Tables[1].Rows[i]["TrainingName"].ToString(),
                        StartDate = DateTime.Parse(ds.Tables[1].Rows[i]["StartDate"].ToString()).Add(TimeSpan.Parse(ds.Tables[1].Rows[i]["StartTime"].ToString())),
                        EndDate = DateTime.Parse(ds.Tables[1].Rows[i]["EndDate"].ToString()).Add(TimeSpan.Parse(ds.Tables[1].Rows[i]["EndTime"].ToString())),
                        PlannedParticipants = int.Parse(ds.Tables[1].Rows[i]["PlanCount"].ToString()),
                        Participated = int.Parse(ds.Tables[1].Rows[i]["FactCount"].ToString()),
                        Passed = int.Parse(ds.Tables[1].Rows[i]["Passed"].ToString()),
                        Failed = int.Parse(ds.Tables[1].Rows[i]["Failed"].ToString())
                    }
                    );
            }
            report.TotalPlannedParticipants = report.TrainingStatistics.Sum(k => k.PlannedParticipants);
            report.TotalParticipated = report.TrainingStatistics.Sum(k => k.Participated);
            report.TotalPassed = report.TrainingStatistics.Sum(k => k.Passed);
            report.TotalFailed = report.TrainingStatistics.Sum(k => k.Failed);
            return Ok(report);
        }
    }
}
