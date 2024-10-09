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
        public IActionResult Get()
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
ORDER BY Date_ DESC; 

SELECT * FROM TRPlanDates ORDER BY PlanID, Date_";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");
            List<TrainingPlan> types = new List<TrainingPlan>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                TrainingPlan type = new TrainingPlan();
                type.ID = ds.Tables[0].Rows[i]["ID"].ToString();
                type.Name = ds.Tables[0].Rows[i]["PlanName"].ToString();
                type.TrainingID = int.Parse(ds.Tables[0].Rows[i]["TrainingID"].ToString());
                type.TrainingName = ds.Tables[0].Rows[i]["TrainingName"].ToString();
                type.RoomID = int.Parse(ds.Tables[0].Rows[i]["RoomID"].ToString());
                type.RoomName = ds.Tables[0].Rows[i]["RoomName"].ToString();
                type.TrainerID = int.Parse(ds.Tables[0].Rows[i]["TrainerID"].ToString());
                type.TrainerID2 = ds.Tables[0].Rows[i]["TrainerID2"].ToString() == "" ? null : int.Parse(ds.Tables[0].Rows[i]["TrainerID2"].ToString());
                type.TrainerID3 = ds.Tables[0].Rows[i]["TrainerID3"].ToString() == "" ? null : int.Parse(ds.Tables[0].Rows[i]["TrainerID3"].ToString());
                type.TrainerName = ds.Tables[0].Rows[i]["TrainerName"].ToString();
                type.TrainerName2 = ds.Tables[0].Rows[i]["TrainerName2"].ToString();
                type.TrainerName3 = ds.Tables[0].Rows[i]["TrainerName3"].ToString();
                type.StartTime = DateTime.Parse(ds.Tables[0].Rows[i]["StartTime"].ToString());
                type.EndTime = DateTime.Parse(ds.Tables[0].Rows[i]["EndTime"].ToString());
                type.Date = DateTime.Parse(ds.Tables[0].Rows[i]["Date_"].ToString());
                type.Organizator = ds.Tables[0].Rows[i]["Organizator"].ToString();
                type.ProgramID = ds.Tables[0].Rows[i]["ProgramID"].ToString() == "" ? null : int.Parse(ds.Tables[0].Rows[i]["ProgramID"].ToString());
                type.ProgramName = ds.Tables[0].Rows[i]["ProgramName"].ToString();
                type.Organizator = ds.Tables[0].Rows[i]["Organizator"].ToString();
                type.Notes = ds.Tables[0].Rows[i]["Notes"].ToString();
                type.Dates = new List<DateTime>();
                DataRow[] dates = ds.Tables[1].Select("PlanID=" + type.ID);
                for (int j = 0; j < dates.Length; j++)
                {
                    type.Dates.Add(DateTime.Parse(ds.Tables[1].Rows[j]["Date_"].ToString()));
                }
                types.Add(type);
            }

            return Ok(types);
        }
    }
}
