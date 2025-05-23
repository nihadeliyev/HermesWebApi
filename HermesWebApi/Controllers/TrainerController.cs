using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Numerics;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TrainerController : ControllerBase
    {

        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;
        public TrainerController(IUserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
            gCon = new SqlConnection(_configuration["ConnectionStrings:Default"]);
        }

        [Microsoft.AspNetCore.Mvc.HttpGet("All"), Authorize]
        public IActionResult Get()
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");

            string sql = @"
select *  from Vw_MDTrainers  ORDER BY TrainerName;
SELECT TP.TrainerID, P.ProfID, P.Profession FROM MDTrainerProfessions TP INNER JOIN MDProfessions P ON TP.ProfID=P.ProfID";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");
            List<Trainer> types = new List<Trainer>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                Trainer type = new Trainer();
                type.ID = ds.Tables[0].Rows[i]["TrainerID"].ToString();
                type.Name = ds.Tables[0].Rows[i]["TrainerName"].ToString();
                type.MailAddress = ds.Tables[0].Rows[i]["MailAddress"].ToString();
                type.Notes = ds.Tables[0].Rows[i]["Notes"].ToString();
                type.MobileNumber = ds.Tables[0].Rows[i]["MobileNo"].ToString();
                DataRow[] drs = ds.Tables[1].Select("TrainerID=" + type.ID);
                if (drs != null && drs.Length > 0)
                {
                    for (int j = 0; j < drs.Length; j++)
                        type.Professions.Add(new Profession()
                        {
                            ID = drs[j]["ProfID"].ToString(),
                            Name = drs[j]["Profession"].ToString()
                        });
                }
                types.Add(type);
            }

            return Ok(types);
        }

        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetData(string id)
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");

            string sql = @"
select *  from Vw_MDTrainers WHERE TrainerID=@TrainerID  ORDER BY TrainerName;
SELECT TP.TrainerID, P.ProfID, P.Profession FROM MDTrainerProfessions TP INNER JOIN MDProfessions P ON TP.ProfID=P.ProfID WHERE TP.TrainerID=@TrainerID";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("TrainerID", id));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");

            Trainer type = new Trainer();
            if (ds.Tables[0].Rows.Count == 0)
                return NotFound("Data could not be found");
            type.ID = ds.Tables[0].Rows[0]["TrainerID"].ToString();
            type.Name = ds.Tables[0].Rows[0]["TrainerName"].ToString();
            type.MailAddress = ds.Tables[0].Rows[0]["MailAddress"].ToString();
            type.Notes = ds.Tables[0].Rows[0]["Notes"].ToString();
            type.MobileNumber = ds.Tables[0].Rows[0]["MobileNo"].ToString();
            DataRow[] drs = ds.Tables[1].Select("TrainerID=" + type.ID);
            if (drs != null && drs.Length > 0)
            {
                for (int j = 0; j < drs.Length; j++)
                    type.Professions.Add(new Profession()
                    {
                        ID = drs[j]["ProfID"].ToString(),
                        Name = drs[j]["Profession"].ToString()
                    });
            }
            return Ok(type);
        }
        [HttpPost("create"), Authorize]
        public IActionResult Create(Trainer data)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "INSERT INTO Vw_MDTrainers (TrainerName, MailAddress, Notes, MobileNo, CreatedBy, CreatedDate) VALUES(@TrainerName, @MailAddress, @Notes, @MobileNo, @CreatedBy, GETDATE())";
            ResultCode res = new ResultCode();
            int affRows = 0;
            object idField = "TrainerID";
            Db.BeginTransaction(ref gCon);
            res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows, ref idField,
                new SqlParameter("TrainerName", data.Name),
                new SqlParameter("MailAddress", data.MailAddress),
                new SqlParameter("Notes", data.Notes ?? ""),
                new SqlParameter("MobileNo", data.MobileNumber ?? ""),
                new SqlParameter("CreatedBy", userID)
                );
            if (res != ResultCodes.noError)
                return UnprocessableEntity(res.ErrorMessage);
            data.ID = idField.ToString();
            if (data.Professions is not null && data.Professions.Count > 0)
            {
                for (int j = 0; j < data.Professions.Count; j++)
                {
                    sql = "INSERT INTO MDTrainerProfessions (TrainerID, ProfID, CreatedBy, CreatedDate) VALUES (@TrainerID, @ProfID, @CreatedBy, GETDATE())";
                    res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,

                        new SqlParameter("TrainerID", data.ID),
                        new SqlParameter("ProfID", data.Professions[j].ID),
                        new SqlParameter("CreatedBy", userID));
                    if (res != ResultCodes.noError)
                    {
                        Db.RollbackTransaction(ref gCon);
                        return UnprocessableEntity(res.ErrorMessage);
                    }
                }

            }
            Db.CommitTransaction(ref gCon);
            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }

        [Microsoft.AspNetCore.Mvc.HttpGet("MyTrainings"), Authorize]
        public IActionResult GetMyTrainings(int pageNumber, DateTime? fromDate, DateTime? toDate, int pageSize = 10)
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
LEFT JOIN (SELECT PlanID, COUNT(DISTINCT(EmpID)) PlanCount FROM TRTrainingParticipants  GROUP BY PlanID) PL ON T.ID=PL.PlanID
LEFT JOIN (SELECT PlanID, COUNT(DISTINCT(EmpID)) FactCount FROM TRTrainingParticipated  GROUP BY PlanID) AC ON T.ID=AC.PlanID
WHERE (T.TrainerID=@TrainerID OR T.TrainerID2=@TrainerID OR T.TrainerID3=@TrainerID) {0}
ORDER BY Date_ DESC
OFFSET @Start ROWS FETCH NEXT @RowCount ROWS ONLY;

SELECT * FROM TRPlanDates ORDER BY PlanID, Date_;
SELECT COUNT(*) FROM TRPlannedTrainings T   WHERE (T.TrainerID=@TrainerID OR T.TrainerID2=@TrainerID OR T.TrainerID3=@TrainerID) {0}
";
            DataSet ds = new DataSet();
            string dateFilter = string.Empty;
            if (fromDate is not null)
                dateFilter += " AND T.Date_>=@fromDate ";
            if (toDate is not null)
                dateFilter += " AND T.Date_<=@toDate ";
            sql = string.Format(sql, dateFilter);
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("Start", (pageNumber - 1) * pageSize), new SqlParameter("RowCount", pageSize),
                new SqlParameter("fromDate", fromDate ?? (object)DBNull.Value), new SqlParameter("CompanyID", me.CompanyID),
                new SqlParameter("toDate", toDate ?? (object)DBNull.Value), new SqlParameter("TrainerID", userID));
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

        [HttpPost("SubmitPartcipated"), Authorize]
        public IActionResult SubmitParticipated(int planID, List<ParticipatedInfo> participants)
        {
            var b = new TrainingPlanController(_userService, _configuration).SubmitParticipated(planID, participants);
            return b;
        }
        /// <summary>
        /// Method that gets Trainer's main screen report
        /// </summary>
        /// <param name="companyID"></param>
        /// <param name="categoryIDs">Sample parameter: 2,4,5 or just 2 or null</param>
        /// <param name="trainingID"></param>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <returns></returns>
        [Microsoft.AspNetCore.Mvc.HttpGet("MainReport"), Authorize]
        public IActionResult Get(int? companyID, string? categoryIDs, int? trainingID, DateTime? fromDate, DateTime? toDate)
        {
            var b = new MainReportController(_userService, _configuration).Get(companyID, categoryIDs, trainingID, fromDate, toDate, 5);
            return b;
        }
    }
}
