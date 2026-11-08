using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    // Send each person to their own home page, or to the login page
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction("Login", "Account");

        return User.IsInRole("Organizer")
            ? RedirectToAction("Dashboard", "Organizer")
            : RedirectToAction("Index", "Events");
    }

    public IActionResult Problem(string? kind)
    {
        var (title, message) = kind switch
        {
            "api-down" => ("We cannot reach RaceDay right now",
                           "The RaceDay service is not responding. Please try again in a moment."),
            "forbidden" => ("You do not have access to that",
                            "That page or action is not available for your account."),
            "notfound" => ("We could not find that",
                           "The item you asked for does not exist or was removed."),
            _ => ("Something went wrong",
                  "That did not work. Please go back and try again.")
        };

        ViewData["Title"] = title;
        ViewData["Message"] = message;
        return View();
    }
}
