using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TrainerController : ControllerBase
    {

        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;
        public TrainerController(IUserService userService, IConfiguration configuration)
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
select *  from MDTrainers  ORDER BY TrainerName";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");
            List<Trainer> types = new List<Trainer>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                Trainer type = new Trainer();
                type.ID = ds.Tables[0].Rows[i]["TrainerID"].ToString();
                type.Name = ds.Tables[0].Rows[i]["TrainerName"].ToString();
                type.MailAddress = ds.Tables[0].Rows[i]["MailAddress"].ToString();
                type.Notes = ds.Tables[0].Rows[i]["Notes"].ToString();
                types.Add(type);
            }

            return Ok(types);
        }
    }
}
