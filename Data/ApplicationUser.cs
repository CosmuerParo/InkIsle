using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace InkIsle.Data;

public class ApplicationUser : IdentityUser
{
    [MaxLength(80)]
    public string DisplayName { get; set; } = "墨屿主人";
}
