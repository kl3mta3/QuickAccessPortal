using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RemoteAccessPortal.Config;
using RemoteAccessPortal.Classes;
using RemoteAccessPortal.Dashboard;
using RemoteAccessPortal.Database;
using System.Threading.Tasks;


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

    public async Task<IActionResult> OnPost()
    {

        string usernameLower = Username.ToLower();
        string userHash = Config.HashString(usernameLower);
        string authKey = Config.HashString(usernameLower + "|" + Password);
        User user = await DatabaseManager.GetUserByUserHash(usernameLower);
        if (user != null && authKey == user.AuthKey && user.IsAdmin == true)
        {
            HttpContext.Session.SetString("IsLoggedIn", "true");

            HttpContext.Session.SetString("Username", Config.CapitalizeFirstLetter(Username));
            return RedirectToPage("/Dashboard");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }
    }

}
