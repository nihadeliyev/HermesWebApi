using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TrainingCategoryController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;
        public TrainingCategoryController(IUserService userService, IConfiguration configuration)
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
select * from MDTrainingCategories ORDER BY CategoryName";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");
            List<TrainingCategory> cats = new List<TrainingCategory>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                TrainingCategory cat = new TrainingCategory();
                cat.ID = ds.Tables[0].Rows[i]["CategoryID"].ToString();
                cat.Name = ds.Tables[0].Rows[i]["CategoryName"].ToString();
                cats.Add(cat);
            }

            return Ok(cats);
        }
    }
}
