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

group by CASE WHEN P.Date_<=getdate() THEN 1 ELSE 0 end, C.CategoryID, C.CategoryName";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
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
            return Ok(report);
        }
    }
}
