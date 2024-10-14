using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmployeController : ControllerBase
    {

        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;
        public EmployeController(IUserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
            gCon = new SqlConnection(_configuration["ConnectionStrings:Default"]);
        }

        [Microsoft.AspNetCore.Mvc.HttpGet("All"), Authorize]
        public async Task<IActionResult> Get(int page = 1)
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");

            string sql = @"
    SELECT  E.EmpID, E.EmpName, E.FatherName, E.BirthDate, C.CompanyName, D.DepartmentName, ED.EducationName, R.RoleName 
    FROM MDEmployees E 
    LEFT JOIN MDCompanies C ON E.CompanyID = C.CompanyID 
    LEFT JOIN MDDepartments D ON E.DepartmentID = D.DepartmentID 
    LEFT JOIN MDEducations ED ON ED.EducationID = E.Education
    LEFT JOIN MDRoles R ON E.RoleID = R.RoleID
    ORDER BY E.EmpName
OFFSET @Start ROWS FETCH NEXT @RowCount ROWS ONLY
";

            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("Start", (page - 1) * 100), new SqlParameter("RowCount", 100));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");

            List<Employe> employees = new List<Employe>();
            foreach (DataRow row in ds.Tables[0].Rows)
            {
                var emp = new Employe
                {
                    ID = row["EmpID"].ToString(),
                    Name = row["EmpName"].ToString(),
                    FatherName = row["FatherName"].ToString(),
                    BirthDate = string.IsNullOrEmpty(row["BirthDate"].ToString()) ? (DateTime?)null : DateTime.Parse(row["BirthDate"].ToString()),
                    CompanyName = row["CompanyName"].ToString(),
                    DepartmentName = row["DepartmentName"].ToString(),
                    EducationName = row["EducationName"].ToString(),
                    RoleName = row["RoleName"].ToString(),
                };

                employees.Add(emp);
            }

            return Ok(employees);
        }
        [Microsoft.AspNetCore.Mvc.HttpGet("Count"), Authorize]
        public async Task<IActionResult> EmployeeCount()
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");

            string sql = @"
    SELECT  COUNT(*) 
    FROM MDEmployees 
";

            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds);
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");

            return Ok(int.Parse(ds.Tables[0].Rows[0][0].ToString()));
        }

    }
}
