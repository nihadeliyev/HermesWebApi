using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography.X509Certificates;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    //[Route("api/[controller]")]

    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;


        public UsersController(IUserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
            gCon = new SqlConnection(_configuration["ConnectionStrings:Default"]);
        }
        [Authorize]
        [HttpGet("me")]
        public IActionResult MyProfile()
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");
            //return Ok(new { userID = userID, message = "Authenticated User" });
            string sql = @"
SELECT * FROM MDUsers WHERE UserID=@UserID; 
SELECT R.*,F.FrameName, F.URL FROM MGUserFrameRights R LEFT JOIN MGFrames F ON R.FrameID=F.FrameID WHERE R.UserID=@UserID ORDER BY F.FrameName";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return NotFound("User could not be found");
            User usr = new User();
            usr.UserID = userID;
            usr.UserName = ds.Tables[0].Rows[0]["UserName"].ToString();
            usr.UserCode = ds.Tables[0].Rows[0]["UserCode"].ToString();
            usr.Email = ds.Tables[0].Rows[0]["MailAddress"].ToString();
            usr.Mobile = ds.Tables[0].Rows[0]["Mobile"].ToString();
            usr.CompanyID = ds.Tables[0].Rows[0]["CompanyID"].ToString() == "" ? "0" : ds.Tables[0].Rows[0]["CompanyID"].ToString();
            usr.Status = bool.Parse(ds.Tables[0].Rows[0]["Status"].ToString());
            usr.FrameRights = new List<UserFrameRight>();
            for (int i = 0; i < ds.Tables[1].Rows.Count; i++)
            {
                usr.FrameRights.Add(new UserFrameRight()
                {
                    MenuID = int.Parse(ds.Tables[1].Rows[i]["FrameID"].ToString()),
                    MenuName = ds.Tables[1].Rows[i]["FrameName"].ToString(),
                    Url = ds.Tables[1].Rows[i]["URL"].ToString(),
                    RNew = bool.Parse(ds.Tables[1].Rows[i]["Rnew"].ToString()),
                    RUpd = bool.Parse(ds.Tables[1].Rows[i]["RUpd"].ToString()),
                    RDel = bool.Parse(ds.Tables[1].Rows[i]["RDel"].ToString())
                });
            }
            return Ok(usr);

            // var username = User.Identity.Name;
            // var userID2 = User.FindFirstValue("userID");
            // var userID = User.Claims.First();
            //if (userID != null)
            // {
            //     return Ok(new { userID = userID.Value, message = "Authenticated User" });
            // }


        }
        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetUserProfile(string id)
        {
            if (id == null)
                return Unauthorized("Unable to find user information.");
            //return Ok(new { userID = userID, message = "Authenticated User" });
            string sql = @"
SELECT * FROM MDUsers WHERE UserID=@UserID; 
SELECT R.*,F.FrameName, F.URL FROM MGUserFrameRights R LEFT JOIN MGFrames F ON R.FrameID=F.FrameID WHERE R.UserID=@UserID ORDER BY F.FrameName";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", id));
            if (res != ResultCodes.noError)
                return NotFound("User could not be found");
            User usr = new User();
            usr.UserID = id;
            usr.UserName = ds.Tables[0].Rows[0]["UserName"].ToString();
            usr.UserCode = ds.Tables[0].Rows[0]["UserCode"].ToString();
            usr.Email = ds.Tables[0].Rows[0]["MailAddress"].ToString();
            usr.Mobile = ds.Tables[0].Rows[0]["Mobile"].ToString();
            usr.CompanyID = ds.Tables[0].Rows[0]["CompanyID"].ToString() == "" ? "0" : ds.Tables[0].Rows[0]["CompanyID"].ToString();
            usr.Status = bool.Parse(ds.Tables[0].Rows[0]["Status"].ToString());
            usr.FrameRights = new List<UserFrameRight>();
            for (int i = 0; i < ds.Tables[1].Rows.Count; i++)
            {
                usr.FrameRights.Add(new UserFrameRight()
                {
                    MenuID = int.Parse(ds.Tables[1].Rows[i]["FrameID"].ToString()),
                    MenuName = ds.Tables[1].Rows[i]["FrameName"].ToString(),
                    Url = ds.Tables[1].Rows[i]["URL"].ToString(),
                    RNew = bool.Parse(ds.Tables[1].Rows[i]["Rnew"].ToString()),
                    RUpd = bool.Parse(ds.Tables[1].Rows[i]["RUpd"].ToString()),
                    RDel = bool.Parse(ds.Tables[1].Rows[i]["RDel"].ToString())
                });
            }
            return Ok(usr);

            // var username = User.Identity.Name;
            // var userID2 = User.FindFirstValue("userID");
            // var userID = User.Claims.First();
            //if (userID != null)
            // {
            //     return Ok(new { userID = userID.Value, message = "Authenticated User" });
            // }


        }
        [Authorize]
        [Microsoft.AspNetCore.Mvc.HttpGet("All")]
        public IActionResult Get()
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");

            string sql = @"
SELECT U.*,C.CompanyName FROM MDUsers U LEFT JOIN MDCompanies C ON U.CompanyID=C.CompanyID ORDER BY U.UserName";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return NotFound("User could not be found");
            List<User> users = new List<User>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                User usr = new User();
                usr.UserID = userID;
                usr.UserName = ds.Tables[0].Rows[i]["UserName"].ToString();
                usr.UserCode = ds.Tables[0].Rows[i]["UserCode"].ToString();
                usr.Email = ds.Tables[0].Rows[i]["MailAddress"].ToString();
                usr.Mobile = ds.Tables[0].Rows[i]["Mobile"].ToString();
                usr.CompanyID = ds.Tables[0].Rows[i]["CompanyID"].ToString() == "" ? "0" : ds.Tables[0].Rows[0]["CompanyID"].ToString();
                usr.CompanyName = ds.Tables[0].Rows[i]["CompanyName"].ToString();
                usr.Status = bool.Parse(ds.Tables[0].Rows[i]["Status"].ToString());
                users.Add(usr);
            }

            return Ok(users);
        }


        [HttpPost("create"), Authorize]
        public IActionResult CreateUser(User usr)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "exec [Sp_MDCreateUser] @UserName, @UserCode, @Pswd, @ADAccount, @MailAddress, @Mobile, @Description, @AllowedIP, @CreatedBy, @CompanyID";
            ResultCode res = new ResultCode();
            int affRows = 0;
            res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,
                new SqlParameter("UserName", usr.UserName),
                new SqlParameter("UserCode", usr.UserCode),
                new SqlParameter("Pswd", usr.Password),
                new SqlParameter("ADAccount", usr.ADAccount),
                new SqlParameter("MailAddress", usr.Email),
                new SqlParameter("Mobile", usr.Mobile),
                new SqlParameter("Description", ""),
                new SqlParameter("AllowedIP", "*"),
                new SqlParameter("CreatedBy", userID),
                new SqlParameter("CompanyID", usr.CompanyID)
                );
            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }

    }
}
