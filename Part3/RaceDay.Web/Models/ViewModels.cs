using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Choose a role.")]
    public string Role { get; set; } = "Participant";

    [Required(ErrorMessage = "Enter your first name."), StringLength(100)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your last name."), StringLength(100)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a password.")]
    [MinLength(8, ErrorMessage = "The password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type the password again.")]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    public string? Gender { get; set; }

    [Display(Name = "Date of birth")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }
}

public class EventFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Enter the event name."), StringLength(150)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Choose the event date and time.")]
    [Display(Name = "Date and time")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Enter the location."), StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "The distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal Distance { get; set; }

    [Required(ErrorMessage = "Choose the event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    public string? ExistingBannerUrl { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }
}

public class CategoryFormViewModel
{
    public int EventId { get; set; }
    public int? Id { get; set; }

    [Required(ErrorMessage = "Enter the category name."), StringLength(100)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "The distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal Distance { get; set; }

    [Range(0, 120, ErrorMessage = "Minimum age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Maximum age must be between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }
}

public class ResultFormViewModel
{
    public int EnrollmentId { get; set; }
    public int EventId { get; set; }
    public int? ResultId { get; set; }

    [Required]
    public string Status { get; set; } = "Finished";

    // hh:mm:ss
    [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Enter the finish time as hh:mm:ss, for example 00:52:14.")]
    public string? FinishTime { get; set; }

    [Range(1, 100000, ErrorMessage = "Enter a position of 1 or more.")]
    public int? Position { get; set; }
}

public class ProfileViewModel
{
    public string Role { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }

    [StringLength(100)]
    [Display(Name = "Full name")]
    public string? FullName { get; set; }

    [StringLength(100)]
    [Display(Name = "First name")]
    public string? FirstName { get; set; }

    [StringLength(100)]
    [Display(Name = "Last name")]
    public string? LastName { get; set; }

    [StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    public string? Gender { get; set; }

    [Display(Name = "Date of birth")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }
}

public class EventFilterViewModel
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public decimal? MaxDistance { get; set; }
    public bool ShowPast { get; set; }
    public List<EventDto> Events { get; set; } = new();
}

public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = null!;
    public List<CategoryDto> Categories { get; set; } = new();
    // category id -> enrolment status, for the logged-in participant
    public Dictionary<int, string> MyEnrollments { get; set; } = new();
}

public class DashboardViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public int TotalEnrollments => Events.Sum(e => e.EnrollmentCount);
    public List<EventDto> Upcoming => Events.Where(e => e.Date >= DateTime.Now).OrderBy(e => e.Date).ToList();
}

public class CategoriesViewModel
{
    public EventDto Event { get; set; } = null!;
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
}

public class EventEnrollmentsViewModel
{
    public EventDto Event { get; set; } = null!;
    public List<EventEnrollmentDto> Enrollments { get; set; } = new();
}

public class EventResultsViewModel
{
    public EventDto Event { get; set; } = null!;
    public List<EventEnrollmentDto> Enrollments { get; set; } = new();
}
