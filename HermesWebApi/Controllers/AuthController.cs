using HermesWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;


        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // POST /auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] Login login)
        {
            string role;
            List<UserRole> roles = new List<UserRole>();
            string? userID = IsAuthenticated(login.Username, login.Password, out roles);
            if (userID != null)
            {
                var token = GenerateJwtToken(userID ?? "");

                return Ok(new { token, roles });
            }

            return Unauthorized("Invalid username or password.");
        }

        private string? IsAuthenticated(string usr, string pwd, out List<UserRole> roles)
        {
#pragma warning disable CS0219 // The variable 'adServer' is assigned but its value is never used
            string adServer = "poi.lan";
#pragma warning restore CS0219 // The variable 'adServer' is assigned but its value is never used
            string sql = string.Empty;
#pragma warning disable CS0219 // The variable 'authenticated' is assigned but its value is never used
            bool authenticated = false;
#pragma warning restore CS0219 // The variable 'authenticated' is assigned but its value is never used
            ResultCode result;
            DataSet ds = new DataSet();
            SqlConnection gCon = new SqlConnection(_configuration["ConnectionStrings:Default"]);
            //role = "";
            roles = new List<UserRole>();
            if (!usr.Contains("@"))
            {
                sql = @"SELECT U.UserID, U.UserName, AllowedIP, PositionID, MailAddress, DeptID, AllowedIP, CompanyID, R.RoleName 
FROM MDUsers U 
LEFT JOIN MDUserRoles R ON U.RoleID=R.RoleID WHERE UserCode=@Code AND PWDCOMPARE(@Password,Password_)=1 AND Active=1;
";
                sql = string.Format(sql, usr, pwd);

                result = Db.GetDbDataWithConnectionSessionless(ref gCon, sql, ref ds, new SqlParameter("Code", usr), new SqlParameter("Password", pwd));
                if (result.Equals(ResultCodes.dbError))
                {
                    authenticated = false;
                    return null;
                }
                else if (ds.Tables[0].Rows.Count == 0)
                {
                    //GeneralErrorDiv.InnerText = "Username or password is incorrect";
                    //FormLayout.FindItemOrGroupByName("GeneralError").Visible = true;
                    authenticated = false;
                    return null;
                }
                else
                {
                    authenticated = true;
                    DataSet dsRoles = new DataSet();
                    sql = "SELECT AR.RoleID, UR.RoleName FROM MDUserAccessRoles AR LEFT JOIN MDUserRoles UR ON AR.RoleID=UR.RoleID WHERE AR.UserID=@UserID";
                    result = Db.GetDbDataWithConnectionSessionless(ref gCon, sql, ref dsRoles, new SqlParameter("UserID", ds.Tables[0].Rows[0]["UserID"].ToString()));
                    if (result.Equals(ResultCodes.noError))
                    {
                        for (int i = 0; i < dsRoles.Tables[0].Rows.Count; i++)
                        {
                            roles.Add(new UserRole()
                            {
                                ID = dsRoles.Tables[0].Rows[i]["RoleID"].ToString(),
                                Name = dsRoles.Tables[0].Rows[i]["RoleName"].ToString()
                            });
                        }
                    }
                }
            }
            // role = ds.Tables[0].Rows[0]["RoleName"].ToString();
            return ds.Tables[0].Rows[0]["UserID"].ToString();
        }

        private string GenerateJwtToken(string userID)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Add userID as a claim in the token
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userID),  // UserID embedded here
                new Claim("userID", userID),  // UserID embedded here
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpiresInMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class Login
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
