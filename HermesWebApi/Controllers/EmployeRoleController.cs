using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmployeRoleController : ControllerBase
    {

        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;
        public EmployeRoleController(IUserService userService, IConfiguration configuration)
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
select *  from MDRoles  ORDER BY RoleName";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");
            List<EmployeRole> types = new List<EmployeRole>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                EmployeRole type = new EmployeRole();
                type.ID = ds.Tables[0].Rows[i]["RoleID"].ToString();
                type.Name = ds.Tables[0].Rows[i]["RoleName"].ToString();
                type.Code = ds.Tables[0].Rows[i]["RoleType"].ToString();
                types.Add(type);
            }

            return Ok(types);
        }
        [HttpPost("create"), Authorize]
        public IActionResult Create(EmployeRole dep)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "INSERT INTO MDRoles (RoleName, RoleType, RoleCode, CreatedBy, CreatedDate) VALUES(@RoleName, @RoleType, @RoleCode, @CreatedBy, GETDATE())";
            ResultCode res = new ResultCode();
            int affRows = 0;
            res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,
                new SqlParameter("RoleName", dep.Name),
                new SqlParameter("RoleType", "0"),
                new SqlParameter("RoleCode", dep.Code),
                new SqlParameter("CreatedBy", userID)
                );
            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }
    }
}