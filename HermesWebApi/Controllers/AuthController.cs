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
            string? userID = IsAuthenticated(login.Username, login.Password);
            if (userID != null)
            {
                var token = GenerateJwtToken(userID ?? "");
                return Ok(new { token });
            }

            return Unauthorized("Invalid username or password.");
        }

        [Microsoft.AspNetCore.Mvc.HttpGet("Get2")]
        public IActionResult Get()
        {
            return Ok("");
        }
        private string? IsAuthenticated(string usr, string pwd)
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
            if (!usr.Contains("@"))
            {
                sql = "SELECT UserID, UserName, AllowedIP, PositionID, MailAddress, DeptID, AllowedIP, CompanyID FROM MDUsers WHERE UserCode=@Code AND PWDCOMPARE(@Password,Password_)=1 AND Active=1";
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
                }
            }


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
