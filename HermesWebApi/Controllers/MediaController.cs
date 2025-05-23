using HermesWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace HermesWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MediaController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        SqlConnection gCon;

        public MediaController(IUserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
            gCon = new SqlConnection(_configuration["ConnectionStrings:Default"]);
        }

        [HttpPost("upload-multiple")]
        public async Task<IActionResult> UploadMultipleMedia(
    [FromForm] List<IFormFile> files,
    [FromForm] string galleryName,
    [FromForm] DateTime mediaDate)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            string uploadsFolder = Path.Combine("wwwroot", "media", galleryName);
            Directory.CreateDirectory(uploadsFolder);

            List<object> uploadedFiles = new List<object>();


            foreach (var file in files)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                object insertedID = "ID";
                int rowsAffected = 0;

                string query = @"INSERT INTO Media 
                (FileName, FilePath, ContentType, UploadDate, GalleryName, MediaDate) 
                VALUES 
                (@FileName, @FilePath, @ContentType, @UploadDate, @GalleryName, @MediaDate)";

                SqlParameter[] parameters = new[]
                {
                new SqlParameter("@FileName", file.FileName),
                new SqlParameter("@FilePath", filePath),
                new SqlParameter("@ContentType", file.ContentType),
                new SqlParameter("@UploadDate", DateTime.UtcNow),
                new SqlParameter("@GalleryName", galleryName),
                new SqlParameter("@MediaDate", mediaDate)
            };

                var result = Db.ExecuteWithConnection(ref gCon, query, ref rowsAffected, parameters);
                if (result != ResultCodes.noError)
                    return StatusCode(500, "Database error while uploading file: " + file.FileName);

                uploadedFiles.Add(new
                {
                    FileName = file.FileName,
                    StoredName = fileName,
                    Gallery = galleryName,
                    MediaDate = mediaDate
                });

            }

            return Ok(new
            {
                Message = "All files uploaded successfully.",
                Files = uploadedFiles
            });
        }
        [HttpGet("media")]
        public IActionResult GetMediaByGalleryOrDate([FromQuery] string galleryName, [FromQuery] DateTime? mediaDate)
        {
            string query = "SELECT ID, FileName, FilePath, ContentType FROM Media WHERE 1=1";
            List<SqlParameter> parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(galleryName))
            {
                query += " AND GalleryName = @GalleryName";
                parameters.Add(new SqlParameter("@GalleryName", galleryName));
            }

            if (mediaDate.HasValue)
            {
                query += " AND CAST(MediaDate AS DATE) = @MediaDate";
                parameters.Add(new SqlParameter("@MediaDate", mediaDate.Value.Date));
            }


            DataSet ds = null;
            var result = Db.GetDbDataWithConnection(ref gCon, query, ref ds, parameters.ToArray());

            if (result != ResultCodes.noError || ds == null || ds.Tables[0].Rows.Count == 0)
                return NotFound("No media found");

            var mediaList = ds.Tables[0].AsEnumerable().Select(row => new
            {
                ID = row.Field<int>("ID"),
                FileName = row.Field<string>("FileName"),
                ContentType = row.Field<string>("ContentType"),
                Url = Url.Content($"~/media/{row.Field<string>("FileName")}") // Assumes public path
            });

            return Ok(mediaList);

        }

    }
}
