using LaundrySignalR.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaundrySignalR.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SubjectsController(IJsonFileService jsonFileService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var subjects = await jsonFileService.LoadSubjects();
        if (subjects == null || subjects.Count == 0)
        {
            return BadRequest("No Subjects found");
        }
        return Ok(subjects);
    }
}