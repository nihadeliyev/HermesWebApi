using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CoordinatorController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;
        SqlConnection gCon;
        public CoordinatorController(IUserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
            gCon = new SqlConnection(_configuration["ConnectionStrings:Default"]);
        }
        [Microsoft.AspNetCore.Mvc.HttpGet("Trainings"), Authorize]
        public IActionResult GetTrainingsForMyCompany(int pageNumber, DateTime? fromDate, DateTime? toDate, int pageSize = 10)
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");
            var a = new UsersController(_userService, _configuration).GetUserProfile(userID);
            if (a.GetType() != typeof(OkObjectResult))
                return a;

            var me = (((OkObjectResult)a).Value as User);
            string sql = @"
select T.*,TR.TrainingName, r.RoomName, TRN.TrainerName, 
TRN2.TrainerName TrainerName2,TRN3.TrainerName TrainerName3,P.ProgramName, PL.PlanCount, AC.FactCount
from TRPlannedTrainings T
LEFT JOIN MDTrainings TR ON T.TrainingID=TR.TrainingID
LEFT JOIN MDTrainingRooms R ON T.RoomID=R.RoomID
LEFT JOIN Vw_MDTrainers TRN ON T.TrainerID=TRN.TrainerID
LEFT JOIN Vw_MDTrainers TRN2 ON T.TrainerID2=TRN2.TrainerID
LEFT JOIN Vw_MDTrainers TRN3 ON T.TrainerID3=TRN3.TrainerID
LEFT JOIN MDPrograms P ON T.ProgramID=P.ProgramID
INNER JOIN TRPlanQuotas Q ON T.ID=Q.PlanID AND Q.CompanyID=@CompanyID
LEFT JOIN (SELECT PlanID, COUNT(DISTINCT(EmpID)) PlanCount FROM TRTrainingParticipants  GROUP BY PlanID) PL ON T.ID=PL.PlanID
LEFT JOIN (SELECT PlanID, COUNT(DISTINCT(EmpID)) FactCount FROM TRTrainingParticipated  GROUP BY PlanID) AC ON T.ID=AC.PlanID
{0}
ORDER BY Date_ DESC
OFFSET @Start ROWS FETCH NEXT @RowCount ROWS ONLY;

