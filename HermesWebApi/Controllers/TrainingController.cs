using HermesWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TrainingController : ControllerBase
    {

        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;
        public TrainingController(IUserService userService, IConfiguration configuration)
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
select T.*,C.CategoryName,  TT.TypeName  from MDTrainings T 
LEFT JOIN MDTrainingCategories C ON T.Category=C.CategoryID LEFT JOIN MDTrainingTypes TT ON T.TrType=TT.TypeID  ORDER BY TrainingName";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("UserID", userID));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");
            List<Training> types = new List<Training>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                Training type = new Training();
                type.ID = ds.Tables[0].Rows[i]["TrainingID"].ToString();
                type.Name = ds.Tables[0].Rows[i]["TrainingName"].ToString();
                type.TrainingCode = ds.Tables[0].Rows[i]["TrainingCode"].ToString();
                type.CategoryID = int.Parse(ds.Tables[0].Rows[i]["Category"].ToString() == "" ? "0" : ds.Tables[0].Rows[i]["Category"].ToString());
                type.TypeID = int.Parse(ds.Tables[0].Rows[i]["TrType"].ToString() == "" ? "0" : ds.Tables[0].Rows[i]["TrType"].ToString());
                type.CategoryName = ds.Tables[0].Rows[i]["CategoryName"].ToString();
                type.TypeName = ds.Tables[0].Rows[i]["TypeName"].ToString();
                type.TypeID = int.Parse(ds.Tables[0].Rows[i]["ExpireDays"].ToString() == "" ? "0" : ds.Tables[0].Rows[i]["ExpireDays"].ToString());
                type.Certificate = bool.Parse(ds.Tables[0].Rows[i]["Certificate"].ToString() == "" ? "False" : ds.Tables[0].Rows[i]["Certificate"].ToString());
                type.Passpoint = int.Parse(ds.Tables[0].Rows[i]["Passpoint"].ToString() == "" ? "0" : ds.Tables[0].Rows[i]["Passpoint"].ToString());
                type.Notes = ds.Tables[0].Rows[i]["Notes"].ToString();
                type.Status = bool.Parse(ds.Tables[0].Rows[i]["Active"].ToString() == "" ? "False" : ds.Tables[0].Rows[i]["Active"].ToString());

                types.Add(type);
            }

            return Ok(types);
        }
        [HttpPost("create"), Authorize]
        public IActionResult Create(Training data)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "INSERT INTO MDTrainings (TrainingName, TrainingCode, Category, TrType, ExpireDays, Certificate, Passpoint, Notes, Active, CreatedBy, CreatedDate) VALUES(@TrainingName, @TrainingCode, @Category, @TrType, @ExpireDays, @Certificate, @Passpoint, @Notes, @Active, @CreatedBy, GETDATE())";
            ResultCode res = new ResultCode();
            int affRows = 0;
            res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,
                new SqlParameter("TrainingName", data.Name),
                new SqlParameter("TrainingCode", data.TrainingCode),
                new SqlParameter("Category", data.CategoryID),
                new SqlParameter("TrType", data.TypeID),
                new SqlParameter("ExpireDays", data.ExpireDays ?? 0),
                new SqlParameter("Certificate", data.Certificate ?? false),
                new SqlParameter("Passpoint", data.Passpoint ?? 0),
                new SqlParameter("Notes", data.Notes ?? ""),
                new SqlParameter("Active", data.Status ?? true),
                new SqlParameter("CreatedBy", userID)
                );
            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }
    }
}
