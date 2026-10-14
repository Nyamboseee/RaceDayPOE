
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class ResultRequest
{
    // Format hh:mm:ss, e.g. 00:52:14. Not needed for DNF or DNS.
    public TimeSpan? FinishTime { get; set; }

    [Range(1, 100000)]
    public int? Position { get; set; }

    [Required]
    public string Status { get; set; } = "Finished"; // Finished, DNF or DNS
}