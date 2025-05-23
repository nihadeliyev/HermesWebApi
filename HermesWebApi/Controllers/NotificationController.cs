using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;
        public NotificationController(IUserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
            gCon = new SqlConnection(_configuration["ConnectionStrings:Default"]);
        }
        [HttpPost("create"), Authorize]
        public IActionResult Create(Notification data)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "INSERT INTO TRNotifications (Title, Details, PlanID, NotType, CreatedBy, CreatedDate) VALUES(@Title, @Details, @PlanID, @NotType, @CreatedBy, GETDATE())";
            ResultCode res = new ResultCode();
            int affRows = 0;

            res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,
                new SqlParameter("Title", data.Name),
                new SqlParameter("Details", data.Details),
                new SqlParameter("PlanID", data.PlanID),
                new SqlParameter("NotType", data.NotType),
                new SqlParameter("CreatedBy", userID)
                );

            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }
        [HttpPatch("seen"), Authorize]
        public IActionResult Seen(int notificationID)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "INSERT INTO TRNotificationReaders (NotID, UserID) VALUES(@NotID, @UserID)";
            ResultCode res = new ResultCode();
            int affRows = 0;
            res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,
                new SqlParameter("NotID", notificationID),
                new SqlParameter("UserID", userID)
                );

            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }
        [HttpGet("latest"), Authorize]
        public IActionResult GetLatest(int roleID)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "";
            ResultCode res = new ResultCode();
            DataSet ds = new DataSet();
            if (roleID == 0)
                return UnprocessableEntity("roleID 0-dan fərqli olmalıdır.");
            sql = @"
SELECT N.* FROM TRNotifications N
LEFT JOIN TRNotificationReaders R ON N.ID=R.NotID AND R.UserID=@UserID
WHERE R.UserID IS NULL {0}
";
            string filter = "";
            if (roleID == 3) //user - Planlananlarin icinde men de varamsa
                filter = " AND N.PlanID IN (SELECT PlanID FROM TRTrainingParticipants WHERE EmpID=@UserID)";
            else if (roleID == 4) //coordinator - kordinatoru oldugum sirketin quotada oldugu planlar
                filter = @" AND N.PlanID IN (select T.ID from TRPlannedTrainings T
INNER JOIN TRPlanQuotas Q ON T.ID=Q.PlanID
INNER JOIN MDUsers U ON Q.CompanyID=U.CompanyID
WHERE U.UserID=@UserID)";
            else if (roleID == 5) //trainer - Traineri oldugum planlarla baglidirsa
                filter = @" AND N.PlanID IN (select ID FROM TRPlannedTrainings WHERE TrainerID=@UserID or TrainerID2=@UserID or TrainerID3=@UserID)";
            sql = string.Format(sql, filter);
            res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return UnprocessableEntity(res.ErrorMessage);
            var notifications = new List<Notification>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                notifications.Add(
                    new Notification()
                    {
                        ID = ds.Tables[0].Rows[i]["ID"].ToString(),
                        Name = ds.Tables[0].Rows[i]["Title"].ToString(),
                        Details = ds.Tables[0].Rows[i]["Details"].ToString(),
                        NotType = int.Parse(ds.Tables[0].Rows[i]["NotType"].ToString()),
                        PlanID = int.Parse(ds.Tables[0].Rows[i]["PlanID"].ToString())
                    });
            }
            return Ok(notifications);
        }
        [HttpGet("all"), Authorize]
        public IActionResult GetAll(int roleID)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "";
            ResultCode res = new ResultCode();
            DataSet ds = new DataSet();
            if (roleID == 0)
                return UnprocessableEntity("roleID 0-dan fərqli olmalıdır.");
            sql = @"
SELECT N.*, R.CreatedDate as SeenDate FROM TRNotifications N
LEFT JOIN TRNotificationReaders R ON N.ID=R.NotID AND R.UserID=@UserID
WHERE 1=1 {0}
";
            string filter = "";
            if (roleID == 3) //user - Planlananlarin icinde men de varamsa
                filter = " AND N.PlanID IN (SELECT PlanID FROM TRTrainingParticipants WHERE EmpID=@UserID)";
            else if (roleID == 4) //coordinator - kordinatoru oldugum sirketin quotada oldugu planlar
                filter = @" AND N.PlanID IN (select T.ID from TRPlannedTrainings T
INNER JOIN TRPlanQuotas Q ON T.ID=Q.PlanID
INNER JOIN MDUsers U ON Q.CompanyID=U.CompanyID
WHERE U.UserID=@UserID)";
            else if (roleID == 5) //trainer - Traineri oldugum planlarla baglidirsa
                filter = @" AND N.PlanID IN (select ID FROM TRPlannedTrainings WHERE TrainerID=@UserID or TrainerID2=@UserID or TrainerID3=@UserID)";
            sql = string.Format(sql, filter);
            res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return UnprocessableEntity(res.ErrorMessage);
            var notifications = new List<Notification>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                notifications.Add(
                    new Notification()
                    {
                        ID = ds.Tables[0].Rows[i]["ID"].ToString(),
                        Name = ds.Tables[0].Rows[i]["Title"].ToString(),
                        Details = ds.Tables[0].Rows[i]["Details"].ToString(),
                        NotType = int.Parse(ds.Tables[0].Rows[i]["NotType"].ToString()),
                        PlanID = int.Parse(ds.Tables[0].Rows[i]["PlanID"].ToString()),
                        SeenDate = ds.Tables[0].Rows[i]["SeenDate"].ToString() == "" ? null : DateTime.Parse(ds.Tables[0].Rows[i]["SeenDate"].ToString()),
                        Seen = ds.Tables[0].Rows[i]["SeenDate"].ToString() != ""
                    });
            }
            return Ok(notifications);
        }
    }
}
