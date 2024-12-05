﻿using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
	COUNT(DISTINCT FA.EmpName) Failed,
	PT.AvgPoint
	
FROM TRPlannedTrainings P
LEFT JOIN MDTrainings T ON P.TrainingID = T.TrainingID
LEFT JOIN TRPlanDates D ON P.ID = D.PlanID
LEFT JOIN TRTrainingParticipants PL ON P.ID=PL.PlanID
LEFT JOIN TRTrainingParticipated RE ON P.ID=RE.PlanID
LEFT JOIN Vw_TREmployeExams Ep ON P.ID=Ep.PlanID AND EP.TrainingResult='Passed'
LEFT JOIN Vw_TREmployeExams FA ON P.ID=FA.PlanID AND FA.TrainingResult='Failed'
LEFT JOIN (SELECT PlanID, AVG(Points) AvgPoint FROM  Vw_TREmployeExams GROUP BY PlanID) PT ON P.ID=PT.PlanID 
{0}
GROUP BY 
	P.ID,
    T.TrainingID, 
    T.TrainingName, 
    P.StartTime, 
    P.EndTime,
	PT.AvgPoint
ORDER BY cast (P.StartTime as date) DESC;
SELECT C.CategoryName, P.Date_, COUNT(P.ID) TrainingCount  FROM TRPlannedTrainings P
LEFT JOIN MDTrainings T ON P.TrainingID=T.TrainingID
LEFT JOIN MDTrainingCategories C ON T.Category=C.CategoryID
GROUP BY  C.CategoryName, P.Date_
ORDER BY  C.CategoryName, P.Date_;

SELECT C.CategoryName, P.Date_, COUNT(P.ID) TrainingCount  FROM TRPlannedTrainings P
LEFT JOIN MDTrainings T ON P.TrainingID=T.TrainingID
LEFT JOIN MDTrainingCategories C ON T.Category=C.CategoryID
{0}
GROUP BY  C.CategoryName, P.Date_
ORDER BY  C.CategoryName, P.Date_

SELECT C.CategoryName  FROM MDTrainingCategories C
GROUP BY  C.CategoryName
ORDER BY  C.CategoryName
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
                        PlannedParticipants = ds.Tables[1].Rows[i]["PlanCount"].ToString() == "" ? 0 : int.Parse(ds.Tables[1].Rows[i]["PlanCount"].ToString()),
                        Participated = ds.Tables[1].Rows[i]["FactCount"].ToString() == "" ? 0 : int.Parse(ds.Tables[1].Rows[i]["FactCount"].ToString()),
                        Passed = ds.Tables[1].Rows[i]["Passed"].ToString() == "" ? 0 : int.Parse(ds.Tables[1].Rows[i]["Passed"].ToString()),
                        Failed = ds.Tables[1].Rows[i]["Failed"].ToString() == "" ? 0 : int.Parse(ds.Tables[1].Rows[i]["Failed"].ToString()),
                        AvaragePoint = ds.Tables[1].Rows[i]["AvgPoint"].ToString() == "" ? 0 : float.Parse(ds.Tables[1].Rows[i]["AvgPoint"].ToString())
                    });
            }
            report.TotalPlannedParticipants = report.TrainingStatistics.Sum(k => k.PlannedParticipants);
            report.TotalParticipated = report.TrainingStatistics.Sum(k => k.Participated);
            report.TotalPassed = report.TrainingStatistics.Sum(k => k.Passed);
            report.TotalFailed = report.TrainingStatistics.Sum(k => k.Failed);
            Dictionary<string, List<int>> graphData = new Dictionary<string, List<int>>();
            if (report.TrainingStatistics.Count > 0)
            {
                DateTime from = report.TrainingStatistics.Min(p => p.StartDate);
                DateTime to = report.TrainingStatistics.Max(p => p.StartDate);

                int numberOfDays = to.Subtract(from).Days;
                List<string> keys = new List<string>();
                for (int i = 0; i < numberOfDays + 1; i++)
                {

                    if (i == 0)
                    {
                        for (int j = 0; j < ds.Tables[4].Rows.Count; j++)
                            graphData.Add(ds.Tables[4].Rows[j]["CategoryName"].ToString(), new List<int>());
                        keys = new List<string>(graphData.Keys);
                    }
                    DateTime currDate = from.AddDays(i);

                    for (int j = 0; j < keys.Count; j++)
                    {
                        DataRow[] drs = ds.Tables[3].Select("Date_='" + currDate.ToString("MM.dd.yyyy") + "' AND CategoryName='" + keys[j] + "'");
                        if (drs.Length == 0)
                            graphData[keys[j]].Add(0);
                        else
                            graphData[keys[j]].Add(int.Parse(drs[0]["TrainingCount"].ToString()));

                    }
                }
                var totalList = new List<int>();

                int maxLength = graphData.Values.Max(list => list.Count);

                for (int i = 0; i < maxLength; i++)
                {
                    int sum = graphData.Values.Sum(list => list.ElementAtOrDefault(i));
                    totalList.Add(sum);
                }

                // Add "Total" to the dictionary
                graphData.Add("Total", totalList);
            }
            
            report.DashboardData = graphData;
            return Ok(report);
        }

        [Microsoft.AspNetCore.Mvc.HttpGet("GetTraining"), Authorize]
        public IActionResult GetPlan(int planID)
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = @"
SELECT P.ID PlanID, T.TrainingName,cast (P.StartTime as time) StartTime,cast( P.EndTime as time) EndTime,
C.CategoryID, C.CategoryName, P.Organizator,  T.Passpoint, R.RoomName, P.Notes, TR1.TrainerName Trainer1, 
TR2.TrainerName Trainer2, TR3.TrainerName Trainer3,

