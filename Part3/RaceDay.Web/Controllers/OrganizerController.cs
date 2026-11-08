using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[Authorize(Roles = "Organizer")]
public class OrganizerController : Controller
{
    private readonly ApiClient _api;

    public OrganizerController(ApiClient api)
    {
        _api = api;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // The API lists every event, so keep only the ones this organizer created
    private async Task<List<EventDto>> MyEventsAsync()
    {
        var all = await _api.GetAsync<List<EventDto>>("api/events");
        return all.Where(e => e.OrganizerId == CurrentUserId).OrderBy(e => e.Date).ToList();
    }

    // An organizer can only manage their own events
    private async Task<EventDto> MyEventAsync(int eventId)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        if (ev.OrganizerId != CurrentUserId)
            throw new ApiException(System.Net.HttpStatusCode.Forbidden, "Not your event.");
        return ev;
    }

    // ---------- Dashboard ----------

    public async Task<IActionResult> Dashboard()
    {
        return View(new DashboardViewModel { Events = await MyEventsAsync() });
    }

    // ---------- Events ----------

    public async Task<IActionResult> Events()
    {
        return View(await MyEventsAsync());
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        return View("EventForm", new EventFormViewModel
        {
            EventDate = DateTime.Today.AddDays(30).AddHours(6)
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View("EventForm", model);

        var result = await _api.SendAsync(HttpMethod.Post, "api/events", ToApiBody(model));
        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View("EventForm", model);
        }

        var created = ApiClient.Parse<System.Text.Json.JsonElement>(result);
        var newId = created.GetProperty("id").GetInt32();

        var bannerMessage = await UploadBannerAsync(newId, model.Banner);
        if (bannerMessage != null)
        {
            TempData["Error"] = "The event was created, but the banner was not saved: " + bannerMessage;
            return RedirectToAction(nameof(EditEvent), new { id = newId });
        }

        TempData["Success"] = "Event created. Now add the race categories.";
        return RedirectToAction(nameof(Categories), new { eventId = newId });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var ev = await MyEventAsync(id);
        return View("EventForm", new EventFormViewModel
        {
            Id = ev.Id,
            EventName = ev.Name,
            Description = ev.Description,
            EventDate = ev.Date,
            Location = ev.Location,
            Distance = ev.Distance,
            EventType = ev.EventType,
            ExistingBannerUrl = ev.BannerUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid) return View("EventForm", model);

        var result = await _api.SendAsync(HttpMethod.Put, $"api/events/{id}", ToApiBody(model));
        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View("EventForm", model);
        }

        var bannerMessage = await UploadBannerAsync(id, model.Banner);
        if (bannerMessage != null)
        {
            TempData["Error"] = "The details were saved, but the banner was not: " + bannerMessage;
            return RedirectToAction(nameof(EditEvent), new { id });
        }

        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await _api.SendAsync(HttpMethod.Delete, $"api/events/{id}");

        if (result.Ok) TempData["Success"] = "Event deleted.";
        else TempData["Error"] = result.Status == System.Net.HttpStatusCode.Conflict
            ? "This event has categories or enrolments, so it cannot be deleted."
            : result.Error;

        return RedirectToAction(nameof(Events));
    }

    private static object ToApiBody(EventFormViewModel m) => new
    {
        eventName = m.EventName,
        description = m.Description,
        eventDate = m.EventDate,
        location = m.Location,
        distance = m.Distance,
        eventType = m.EventType
    };

    // Returns null when the banner was saved or no file was chosen, otherwise a message
    private async Task<string?> UploadBannerAsync(int eventId, IFormFile? banner)
    {
        if (banner == null || banner.Length == 0) return null;

        var result = await _api.UploadAsync($"api/events/{eventId}/banner", banner);
        return result.Ok ? null : result.Error;
    }

    // ---------- Categories ----------

    [HttpGet]
    public async Task<IActionResult> Categories(int eventId)
    {
        var ev = await MyEventAsync(eventId);
        var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");

        return View(new CategoriesViewModel
        {
            Event = ev,
            Categories = categories,
            Form = new CategoryFormViewModel { EventId = eventId }
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(CategoryFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var ev = await MyEventAsync(form.EventId);
            var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{form.EventId}/categories");
            return View("Categories", new CategoriesViewModel { Event = ev, Categories = categories, Form = form });
        }

        if (form.MinAge.HasValue && form.MaxAge.HasValue && form.MinAge > form.MaxAge)
        {
            ModelState.AddModelError(nameof(form.MaxAge), "Maximum age cannot be lower than minimum age.");
            var ev = await MyEventAsync(form.EventId);
            var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{form.EventId}/categories");
            return View("Categories", new CategoriesViewModel { Event = ev, Categories = categories, Form = form });
        }

        var result = await _api.SendAsync(HttpMethod.Post, $"api/events/{form.EventId}/categories", CategoryBody(form));

        if (result.Ok) TempData["Success"] = "Category added.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Categories), new { eventId = form.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int eventId, int id)
    {
        await MyEventAsync(eventId);
        var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");
        var category = categories.FirstOrDefault(c => c.Id == id);
        if (category == null)
            throw new ApiException(System.Net.HttpStatusCode.NotFound, "No such category.");

        return View(new CategoryFormViewModel
        {
            EventId = eventId,
            Id = id,
            CategoryName = category.Name,
            Distance = category.Distance,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditCategory(CategoryFormViewModel form)
    {
        if (form.MinAge.HasValue && form.MaxAge.HasValue && form.MinAge > form.MaxAge)
            ModelState.AddModelError(nameof(form.MaxAge), "Maximum age cannot be lower than minimum age.");

        if (!ModelState.IsValid) return View(form);

        var result = await _api.SendAsync(HttpMethod.Put, $"api/categories/{form.Id}", CategoryBody(form));
        if (!result.Ok)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(form);
        }

        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Categories), new { eventId = form.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int eventId, int id)
    {
        var result = await _api.SendAsync(HttpMethod.Delete, $"api/categories/{id}");

        if (result.Ok) TempData["Success"] = "Category deleted.";
        else TempData["Error"] = result.Status == System.Net.HttpStatusCode.Conflict
            ? "People are already enrolled in this category, so it cannot be deleted."
            : result.Error;

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    private static object CategoryBody(CategoryFormViewModel f) => new
    {
        categoryName = f.CategoryName,
        distance = f.Distance,
        minAge = f.MinAge,
        maxAge = f.MaxAge
    };

    // ---------- Enrolments ----------

    public async Task<IActionResult> Enrolments(int eventId)
    {
        var ev = await MyEventAsync(eventId);
        var list = await _api.GetAsync<List<EventEnrollmentDto>>($"api/events/{eventId}/enrollments");
        return View(new EventEnrollmentsViewModel { Event = ev, Enrollments = list });
    }

    [HttpPost]
    public async Task<IActionResult> SetEnrolmentStatus(int eventId, int id, string status)
    {
        var result = await _api.SendAsync(HttpMethod.Put, $"api/enrollments/{id}/status", new { status });

        if (result.Ok) TempData["Success"] = $"Enrolment marked as {status}.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    // ---------- Results ----------

    public async Task<IActionResult> Results(int eventId)
    {
        var ev = await MyEventAsync(eventId);
        var list = await _api.GetAsync<List<EventEnrollmentDto>>($"api/events/{eventId}/enrollments");
        return View(new EventResultsViewModel { Event = ev, Enrollments = list });
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(ResultFormViewModel form)
    {
        TimeSpan? finishTime = null;

        if (form.Status == "Finished")
        {
            if (string.IsNullOrWhiteSpace(form.FinishTime) || form.Position == null)
            {
                TempData["Error"] = "A finished runner needs a finish time and a position.";
                return RedirectToAction(nameof(Results), new { eventId = form.EventId });
            }
            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values
                    .SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault()
                    ?? "Check the finish time and position.";
                return RedirectToAction(nameof(Results), new { eventId = form.EventId });
            }
            if (!TimeSpan.TryParse(form.FinishTime, CultureInfo.InvariantCulture, out var parsed))
            {
                TempData["Error"] = "Enter the finish time as hh:mm:ss, for example 00:52:14.";
                return RedirectToAction(nameof(Results), new { eventId = form.EventId });
            }
            finishTime = parsed;
        }

        var body = new
        {
            finishTime = form.Status == "Finished" ? finishTime : null,
            position = form.Status == "Finished" ? form.Position : null,
            status = form.Status
        };

        var result = form.ResultId.HasValue
            ? await _api.SendAsync(HttpMethod.Put, $"api/results/{form.ResultId}", body)
            : await _api.SendAsync(HttpMethod.Post, $"api/enrollments/{form.EnrollmentId}/results", body);

        if (result.Ok) TempData["Success"] = "Result saved.";
        else TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Results), new { eventId = form.EventId });
    }
}
