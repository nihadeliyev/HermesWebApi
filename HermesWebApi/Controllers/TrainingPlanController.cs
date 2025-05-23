using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Numerics;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TrainingPlanController : ControllerBase
    {

        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;
        public TrainingPlanController(IUserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
            gCon = new SqlConnection(_configuration["ConnectionStrings:Default"]);
        }

        [Microsoft.AspNetCore.Mvc.HttpGet("All"), Authorize]
        public IActionResult Get(int pageNumber, DateTime? fromDate, DateTime? toDate, string? filter, int pageSize = 10, string? sortBy = "date", string? sortOrder = "desc")
        {
            var userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");

            // Mapping user-friendly field names to actual database columns
            var columnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "trainingName", "TR.TrainingName" },
        { "roomName", "R.RoomName" },
        { "trainerName", "TRN.TrainerName" },
        { "status", "S.StatusName" },
        { "startDate", "T.StartTime" },
        { "endDate", "T.EndTime" },
        { "date", "T.Date_" },
        { "programName", "P.ProgramName" }
    };

            // Default to "date" if invalid sortBy is provided
            string orderByColumn = columnMappings.ContainsKey(sortBy) ? columnMappings[sortBy] : "T.Date_";

            // Ensure sort order is either ASC or DESC (default to DESC)
            string orderDirection = (sortOrder?.ToLower() == "asc") ? "ASC" : "DESC";

            // Construct SQL query with ORDER BY
            string sql = @"
    select T.*, TR.TrainingName, R.RoomName, TRN.TrainerName, 
        CASE WHEN T.StatusID<>1 OR StartTime>GETDATE() THEN S.StatusName 
        ELSE (CASE WHEN StartTime<GETDATE() THEN (CASE WHEN EndTime<GETDATE() THEN N'Bitdi' ELSE N'Başladı' END) END) 
        END as StatusName,
        TRN2.TrainerName TrainerName2, TRN3.TrainerName TrainerName3, P.ProgramName, PL.PlanCount, AC.FactCount
    from TRPlannedTrainings T
    LEFT JOIN MDTrainings TR ON T.TrainingID=TR.TrainingID
    LEFT JOIN MDTrainingRooms R ON T.RoomID=R.RoomID
    LEFT JOIN MDTrainingStatuses S ON T.StatusID=S.StatusID
    LEFT JOIN Vw_MDTrainers TRN ON T.TrainerID=TRN.TrainerID
    LEFT JOIN Vw_MDTrainers TRN2 ON T.TrainerID2=TRN2.TrainerID
    LEFT JOIN Vw_MDTrainers TRN3 ON T.TrainerID3=TRN3.TrainerID
    LEFT JOIN MDPrograms P ON T.ProgramID=P.ProgramID
    LEFT JOIN (SELECT PlanID, COUNT(DISTINCT(EmpID)) PlanCount FROM TRTrainingParticipants GROUP BY PlanID) PL ON T.ID=PL.PlanID
    LEFT JOIN (SELECT PlanID, COUNT(DISTINCT(EmpID)) FactCount FROM TRTrainingParticipated GROUP BY PlanID) AC ON T.ID=AC.PlanID
    {0}
    ORDER BY {1} {2}
    OFFSET @Start ROWS FETCH NEXT @RowCount ROWS ONLY;
    
    SELECT * FROM TRPlanDates ORDER BY PlanID, Date_;
    
    SELECT COUNT(*)
    FROM TRPlannedTrainings T
    LEFT JOIN MDTrainings TR ON T.TrainingID=TR.TrainingID
    LEFT JOIN MDTrainingRooms R ON T.RoomID=R.RoomID
    LEFT JOIN MDTrainingStatuses S ON T.StatusID=S.StatusID
    LEFT JOIN Vw_MDTrainers TRN ON T.TrainerID=TRN.TrainerID
    LEFT JOIN Vw_MDTrainers TRN2 ON T.TrainerID2=TRN2.TrainerID
    LEFT JOIN Vw_MDTrainers TRN3 ON T.TrainerID3=TRN3.TrainerID
    LEFT JOIN MDPrograms P ON T.ProgramID=P.ProgramID
    LEFT JOIN (SELECT PlanID, COUNT(DISTINCT(EmpID)) PlanCount FROM TRTrainingParticipants GROUP BY PlanID) PL ON T.ID=PL.PlanID
    LEFT JOIN (SELECT PlanID, COUNT(DISTINCT(EmpID)) FactCount FROM TRTrainingParticipated GROUP BY PlanID) AC ON T.ID=AC.PlanID
    {0};";

            // Construct filters
            string dateFilter = string.Empty;
            if (fromDate is not null)
            {
                dateFilter += " WHERE T.Date_>=@fromDate ";
                if (toDate is not null)
                    dateFilter += " AND T.Date_<=@toDate ";
            }
            else if (toDate is not null)
                dateFilter = " WHERE T.Date_<=@toDate ";

            if (!string.IsNullOrEmpty(filter))
            {
                dateFilter += ((string.IsNullOrEmpty(dateFilter) ? " WHERE " : " AND ") +
                    @"TR.TrainingName LIKE @filter OR R.RoomName LIKE @filter OR TRN.TrainerName LIKE @filter OR TRN2.TrainerName LIKE @filter OR 
              TRN3.TrainerName LIKE @filter OR P.ProgramName LIKE @filter OR 
              (CASE WHEN T.StatusID<>1 OR StartTime>GETDATE() THEN S.StatusName ELSE
                 (CASE WHEN StartTime<GETDATE() THEN (CASE WHEN EndTime<GETDATE() THEN N'Bitdi' ELSE N'Başladı' END) END) end) LIKE @filter 
");
            }

            sql = string.Format(sql, dateFilter, orderByColumn, orderDirection);

            // Execute the query
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds,
                new SqlParameter("Start", (pageNumber - 1) * pageSize),
                new SqlParameter("RowCount", pageSize),
                new SqlParameter("fromDate", fromDate ?? (object)DBNull.Value),
                new SqlParameter("filter", $"%{filter}%"),
                new SqlParameter("toDate", toDate ?? (object)DBNull.Value));

            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");

            // Parse results
            List<TrainingPlan> data = new List<TrainingPlan>();
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                var plan = new TrainingPlan
                {
                    ID = row["ID"].ToString(),
                    TrainingID = int.Parse(row["TrainingID"].ToString()),
                    TrainingName = row["TrainingName"].ToString(),
                    RoomID = int.Parse(row["RoomID"].ToString()),
                    RoomName = row["RoomName"].ToString(),
                    TrainerID = int.Parse(row["TrainerID"].ToString()),
                    TrainerName = row["TrainerName"].ToString(),
                    StartTime = DateTime.Parse(row["StartTime"].ToString()),
                    EndTime = DateTime.Parse(row["EndTime"].ToString()),
                    Date = DateTime.Parse(row["Date_"].ToString()),
                    StatusName = row["StatusName"].ToString(),
                    PlannedParticipantCount = row["PlanCount"].ToString() == "" ? null : int.Parse(row["PlanCount"].ToString()),
                    ActualParticipantCount = row["FactCount"].ToString() == "" ? null : int.Parse(row["FactCount"].ToString()),
                };
                plan.Dates = new List<DateTime>();
                DataRow[] dates = ds.Tables[1].Select("PlanID=" + plan.ID);
                for (int j = 0; j < dates.Length; j++)
                {
                    plan.Dates.Add(DateTime.Parse(ds.Tables[1].Rows[j]["Date_"].ToString()));
                }
                data.Add(plan);
            }

            return Ok(new DataList<TrainingPlan>
            {
                PageSize = pageSize,
                CurrentPage = pageNumber,
                Data = data,
                RowCount = int.Parse(ds.Tables[2].Rows[0][0].ToString())
            });
        }


        [HttpGet("{id}"), Authorize]
        public IActionResult Get(int id, int companyID = 0)
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");

            string sql = @"
