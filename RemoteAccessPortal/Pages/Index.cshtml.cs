using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RemoteAccessPortal.Config;


public class IndexModel : PageModel
{


    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    [BindProperty]
    public string Username { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public void OnGet()
    {

    }

    public IActionResult OnPost()
    {
        if (Username == Config.AdminUsername && Config.HashPassword(Password).SequenceEqual(Config.AdminPassword))
        {
            HttpContext.Session.SetString("IsLoggedIn", "true");
            return RedirectToPage("/Dashboard"); 
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page(); 
        }
    }

}
