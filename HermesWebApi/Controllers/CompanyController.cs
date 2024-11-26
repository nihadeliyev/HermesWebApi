using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;
        public CompanyController(IUserService userService, IConfiguration configuration)
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
select *  from MDCompanies  ORDER BY CompanyName";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");
            List<Company> types = new List<Company>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                Company type = new Company();
                type.ID = ds.Tables[0].Rows[i]["CompanyID"].ToString();
                type.Name = ds.Tables[0].Rows[i]["CompanyName"].ToString();

                types.Add(type);
            }

            return Ok(types);
        }

        [HttpPost("create"), Authorize]
        public IActionResult Create(Company dep)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "INSERT INTO MDCompanies (CompanyName, CreatedBy, CreatedDate) VALUES(@CompanyName, @CreatedBy, GETDATE())";
            ResultCode res = new ResultCode();
            int affRows = 0;
            res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,
                new SqlParameter("CompanyName", dep.Name),
                new SqlParameter("CreatedBy", userID)
                );
            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }
    }
}
