using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserRoleController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;
        public UserRoleController(IUserService userService, IConfiguration configuration)
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
select *  from MDUserRoles  ORDER BY RoleID";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");
            List<UserRole> types = new List<UserRole>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                UserRole type = new UserRole();
                type.ID = ds.Tables[0].Rows[i]["RoleID"].ToString();
                type.Name = ds.Tables[0].Rows[i]["RoleName"].ToString();

                types.Add(type);
            }

            return Ok(types);
        }
    }
}
