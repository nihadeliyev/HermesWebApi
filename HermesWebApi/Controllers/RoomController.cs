using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RoomController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;
        public RoomController(IUserService userService, IConfiguration configuration)
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
select * from MDTrainingRooms ORDER BY RoomName";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");
            List<Room> types = new List<Room>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                Room type = new Room();
                type.ID = ds.Tables[0].Rows[i]["RoomID"].ToString();
                type.Name = ds.Tables[0].Rows[i]["RoomName"].ToString();
                type.RoomCode = ds.Tables[0].Rows[i]["RoomCode"].ToString();
                type.Capacity = int.Parse(ds.Tables[0].Rows[i]["Capacity"].ToString() == "" ? "0" : ds.Tables[0].Rows[i]["Capacity"].ToString());
                types.Add(type);
            }

            return Ok(types);
        }
    }
}
