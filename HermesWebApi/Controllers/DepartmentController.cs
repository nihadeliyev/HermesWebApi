using HermesWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Authorization;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DepartmentController : ControllerBase
    {

        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;
        public DepartmentController(IUserService userService, IConfiguration configuration)
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
select * from MDDepartments ORDER BY DepartmentName";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");
            List<Department> types = new List<Department>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                Department type = new Department();
                type.ID = ds.Tables[0].Rows[i]["DepartmentID"].ToString();
                type.Name = ds.Tables[0].Rows[i]["DepartmentName"].ToString();
                types.Add(type);
            }

            return Ok(types);
        }
        [HttpPost("create"), Authorize]
        public IActionResult Create(Department dep)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "INSERT INTO MDDepartments (DepartmentName, CreatedBy, CreatedDate) VALUES(@DepartmentName, @CreatedBy, GETDATE())";
            ResultCode res = new ResultCode();
            int affRows = 0;
            res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,
                new SqlParameter("DepartmentName", dep.Name),
                new SqlParameter("CreatedBy", userID)
                );
            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }
    }
}