MIN(D.Date_) StartDate, MAX(D.Date_) EndDate
FROM TRPlannedTrainings P
LEFT JOIN MDTrainings T ON P.TrainingID=T.TrainingID
LEFT JOIN MDTrainingCategories C ON T.Category=C.CategoryID
LEFT JOIN TRPlanDates D ON P.ID=D.PlanID
LEFT JOIN MDTrainingRooms R ON P.RoomID=R.RoomID
LEFT JOIN MDTrainers TR1 ON P.TrainerID=TR1.TrainerID
LEFT JOIN MDTrainers TR2 ON P.TrainerID2=TR2.TrainerID
LEFT JOIN MDTrainers TR3 ON P.TrainerID3=TR3.TrainerID
WHERE P.ID=@ID
GROUP BY 
P.ID , T.TrainingName,cast (P.StartTime as time) ,cast( P.EndTime as time) ,
C.CategoryID, C.CategoryName, P.Organizator,  T.Passpoint, R.RoomName, P.Notes,
TR1.TrainerName, TR2.TrainerName, TR3.TrainerName;

select C.CompanyID, C.CompanyName, Q.ParticipantCount from TRPlanQuotas Q
LEFT JOIN MDCompanies C ON Q.CompanyID=C.CompanyID
WHERE Q.PlanID=@ID;

SELECT E.EmpID, E.EmpName, CASE WHEN PT.EmpID IS NULL THEN 0 ELSE 1 END Participated, ex.AvgResult ExamPoint FROM TRTrainingParticipants P
LEFT JOIN MDEmployees E ON P.EmpID=E.EmpID
LEFT JOIN (SELECT PlanID, EmpID FROM TRTrainingParticipated GROUP BY PlanID, EmpID) PT ON P.PlanID=PT.PlanID AND E.EmpID=PT.EmpID
LEFT JOIN (SELECT PlanID, EmpID, AVG(Points) AvgResult FROM Vw_TREmployeExams GROUP BY PlanID, EmpID) EX ON P.PlanID=EX.PlanID AND P.EmpID=EX.EmpID
WHERE P.PlanID=@ID

";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds,
              new SqlParameter("ID", planID));
            if (res != ResultCodes.noError || ds.Tables[0].Rows.Count == 0)
                return NotFound("Data could not be found");
            TrainingPlanDetail planDetail = new TrainingPlanDetail();
            planDetail.PlanID = planID;
            planDetail.Category = ds.Tables[0].Rows[0]["CategoryName"].ToString();
            planDetail.TrainingName = ds.Tables[0].Rows[0]["TrainingName"].ToString();
            planDetail.StartDate = DateTime.Parse(ds.Tables[0].Rows[0]["StartDate"].ToString());
            planDetail.EndDate = DateTime.Parse(ds.Tables[0].Rows[0]["EndDate"].ToString());
            planDetail.StartTime = ds.Tables[0].Rows[0]["StartTime"].ToString();
            planDetail.EndTime = ds.Tables[0].Rows[0]["EndTime"].ToString();
            planDetail.Cordinator = ds.Tables[0].Rows[0]["Organizator"].ToString();
            planDetail.Room = ds.Tables[0].Rows[0]["RoomName"].ToString();
            planDetail.Note = ds.Tables[0].Rows[0]["Notes"].ToString();
            planDetail.Trainer1 = ds.Tables[0].Rows[0]["Trainer1"].ToString();
            planDetail.Trainer2 = ds.Tables[0].Rows[0]["Trainer2"].ToString();
            planDetail.Trainer3 = ds.Tables[0].Rows[0]["Trainer3"].ToString();
            planDetail.Passpoint = int.Parse(ds.Tables[0].Rows[0]["Passpoint"].ToString());
            for (int i = 0; i < ds.Tables[1].Rows.Count; i++)
                planDetail.Quotas.Add(new Quota()
                {
                    CompanyID = int.Parse(ds.Tables[1].Rows[i]["CompanyID"].ToString()),
                    ParticipantsCount = int.Parse(ds.Tables[1].Rows[i]["ParticipantCount"].ToString()),
                    CompanyName = ds.Tables[1].Rows[i]["CompanyName"].ToString()
                });
            for (int i = 0; i < ds.Tables[2].Rows.Count; i++)
            {
                var participant = new ParticipantActivity()
                {
                    EmployeID = int.Parse(ds.Tables[2].Rows[i]["EmpID"].ToString()),
                    EmployeName = ds.Tables[2].Rows[i]["EmpName"].ToString(),
                    Participated = ds.Tables[2].Rows[i]["Participated"].ToString() == "1",
                    ExamPoint = ds.Tables[2].Rows[i]["ExamPoint"].ToString() == "" ? null : float.Parse(ds.Tables[2].Rows[i]["ExamPoint"].ToString())
                };
                if (participant.ExamPoint is not null)
                    participant.Passed = planDetail.Passpoint <= participant.ExamPoint;
                planDetail.Participants.Add(participant);
            }
            return Ok(planDetail);
        }
    }
}
