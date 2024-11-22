using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using System.ComponentModel.Design;
using System.Data.Common;
using System.Net.Mail;
using System.Xml.Linq;

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
        public async Task<IActionResult> Get(int pageNumber, int pageSize, string orderBy="E.EmpID DESC")
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");

            string sql = @"
    SELECT  E.EmpID, E.EmpName, E.FatherName, E.BirthDate, C.CompanyName, D.DepartmentName, ED.EducationName, R.RoleName , E.EmailAddress, E.PhoneNumber, E.Notes
    FROM MDEmployees E 
    LEFT JOIN MDCompanies C ON E.CompanyID = C.CompanyID 
    LEFT JOIN MDDepartments D ON E.DepartmentID = D.DepartmentID 
    LEFT JOIN MDEducations ED ON ED.EducationID = E.Education
    LEFT JOIN MDRoles R ON E.RoleID = R.RoleID
    ORDER BY E.EmpID DESC
OFFSET @Start ROWS FETCH NEXT @RowCount ROWS ONLY;
SELECT COUNT(*) FROM MDEmployees;
";

            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("Start", (pageNumber - 1) * pageSize), new SqlParameter("RowCount", pageSize));
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
                    PhoneNumber = row["PhoneNumber"].ToString(),
                    EmailAddress = row["EmailAddress"].ToString()
                };

                employees.Add(emp);
            }
            DataList<Employe> dl = new DataList<Employe>();
            dl.PageSize = pageSize;
            dl.CurrentPage = pageNumber;
            dl.RowCount = int.Parse(ds.Tables[1].Rows[0][0].ToString());
            dl.Data = employees;
            return Ok(dl);
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

        [HttpPost("create"), Authorize]
        public IActionResult Create(Employe data)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            object idField = "EmpID";
            ResultCode res;
            int affRows = 0;
            string sql = string.Empty;
            sql = @"INSERT INTO MDEmployees
                       (EmpName
                       ,EmpSurname
                       ,FatherName
                       ,BirthDate
                       ,Education
                       ,Citizenship
                       ,Notes
                       ,PhoneNumber
                       ,EmailAddress
                       ,CompanyID
                       ,DepartmentID
                       ,RoleID
                       ,CreatedBy)
                VALUES
                        (@EmpName
                       ,@EmpSurname
                       ,@FatherName
                       ,@BirthDate
                       ,@Education
                       ,@Citizenship
                       ,@Notes
                       ,@PhoneNumber
                       ,@EmailAddress
                       ,@CompanyID
                       ,@DepartmentID
                       ,@RoleID
                       ,@CreatedBy)
";
            res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows, ref idField,
                new SqlParameter("EmpName", data.Name),
                new SqlParameter("EmpSurname", ""),
                new SqlParameter("FatherName", data.FatherName ?? ""),
                new SqlParameter("BirthDate", data.BirthDate ?? (object)DBNull.Value),
                new SqlParameter("Education", data.Education),
                new SqlParameter("Citizenship", ""),
                new SqlParameter("Notes", data.Notes ?? ""),
                new SqlParameter("PhoneNumber", data.PhoneNumber ?? ""),
                new SqlParameter("EmailAddress", data.EmailAddress ?? ""),
                new SqlParameter("CompanyID", data.CompanyID),
                new SqlParameter("DepartmentID", data.DepartmentID),
                new SqlParameter("RoleID", data.RoleID),
                new SqlParameter("CreatedBy", userID));
            if (res.Equals(ResultCodes.noError))
                data.ID = idField.ToString();
            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }

    }
}