--Training info
select T.*,TR.TrainingName, r.RoomName, TRN.TrainerName, 
TRN2.TrainerName TrainerName2,TRN3.TrainerName TrainerName3,P.ProgramName, PL.PlanCount, AC.FactCount,
CASE WHEN T.StatusID<>1 OR StartTime>GETDATE() THEN S.StatusName ELSE (CASE WHEN StartTime<GETDATE() THEN (CASE WHEN EndTime<GETDATE() THEN N'Bitdi' ELSE N'Başladı' END) END) end as StatusName
from TRPlannedTrainings T
LEFT JOIN MDTrainings TR ON T.TrainingID=TR.TrainingID
LEFT JOIN MDTrainingStatuses S ON S.StatusID=T.StatusID
LEFT JOIN MDTrainingRooms R ON T.RoomID=R.RoomID
LEFT JOIN Vw_MDTrainers TRN ON T.TrainerID=TRN.TrainerID
LEFT JOIN Vw_MDTrainers TRN2 ON T.TrainerID2=TRN2.TrainerID
LEFT JOIN Vw_MDTrainers TRN3 ON T.TrainerID3=TRN3.TrainerID
LEFT JOIN MDPrograms P ON T.ProgramID=P.ProgramID
LEFT JOIN (SELECT PlanID, COUNT(DISTINCT(EmpID)) PlanCount FROM TRTrainingParticipants  GROUP BY PlanID) PL ON T.ID=PL.PlanID
LEFT JOIN (SELECT PlanID, COUNT(DISTINCT(EmpID)) FactCount FROM TRTrainingParticipated  GROUP BY PlanID) AC ON T.ID=AC.PlanID
WHERE T.ID=@ID
ORDER BY Date_ DESC;

