using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace markly.ViewModels;

public class EditProfileViewModel
{
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [Display(Name = "Bio")]
    [MaxLength(500)]
    public string? Bio { get; set; }

    [Display(Name = "Profile Picture")]
    public IFormFile? ProfilePicture { get; set; }
    
    public string? CurrentProfilePictureUrl { get; set; }
}
