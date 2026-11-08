using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Authorize(Roles = "Participant")]
public class ParticipantController : Controller
{
    private readonly ApiClient _api;

    public ParticipantController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Enrolments()
    {
        var mine = await _api.GetAsync<List<MyEnrollmentDto>>("api/enrollments/me");
        return View(mine);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _api.SendAsync(HttpMethod.Delete, $"api/enrollments/{id}");

        if (result.Ok) TempData["Success"] = "Your enrolment was cancelled.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Enrolments));
    }

    public async Task<IActionResult> Results()
    {
        var mine = await _api.GetAsync<List<MyResultDto>>("api/results/me");
        return View(mine);
    }
}
