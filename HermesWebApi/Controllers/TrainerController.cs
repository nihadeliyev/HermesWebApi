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
select *  from MDTrainers  ORDER BY TrainerName;
SELECT TP.TrainerID, P.ProfID, P.Profession FROM MDTrainerProfessions TP INNER JOIN MDProfessions P ON TP.ProfID=P.ProfID";
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
                type.MobileNumber = ds.Tables[0].Rows[i]["MobileNo"].ToString();
                DataRow[] drs = ds.Tables[1].Select("TrainerID=" + type.ID);
                if (drs != null && drs.Length > 0)
                {
                    for (int j = 0; j < drs.Length; j++)
                        type.Professions.Add(new Profession()
                        {
                            ID = drs[j]["ProfID"].ToString(),
                            Name = drs[j]["Profession"].ToString()
                        });
                }
                types.Add(type);
            }

            return Ok(types);
        }

        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetData(string id)
        {
            var userID = _userService.GetUserId();

            if (userID == null)
                return Unauthorized("Unable to find user information.");

            string sql = @"
select *  from MDTrainers WHERE TrainerID=@TrainerID  ORDER BY TrainerName;
SELECT TP.TrainerID, P.ProfID, P.Profession FROM MDTrainerProfessions TP INNER JOIN MDProfessions P ON TP.ProfID=P.ProfID WHERE TP.TrainerID=@TrainerID";
            DataSet ds = new DataSet();
            ResultCode res = Db.GetDbDataWithConnection(ref gCon, sql, ref ds, new SqlParameter("TrainerID", id));
            if (res != ResultCodes.noError)
                return NotFound("Data could not be found");

            Trainer type = new Trainer();
            type.ID = ds.Tables[0].Rows[0]["TrainerID"].ToString();
            type.Name = ds.Tables[0].Rows[0]["TrainerName"].ToString();
            type.MailAddress = ds.Tables[0].Rows[0]["MailAddress"].ToString();
            type.Notes = ds.Tables[0].Rows[0]["Notes"].ToString();
            type.MobileNumber = ds.Tables[0].Rows[0]["MobileNo"].ToString();
            DataRow[] drs = ds.Tables[1].Select("TrainerID=" + type.ID);
            if (drs != null && drs.Length > 0)
            {
                for (int j = 0; j < drs.Length; j++)
                    type.Professions.Add(new Profession()
                    {
                        ID = drs[j]["ProfID"].ToString(),
                        Name = drs[j]["Profession"].ToString()
                    });
            }
            return Ok(type);
        }
        [HttpPost("create"), Authorize]
        public IActionResult Create(Trainer data)
        {
            string? userID = _userService.GetUserId();
            if (userID == null)
                return Unauthorized("Unable to find user information.");
            string sql = "INSERT INTO MDTrainers (TrainerName, MailAddress, Notes, MobileNo, CreatedBy, CreatedDate) VALUES(@TrainerName, @MailAddress, @Notes, @MobileNo, @CreatedBy, GETDATE())";
            ResultCode res = new ResultCode();
            int affRows = 0;
            object idField = "TrainerID";
            Db.BeginTransaction(ref gCon);
            res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows, ref idField,
                new SqlParameter("TrainerName", data.Name),
                new SqlParameter("MailAddress", data.MailAddress),
                new SqlParameter("Notes", data.Notes ?? ""),
                new SqlParameter("MobileNo", data.MobileNumber ?? ""),
                new SqlParameter("CreatedBy", userID)
                );
            if (res != ResultCodes.noError)
                return UnprocessableEntity(res.ErrorMessage);
            data.ID = idField.ToString();
            if (data.Professions is not null && data.Professions.Count > 0)
            {
                for (int j = 0; j < data.Professions.Count; j++)
                {
                    sql = "INSERT INTO MDTrainerProfessions (TrainerID, ProfID, CreatedBy, CreatedDate) VALUES (@TrainerID, @ProfID, @CreatedBy, GETDATE())";
                    res = Db.ExecuteWithConnection(ref gCon, sql, ref affRows,

                        new SqlParameter("TrainerID", data.ID),
                        new SqlParameter("ProfID", data.Professions[j].ID),
                        new SqlParameter("CreatedBy", userID));
                    if (res != ResultCodes.noError)
                    {
                        Db.RollbackTransaction(ref gCon);
                        return UnprocessableEntity(res.ErrorMessage);
                    }
                }

            }
            Db.CommitTransaction(ref gCon);
            return res == ResultCodes.noError ? Ok(res.ErrorMessage) : UnprocessableEntity(res.ErrorMessage);
        }
    }
}