--Training dates
SELECT * FROM TRPlanDates WHERE PlanID=@ID ORDER BY PlanID, Date_;

--Training quota
SELECT Q.*, C.CompanyName FROM TRPlanQuotas Q LEFT JOIN MDCompanies C ON Q.CompanyID=C.CompanyID WHERE Q.PlanID=@ID;

SELECT E.EmpID, E.EmpName,E.CompanyID,C.CompanyName FROM TRTrainingParticipants P 
INNER JOIN Vw_MDEmployees E ON P.EmpID=E.EmpID 
LEFT JOIN MDCompanies C ON E.CompanyID=C.CompanyID WHERE P.PlanID=@ID AND E.CompanyID=(CASE WHEN @CompanyID=0 THEN E.CompanyID ELSE @CompanyID END)

SELECT E.EmpID, E.EmpName, p.Date_, E.CompanyID,C.CompanyName FROM TRTrainingParticipated P 
INNER JOIN Vw_MDEmployees E ON P.EmpID=E.EmpID 
LEFT JOIN MDCompanies C ON E.CompanyID=C.CompanyID WHERE P.PlanID=@ID AND E.CompanyID=(CASE WHEN @CompanyID=0 THEN E.CompanyID ELSE @CompanyID END)
ORDER BY P.Date_, C.CompanyName,E.EmpName
";
            DataSet ds = new DataSet();

            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("ID", id), new SqlParameter("CompanyID", companyID));
            if (res != ResultCodes.noError || ds.Tables[0].Rows.Count == 0)
                return NotFound("Data could not be found");

            TrainingPlan plan = new TrainingPlan();
            plan.ID = ds.Tables[0].Rows[0]["ID"].ToString();
            plan.Name = ds.Tables[0].Rows[0]["PlanName"].ToString();
            plan.TrainingID = int.Parse(ds.Tables[0].Rows[0]["TrainingID"].ToString());
            plan.TrainingName = ds.Tables[0].Rows[0]["TrainingName"].ToString();
            plan.RoomID = int.Parse(ds.Tables[0].Rows[0]["RoomID"].ToString());
            plan.RoomName = ds.Tables[0].Rows[0]["RoomName"].ToString();
            plan.TrainerID = int.Parse(ds.Tables[0].Rows[0]["TrainerID"].ToString());
            plan.TrainerID2 = ds.Tables[0].Rows[0]["TrainerID2"].ToString() == "" ? null : int.Parse(ds.Tables[0].Rows[0]["TrainerID2"].ToString());
            plan.TrainerID3 = ds.Tables[0].Rows[0]["TrainerID3"].ToString() == "" ? null : int.Parse(ds.Tables[0].Rows[0]["TrainerID3"].ToString());
            plan.TrainerName = ds.Tables[0].Rows[0]["TrainerName"].ToString();
            plan.TrainerName2 = ds.Tables[0].Rows[0]["TrainerName2"].ToString();
            plan.TrainerName3 = ds.Tables[0].Rows[0]["TrainerName3"].ToString();
            plan.StartTime = DateTime.Parse(ds.Tables[0].Rows[0]["StartTime"].ToString());
            plan.EndTime = DateTime.Parse(ds.Tables[0].Rows[0]["EndTime"].ToString());
            plan.Date = DateTime.Parse(ds.Tables[0].Rows[0]["Date_"].ToString());
            plan.Organizator = ds.Tables[0].Rows[0]["Organizator"].ToString();
            plan.ProgramID = ds.Tables[0].Rows[0]["ProgramID"].ToString() == "" ? null : int.Parse(ds.Tables[0].Rows[0]["ProgramID"].ToString());
            plan.ProgramName = ds.Tables[0].Rows[0]["ProgramName"].ToString();
            plan.Notes = ds.Tables[0].Rows[0]["Notes"].ToString();
            plan.StatusID = int.Parse(ds.Tables[0].Rows[0]["StatusID"].ToString());
            plan.StatusName = ds.Tables[0].Rows[0]["StatusName"].ToString();
            plan.PlannedParticipantCount = ds.Tables[0].Rows[0]["PlanCount"].ToString() == "" ? null : int.Parse(ds.Tables[0].Rows[0]["PlanCount"].ToString());
            plan.ActualParticipantCount = ds.Tables[0].Rows[0]["FactCount"].ToString() == "" ? null : int.Parse(ds.Tables[0].Rows[0]["FactCount"].ToString());

            plan.Dates = new List<DateTime>();

            for (int j = 0; j < ds.Tables[1].Rows.Count; j++)
            {
                plan.Dates.Add(DateTime.Parse(ds.Tables[1].Rows[j]["Date_"].ToString()));
            }
            for (int j = 0; j < ds.Tables[2].Rows.Count; j++)
            {
                plan.Quotas.Add(new TrainingQuota()
                {
                    CompanyID = int.Parse(ds.Tables[2].Rows[j]["CompanyID"].ToString()),
                    CompanyName = ds.Tables[2].Rows[j]["CompanyName"].ToString(),
                    MaxParticipant = ds.Tables[2].Rows[j]["ParticipantCount"].ToString() == "" ? null : int.Parse(ds.Tables[2].Rows[j]["ParticipantCount"].ToString())
                });
            }
            for (int j = 0; j < ds.Tables[3].Rows.Count; j++)
            {
                plan.PlannedParticipants.Add(new ParticipantInfo()
                {
                    EmpID = int.Parse(ds.Tables[3].Rows[j]["EmpID"].ToString()),
                    EmpName = ds.Tables[3].Rows[j]["EmpName"].ToString(),
                    Company = ds.Tables[3].Rows[j]["CompanyName"].ToString()
                });
            }
            for (int j = 0; j < ds.Tables[4].Rows.Count; j++)
            {
                plan.Participants.Add(new ParticipatedInfo()
                {
                    EmpID = int.Parse(ds.Tables[4].Rows[j]["EmpID"].ToString()),
                    EmpName = ds.Tables[4].Rows[j]["EmpName"].ToString(),
                    Company = ds.Tables[4].Rows[j]["CompanyName"].ToString(),
                    Date = DateTime.Parse(ds.Tables[4].Rows[j]["Date_"].ToString())
                });
            }
            return Ok(plan);
        }

        [Microsoft.AspNetCore.Mvc.HttpGet("Count"), Authorize]
        public async Task<IActionResult> Count()
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");

            string sql = @"
    SELECT  COUNT(*) 
    FROM TRPlannedTrainings 
