using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;

namespace HermesWebApi.Controllers
{
    namespace HermesWebApi.Controllers
    {
        [ApiController]
        [Route("[controller]")]
        public class ProfessionController : ControllerBase
        {

            private readonly IUserService _userService;
            private readonly IConfiguration _configuration;

            SqlConnection gCon;
            public ProfessionController(IUserService userService, IConfiguration configuration)
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
select * from MDProfessions ORDER BY ProfID DESC";
                DataSet ds = new DataSet();
                ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
                if (res != ResultCodes.noError)
                    return NotFound("Data could not be found");
                List<Department> types = new List<Department>();
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    Department type = new Department();
                    type.ID = ds.Tables[0].Rows[i]["ProfID"].ToString();
                    type.Name = ds.Tables[0].Rows[i]["Profession"].ToString();
                    types.Add(type);
                }

                return Ok(types);
            }
            [HttpPost("create"), Authorize]
            public IActionResult Create(Profession data)
            {
                string? userID = _userService.GetUserId();
                if (userID == null)
                    return Unauthorized("Unable to find user information.");
                string sql = "INSERT INTO MDProfessions (Profession, CreatedBy, CreatedDate) VALUES(@Profession, @CreatedBy, GETDATE())";
                ResultCode res = new ResultCode();
                int affRows = 0;
                res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,
                    new SqlParameter("Profession", data.Name),
                    new SqlParameter("CreatedBy", userID)
                    );
                return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
            }
        }
    }
}
