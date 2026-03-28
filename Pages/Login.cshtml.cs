using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ThesisWebApp.Pages;

public class LoginModel : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        // Ovde će kasnije ići prava logika za proveru korisnika.
        // Za sada samo se vratimo na početnu stranicu.
        if (!ModelState.IsValid)
        {
            return Page();
        }

        return RedirectToPage("/Index");
    }
}

public class LoginInput
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