SELECT * FROM TRPlanDates ORDER BY PlanID, Date_;
SELECT COUNT(*) FROM TRPlannedTrainings T INNER JOIN TRPlanQuotas Q ON T.ID=Q.PlanID AND Q.CompanyID=@CompanyID  {0}";
            DataSet ds = new DataSet();
            string dateFilter = string.Empty;
            if (fromDate is not null)
            {
                dateFilter += " WHERE T.Date_>=@fromDate ";
                if (toDate is not null)
                    dateFilter += " AND T.Date_<=@toDate ";
            }
            else if (toDate is not null)
                dateFilter = " WHERE T.Date_<=@toDate ";
            sql = string.Format(sql, dateFilter);
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("Start", (pageNumber - 1) * pageSize), new SqlParameter("RowCount", pageSize),
                new SqlParameter("fromDate", fromDate ?? (object)DBNull.Value), new SqlParameter("CompanyID", me.CompanyID), new SqlParameter("toDate", toDate ?? (object)DBNull.Value));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");
            List<TrainingPlan> data = new List<TrainingPlan>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                TrainingPlan plan = new TrainingPlan();
                plan.ID = ds.Tables[0].Rows[i]["ID"].ToString();
                plan.Name = ds.Tables[0].Rows[i]["PlanName"].ToString();
                plan.TrainingID = int.Parse(ds.Tables[0].Rows[i]["TrainingID"].ToString());
                plan.TrainingName = ds.Tables[0].Rows[i]["TrainingName"].ToString();
                plan.RoomID = int.Parse(ds.Tables[0].Rows[i]["RoomID"].ToString());
                plan.RoomName = ds.Tables[0].Rows[i]["RoomName"].ToString();
                plan.TrainerID = int.Parse(ds.Tables[0].Rows[i]["TrainerID"].ToString());
                plan.TrainerID2 = ds.Tables[0].Rows[i]["TrainerID2"].ToString() == "" ? null : int.Parse(ds.Tables[0].Rows[i]["TrainerID2"].ToString());
                plan.TrainerID3 = ds.Tables[0].Rows[i]["TrainerID3"].ToString() == "" ? null : int.Parse(ds.Tables[0].Rows[i]["TrainerID3"].ToString());
                plan.TrainerName = ds.Tables[0].Rows[i]["TrainerName"].ToString();
                plan.TrainerName2 = ds.Tables[0].Rows[i]["TrainerName2"].ToString();
                plan.TrainerName3 = ds.Tables[0].Rows[i]["TrainerName3"].ToString();
                plan.StartTime = DateTime.Parse(ds.Tables[0].Rows[i]["StartTime"].ToString());
                plan.EndTime = DateTime.Parse(ds.Tables[0].Rows[i]["EndTime"].ToString());
                plan.Date = DateTime.Parse(ds.Tables[0].Rows[i]["Date_"].ToString());
                plan.Organizator = ds.Tables[0].Rows[i]["Organizator"].ToString();
                plan.ProgramID = ds.Tables[0].Rows[i]["ProgramID"].ToString() == "" ? null : int.Parse(ds.Tables[0].Rows[i]["ProgramID"].ToString());
                plan.ProgramName = ds.Tables[0].Rows[i]["ProgramName"].ToString();
                plan.Organizator = ds.Tables[0].Rows[i]["Organizator"].ToString();
                plan.Notes = ds.Tables[0].Rows[i]["Notes"].ToString();
                plan.PlannedParticipantCount = ds.Tables[0].Rows[i]["PlanCount"].ToString() == "" ? null : int.Parse(ds.Tables[0].Rows[i]["PlanCount"].ToString());
                plan.ActualParticipantCount = ds.Tables[0].Rows[i]["FactCount"].ToString() == "" ? null : int.Parse(ds.Tables[0].Rows[i]["FactCount"].ToString());

                plan.Dates = new List<DateTime>();
                DataRow[] dates = ds.Tables[1].Select("PlanID=" + plan.ID);
                for (int j = 0; j < dates.Length; j++)
                {
                    plan.Dates.Add(DateTime.Parse(ds.Tables[1].Rows[j]["Date_"].ToString()));
                }
                data.Add(plan);
            }
            
            DataList<TrainingPlan> list = new DataList<TrainingPlan>();
            list.PageSize = pageSize;
            list.CurrentPage = pageNumber;
            list.Data = data;
            list.RowCount = int.Parse(ds.Tables[2].Rows[0][0].ToString());
            return Ok(list);
        }
        [Microsoft.AspNetCore.Mvc.HttpGet("Trainings/{id}"), Authorize]
        public IActionResult GetTrainingPlan(int id)
        {
            var userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            var a = new UsersController(_userService, _configuration).GetUserProfile(userID);
            if (a.GetType() != typeof(OkObjectResult))
                return a;

            var me = (((OkObjectResult)a).Value as User);
            var plan = new TrainingPlanController(_userService, _configuration);

            return plan.Get(id, int.Parse(me.CompanyID));

        }


        [HttpPost("SubmitPartcipants"), Authorize]
        public IActionResult SubmitPartcipants(int planID, List<int> participants)
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");
            var a = new UsersController(_userService, _configuration).GetUserProfile(userID);
            if (a.GetType() != typeof(OkObjectResult))
                return a;

            var me = (((OkObjectResult)a).Value as User);
            var b = new TrainingPlanController(_userService, _configuration).SubmitPartcipants(planID, participants, int.Parse(me.CompanyID));
            return b;
        }

        [Microsoft.AspNetCore.Mvc.HttpGet("Employees"), Authorize]
        public async Task<IActionResult> GetEmplyees(int pageNumber, int pageSize)
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");
            var a = new UsersController(_userService, _configuration).GetUserProfile(userID);
            if (a.GetType() != typeof(OkObjectResult))
                return a;

            var me = (((OkObjectResult)a).Value as User);

            string sql = @"
    SELECT  E.EmpID, E.EmpName, E.FatherName, E.BirthDate, C.CompanyName, D.DepartmentName, ED.EducationName, R.RoleName , E.EmailAddress, E.PhoneNumber, E.Notes
    FROM Vw_MDEmployees E 
    LEFT JOIN MDCompanies C ON E.CompanyID = C.CompanyID 
    LEFT JOIN MDDepartments D ON E.DepartmentID = D.DepartmentID 
    LEFT JOIN MDEducations ED ON ED.EducationID = E.Education
    LEFT JOIN MDRoles R ON E.RoleID = R.RoleID
WHERE E.CompanyID=@CompanyID
    ORDER BY E.EmpID DESC
OFFSET @Start ROWS FETCH NEXT @RowCount ROWS ONLY;
SELECT COUNT(*) FROM Vw_MDEmployees WHERE CompanyID=@CompanyID;
";

            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("Start", (pageNumber - 1) * pageSize), 
                new SqlParameter("RowCount", pageSize), new SqlParameter("CompanyID", me.CompanyID));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");

            List<Employe> employees = new List<Employe>();
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                var emp = new Employe
                {
                    ID = row["EmpID"].ToString(),
                    Name = row["EmpName"].ToString(),
                    FatherName = row["FatherName"].ToString(),
                    BirthDate = string.IsNullOrEmpty(row["BirthDate"].ToString()) ? (DateTime?)null : DateTime.Parse(row["BirthDate"].ToString()),
                    CompanyName = row["CompanyName"].ToString(),
                    DepartmentName = row["DepartmentName"].ToString(),
                    EducationName = row["EducationName"].ToString(),
                    RoleName = row["RoleName"].ToString(),
                    PhoneNumber = row["PhoneNumber"].ToString(),
                    EmailAddress = row["EmailAddress"].ToString()
                };

                employees.Add(emp);
            }
            DataList<Employe> dl = new DataList<Employe>();
            dl.PageSize = pageSize;
            dl.CurrentPage = pageNumber;
            dl.RowCount = int.Parse(ds.Tables[1].Rows[0][0].ToString());
            dl.Data = employees;
            return Ok(dl);
        }

        [Microsoft.AspNetCore.Mvc.HttpGet("MainReport"), Authorize]
        public IActionResult Get(int? companyID, string? categoryIDs, int? trainingID, DateTime? fromDate, DateTime? toDate)
        {
            var b = new MainReportController(_userService, _configuration).Get(companyID, categoryIDs, trainingID, fromDate, toDate, 4);
            return b;
        }
    }
}
