
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class CategoryRequest
{
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.1, 1000)]
    public decimal Distance { get; set; }

    [Range(0, 120)]
    public int? MinAge { get; set; }

    [Range(0, 120)]
    public int? MaxAge { get; set; }
}