";

            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds);
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");

            return Ok(int.Parse(ds.Tables[0].Rows[0][0].ToString()));
        }
        [HttpPost("create"), Authorize]
        public IActionResult CreatePlan(TrainingPlan plan)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = @"
INSERT INTO TRPlannedTrainings 
(PlanName, TrainingID, RoomID, Date_, StartTime, EndTime, TrainerID, TrainerID2, TrainerID3, Organizator, ProgramID, Notes, MaxCapacity, CreatedBy, CreatedDate) 
VALUES
(@PlanName, @TrainingID, @RoomID, @Date_, @StartTime, @EndTime, @TrainerID, @TrainerID2, @TrainerID3, @Organizator, @ProgramID, @Notes, @MaxCapacity, @CreatedBy, GETDATE())";
            ResultCode res = new ResultCode();
            DataSet ds = new DataSet();
            int affRows = 0;
            object id = "ID";

            res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows, ref id,
                new SqlParameter("PlanName", plan.Name),
                new SqlParameter("TrainingID", plan.TrainingID),
                new SqlParameter("RoomID", plan.RoomID),
                new SqlParameter("Date_", plan.Date.Date),
                new SqlParameter("StartTime", plan.Date.Date + plan.StartTime.TimeOfDay),
                new SqlParameter("EndTime", plan.Date.Date + plan.EndTime.TimeOfDay),
                new SqlParameter("TrainerID", plan.TrainerID),
                new SqlParameter("TrainerID2", plan.TrainerID2),
                new SqlParameter("TrainerID3", plan.TrainerID3),
                new SqlParameter("Organizator", plan.Organizator),
                new SqlParameter("ProgramID", plan.ProgramID),
                new SqlParameter("Notes", plan.Notes),
                new SqlParameter("MaxCapacity", plan.MaxCapacity),
                new SqlParameter("CreatedBy", userID)
                );
            if (res != ResultCodes.noError)
                return UnprocessableEntity(res.ErrorMessage);

            plan.ID = id.ToString();
            for (int i = 0; i < plan.Dates.Count; i++)
            {
                sql = "INSERT INTO TRPlanDates (PlanID, Date_, CreatedBy, CreatedDate) VALUES (@PlanID, @Date_, @CreatedBy, GETDATE())";
                res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,
                    new SqlParameter("PlanID", plan.ID),
                    new SqlParameter("Date_", plan.Dates[i].Date),
                    new SqlParameter("CreatedBy", userID));
            }
            for (int i = 0; i < plan.Quotas.Count; i++)
            {
                sql = "INSERT INTO TRPlanQuotas (PlanID, CompanyID, ParticipantCount, CreatedBy, CreatedDate) VALUES (@PlanID, @CompanyID, @ParticipantCount, @CreatedBy, GETDATE())";
                res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,
                    new SqlParameter("PlanID", plan.ID),
                    new SqlParameter("CompanyID", plan.Quotas[i].CompanyID),
                    new SqlParameter("ParticipantCount", plan.Quotas[i].MaxParticipant ?? (object)DBNull.Value),
                    new SqlParameter("CreatedBy", userID));
            }

            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }

        [HttpPost("submitPartcipants"), Authorize]
        public IActionResult SubmitPartcipants(int planID, List<int> participants, int companyID = 0)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "";
            if (companyID == 0)
                sql = @"
