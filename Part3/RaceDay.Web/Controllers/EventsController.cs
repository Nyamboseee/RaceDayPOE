using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Authorize]
public class EventsController : Controller
{
    private readonly ApiClient _api;

    public EventsController(ApiClient api)
    {
        _api = api;
    }

    // Participant home page: upcoming events with filters
    public async Task<IActionResult> Index(string? search, string? type, decimal? maxDistance, bool showPast = false)
    {
        var all = await _api.GetAsync<List<EventDto>>("api/events");

        var events = all.AsEnumerable();
        if (!showPast) events = events.Where(e => e.Date >= DateTime.Today);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            events = events.Where(e =>
                e.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(type)) events = events.Where(e => e.EventType == type);
        if (maxDistance.HasValue) events = events.Where(e => e.Distance <= maxDistance.Value);

        return View(new EventFilterViewModel
        {
            Search = search,
            Type = type,
            MaxDistance = maxDistance,
            ShowPast = showPast,
            Events = events.OrderBy(e => e.Date).ToList()
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{id}");
        var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");

        var model = new EventDetailsViewModel { Event = ev, Categories = categories };

        if (User.IsInRole("Participant"))
        {
            var mine = await _api.GetAsync<List<MyEnrollmentDto>>("api/enrollments/me");
            model.MyEnrollments = mine
                .Where(m => m.EventId == id)
                .ToDictionary(m => m.CategoryId, m => m.Status);
        }

        return View(model);
    }

    [HttpPost, Authorize(Roles = "Participant")]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var result = await _api.SendAsync(HttpMethod.Post, $"api/categories/{categoryId}/enrollments");

        if (result.Ok)
        {
            TempData["Success"] = "You are in! Your enrolment is pending until the organiser confirms it.";
            return RedirectToAction("Enrolments", "Participant");
        }

        TempData["Error"] = result.Status == System.Net.HttpStatusCode.Conflict
            ? "You are already enrolled in that category."
            : result.Error;
        return RedirectToAction(nameof(Details), new { id = eventId });
    }
}
