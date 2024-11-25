using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;

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
        public IActionResult Get(int pageNumber, DateTime? fromDate, DateTime? toDate, int pageSize = 10)
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");

            string sql = @"
select T.*,TR.TrainingName, r.RoomName, TRN.TrainerName, 
TRN2.TrainerName TrainerName2,TRN3.TrainerName TrainerName3,P.ProgramName
from TRPlannedTrainings T
LEFT JOIN MDTrainings TR ON T.TrainingID=TR.TrainingID
LEFT JOIN MDTrainingRooms R ON T.RoomID=T.RoomID
LEFT JOIN MDTrainers TRN ON T.TrainerID=TRN.TrainerID
LEFT JOIN MDTrainers TRN2 ON T.TrainerID2=TRN2.TrainerID
LEFT JOIN MDTrainers TRN3 ON T.TrainerID3=TRN3.TrainerID
LEFT JOIN MDPrograms P ON T.ProgramID=P.ProgramID
{0}
ORDER BY Date_ DESC
OFFSET @Start ROWS FETCH NEXT @RowCount ROWS ONLY;

SELECT * FROM TRPlanDates ORDER BY PlanID, Date_;
SELECT COUNT(*) FROM TRPlannedTrainings T {0}";
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
                new SqlParameter("fromDate", fromDate ?? (object)DBNull.Value), new SqlParameter("toDate", toDate ?? (object)DBNull.Value));
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
    }
}
