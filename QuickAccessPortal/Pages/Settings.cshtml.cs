using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using RemoteAccessPortal;
using RemoteAccessPortal.Classes;
using RemoteAccessPortal.Database;
using RemoteAccessPortal.Dashboard;
using Microsoft.Data.Sqlite;
using System.Globalization;


public class SettingsModel : PageModel
{
    private readonly ILogger<SettingsModel> _logger;
    private readonly DatabaseManager _dbContext;

    [BindProperty]
    public string SelectedUser { get; set; }



    public SettingsModel(ILogger<SettingsModel> logger, DatabaseManager dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> OnGet(string sortBy, string sortDir = "asc")
    {
        var loggedIn = HttpContext.Session.GetString("IsLoggedIn");

        if (loggedIn != "true")
        {
            return RedirectToPage("/Index");
        }

       
        return Page();

    }



}
