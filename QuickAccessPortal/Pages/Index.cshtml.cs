using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RemoteAccessPortal.Config;
using RemoteAccessPortal.Classes;
using RemoteAccessPortal.Dashboard;
using RemoteAccessPortal.Database;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;


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
        User user = await DatabaseManager.GetUserByUserHash(userHash);
        if (user != null && userHash==user.UserHash)
        {
            string prehashedPass = Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(Password)));

            if (!Config.VerifyPassword(prehashedPass, user.PasswordHash) || user.IsAdmin != true)
            {
                ModelState.AddModelError(string.Empty, "Invalid Password or UserName.");
                return Page();
            }
            HttpContext.Session.SetString("IsLoggedIn", "true");

            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Name", user.Name);
            string firstName = user.Name.Split(' ')[0];
            HttpContext.Session.SetString("FirstName", firstName);
            return RedirectToPage("/Dashboard");
        }
        else if(user==null)
        {
            ModelState.AddModelError(string.Empty, "User is Null");
            Console.WriteLine($"user is Null");
            return Page();

        }
         else
        {
            ModelState.AddModelError(string.Empty, "User not found.");
            return Page();
        }
    }

}
