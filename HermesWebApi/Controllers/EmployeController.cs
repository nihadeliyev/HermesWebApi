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
        public IActionResult Get()
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");

            string sql = @"
select E.*, C.CompanyName, D.DepartmentName, ED.EducationName, R.RoleName  from MDEmployees E 
LEFT JOIN MDCompanies C ON E.CompanyID=C.CompanyID 
LEFT JOIN MDDepartments D ON E.DepartmentID=D.DepartmentID 
LEFT JOIN MDEducations ED ON ED.EducationID=E.Education
LEFT JOIN MDRoles R ON E.RoleID=R.RoleID
ORDER BY E.EmpName";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");
            List<Employe> types = new List<Employe>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                Employe type = new Employe();
                type.ID = ds.Tables[0].Rows[i]["EmpID"].ToString();
                type.Name = ds.Tables[0].Rows[i]["EmpName"].ToString();
                type.FatherName = ds.Tables[0].Rows[i]["FatherName"].ToString();
                type.BirthDate = ds.Tables[0].Rows[i]["BirthDate"].ToString() == "" ? null : DateTime.Parse(ds.Tables[0].Rows[i]["BirthDate"].ToString());
                type.Education = ds.Tables[0].Rows[i]["Education"].ToString() == "" ? 0 : int.Parse(ds.Tables[0].Rows[i]["Education"].ToString());
                type.EducationName = ds.Tables[0].Rows[i]["EducationName"].ToString();
                type.Notes = ds.Tables[0].Rows[i]["Notes"].ToString();
                type.PhoneNumber = ds.Tables[0].Rows[i]["PhoneNumber"].ToString();
                type.EmailAddress = ds.Tables[0].Rows[i]["EmailAddress"].ToString();
                type.CompanyID = ds.Tables[0].Rows[i]["CompanyID"].ToString() == "" ? 0 : int.Parse(ds.Tables[0].Rows[i]["CompanyID"].ToString());
                type.CompanyName = ds.Tables[0].Rows[i]["CompanyName"].ToString();
                type.DepartmentID = ds.Tables[0].Rows[i]["DepartmentID"].ToString() == "" ? 0 : int.Parse(ds.Tables[0].Rows[i]["DepartmentID"].ToString());
                type.DepartmentName = ds.Tables[0].Rows[i]["DepartmentName"].ToString();
                type.RoleID = ds.Tables[0].Rows[i]["RoleID"].ToString() == "" ? 0 : int.Parse(ds.Tables[0].Rows[i]["RoleID"].ToString());
                type.RoleName = ds.Tables[0].Rows[i]["RoleName"].ToString();

                types.Add(type);
            }

            return Ok(types);
        }
    }
}