DELETE FROM TRTrainingParticipants WHERE PlanID=@ID";
            else
                sql = "DELETE FROM TRTrainingParticipants WHERE PlanID=@ID AND EmpID IN (SELECT EmpID FROM Vw_MDEmployees WHERE CompanyID=@CompanyID)";
            ResultCode res = new ResultCode();
            DataSet ds = new DataSet();
            int affRows = 0;

            res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,
                new SqlParameter("ID", planID),
                new SqlParameter("CompanyID", companyID)
                );
            if (res != ResultCodes.noError)
                return UnprocessableEntity(res.ErrorMessage);

            for (int i = 0; i < participants.Count; i++)
            {
                sql = "INSERT INTO TRTrainingParticipants (PlanID, EmpID, CreatedBy, CreatedDate) VALUES (@PlanID, @EmpID, @CreatedBy, GETDATE())";
                res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,
                    new SqlParameter("PlanID", planID),
                    new SqlParameter("EmpID", participants[i]),
                    new SqlParameter("CreatedBy", userID));
            }
            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }
        [HttpPost("submitPartcipated"), Authorize]
        public IActionResult SubmitParticipated(int planID, List<ParticipatedInfo> participants, int companyID = 0)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "";
            if (companyID == 0)
                sql = @"
DELETE FROM TRTrainingParticipated WHERE PlanID=@ID";
            else
                sql = "DELETE FROM TRTrainingParticipated WHERE PlanID=@ID AND EmpID IN (SELECT EmpID FROM Vw_MDEmployees WHERE CompanyID=@CompanyID)";
            ResultCode res = new ResultCode();
            DataSet ds = new DataSet();
            int affRows = 0;

            res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,
                new SqlParameter("ID", planID),
                new SqlParameter("CompanyID", companyID)
                );
            if (res != ResultCodes.noError)
                return UnprocessableEntity(res.ErrorMessage);

            for (int i = 0; i < participants.Count; i++)
            {
                sql = "INSERT INTO TRTrainingParticipated (PlanID, EmpID, Date_, CreatedBy, CreatedDate) VALUES (@PlanID, @EmpID, @Date_, @CreatedBy, GETDATE())";
                res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,
                    new SqlParameter("PlanID", planID),
                    new SqlParameter("EmpID", participants[i].EmpID),
                    new SqlParameter("Date_", participants[i].Date),
                    new SqlParameter("CreatedBy", userID));
            }
            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }

        [Microsoft.AspNetCore.Mvc.HttpPatch("Publish"), Authorize]
        public async Task<IActionResult> Publish(int planID)
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "UPDATE TRPlannedTrainings SET StatusID=1 WHERE ID=@PlanID";
            int affRows = 0;
            ResultCode res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows, new SqlParameter("PlanID", planID));
            if (res == ResultCodes.noError)
            {
                var a = new TrainingPlanController(_userService, _configuration).Get(planID);

                if (a.GetType() != typeof(OkObjectResult))
                    return a;
                var plan = (((OkObjectResult)a).Value as TrainingPlan);

                new NotificationController(_userService, _configuration).Create(new Notification()
                {
                    Name = $"{plan.TrainingName} elan edildi. Tarix: {plan.StartTime.ToString("dd.MM.yyyy HH:mm")}",
                    Details = $"Məkan: {plan.RoomName}, {plan.StartTime.ToString("HH:mm")} - {plan.EndTime.ToString("HH:mm")} ({plan.Dates.Count} gün)",
                    PlanID = planID,
                    NotType = 1
                });
            }
            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);

        }
        [Microsoft.AspNetCore.Mvc.HttpPatch("Cancel"), Authorize]
        public async Task<IActionResult> Cancel(int planID, string reason)
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "UPDATE TRPlannedTrainings SET StatusID=2 WHERE ID=@PlanID";
            int affRows = 0;
            ResultCode res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows, new SqlParameter("PlanID", planID));
            if (res == ResultCodes.noError)
            {
                var a = new TrainingPlanController(_userService, _configuration).Get(planID);

                if (a.GetType() != typeof(OkObjectResult))
                    return a;
                var plan = (((OkObjectResult)a).Value as TrainingPlan);

                new NotificationController(_userService, _configuration).Create(new Notification()
                {
                    Name = $"{plan.TrainingName} ləğv edildi. Tarix: {plan.StartTime.ToString("dd.MM.yyyy HH:mm")}",
                    Details = $"Məkan: {plan.RoomName}, {plan.StartTime.ToString("HH:mm")} - {plan.EndTime.ToString("HH:mm")} ({plan.Dates.Count} gün)",
                    PlanID = planID,
                    NotType = 0
                });
            }
            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }
        [Microsoft.AspNetCore.Mvc.HttpDelete("Delete"), Authorize]
        public async Task<IActionResult> Delete(int planID)
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "DELETE FROM  TRPlannedTrainings WHERE StatusID=0 AND ID=@PlanID";
            int affRows = 0;
            ResultCode res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows, new SqlParameter("PlanID", planID));
            if (res != ResultCodes.noError)
                return UnprocessableEntity(res.ErrorMessage);
            else if (res == ResultCodes.noError && affRows == 0)
                return NotFound($"Plan tapılmadı : {planID}");
            sql = @"
DELETE FROM TRTrainingParticipated WHERE PlanID=@PlanID
DELETE FROM TRPlanDates WHERE PlanID=@PlanID
DELETE FROM TRPlanQuotas WHERE PlanID=@PlanID
DELETE FROM TRTrainingParticipants WHERE PlanID=@PlanID
DELETE FROM TRTrainingFiles WHERE PlanID=@PlanID
DELETE FROM TREmployeExams WHERE PlanID=@PlanID
DELETE FROM TRPlannedExams WHERE PlanID=@PlanID
";
            res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows, new SqlParameter("PlanID", planID));
            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }
    }

}
