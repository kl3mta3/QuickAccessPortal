using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using RemoteAccessPortal;
using RemoteAccessPortal.Dashboard;
using RemoteAccessPortal.Database;
using RemoteAccessPortal.Classes;
using RemoteAccessPortal.Config;
using Microsoft.Data.Sqlite;


public class UsersModel : PageModel
{
    private readonly ILogger<UsersModel> _logger;
    private readonly DatabaseManager _dbContext;

    public List<User> Users { get; set; } = new List<User>();

    [BindProperty]
    public string SelectedUser { get; set; }

    public int TotalAlerts { get; set; }


    public UsersModel(ILogger<UsersModel> logger, DatabaseManager dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> OnGet()
    {
        var loggedIn = HttpContext.Session.GetString("IsLoggedIn");

        if (loggedIn != "true")
        {
            return RedirectToPage("/Index");
        }

        Users = await DatabaseManager.GetAllUsers();

        return Page();

    }


    public async Task<IActionResult> OnGetUserModal(string username)
    {
        try
        {
            string userHash = Config.HashString(username);
            var user = await DatabaseManager.GetUserByUserHash(userHash);
            string originalUsername = username;
            if (user == null)
                return Content("<p class='text-danger'>No data available for this client.</p>");

            var html = $@"
            <form id='userForm'>
                <div class='mb-3'>
                    <label for='name' class='form-label'>Full Name</label>
                    <input type='text' class='form-control' id='name' value='{user.Name}' />
                </div>

                    <div class='mb-3'>
                    <label for='username' class='form-label'>Username</label>
                    <input type='text' class='form-control' id='username' value='{user.Username}' />
                    <input type='hidden' id='originalUsername' value='{originalUsername}' />
                </div>

                <div class='form-check mb-3'>
                    <input class='form-check-input' type='checkbox' id='isAdmin' {(user.IsAdmin ? "checked" : "")}>
                    <label class='form-check-label' for='isAdmin'>Is Admin</label>
                </div>

            <div class='modal-footer justify-content-between mt-4'>
                    <div>
                    <button type='button' class='btn btn-danger me-2' onclick='deleteUser(""{user.Username}"")'>Delete</button>
                    <button type='button' class='btn btn-danger me-2' onclick='changePassword(""{user.Username}"")'>Change Password</button>
                    <button type='button' class='btn btn-primary' onclick='saveUser()'>Save</button>
                </div>
            </div>
            </form>";

            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user data for modal");
            return Content("<p class='text-danger'>An error occurred while fetching user data.</p>");
        }
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostDeleteCurrentUser(string username)
    {
        try
        {
            var result = await DatabaseManager.DeleteUserByUsername(username);
            if (result)
                return new JsonResult(new { success = true });
            else
                return StatusCode(500, "Failed to delete user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            return StatusCode(500, "Server error");
        }
    }



    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostChangePassword([FromBody] PasswordChangeRequest data)
    {
        try
        {
            Console.WriteLine($"Changing password for user: {data.Username}");
            Console.WriteLine($"New Password: {data.NewPassword}");

            if (!Regex.IsMatch(data.NewPassword, "[A-Z]"))
                return StatusCode(400, "Password must contain at least one uppercase letter.");
            if (!Regex.IsMatch(data.NewPassword, "[a-z]"))
                return StatusCode(400, "Password must contain at least one lowercase letter.");
            if (!Regex.IsMatch(data.NewPassword, "[0-9]"))
                return StatusCode(400, "Password must contain at least one number.");
            if (data.NewPassword.Length < 8)
                return StatusCode(400, "Password must be at least 8 characters long.");
            if (data.NewPassword.Length > 24)
                return StatusCode(400, "Password must be less than 24 characters long.");

            var result = await DatabaseManager.UpdateUserPassword(data.Username, data.NewPassword);

            if (result)
                return new JsonResult(new { success = true });
            else
                return StatusCode(500, "Failed to update Password");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error Updateing Password for user");
            return StatusCode(500, "Server error");
        }
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostSaveUser([FromBody] User user, string originalUsername)
    {

        if (user == null)
        {
            return BadRequest("User data not parsed.");
        }

        await DatabaseManager.UpdateUser(user, originalUsername);

        return new JsonResult(new { success = true });

    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostAddUser([FromBody] NewUserRequest payload)
    {
        User data = payload.User;
        string password = payload.Password;

        if (data == null || string.IsNullOrEmpty(data.Name) || string.IsNullOrEmpty(data.Username) || string.IsNullOrEmpty(password))
        {
            return BadRequest("User data not parsed.");
        }
        if (!Regex.IsMatch(password, "[A-Z]"))
            return StatusCode(400, "Password must contain at least one uppercase letter.");
        if (!Regex.IsMatch(password, "[a-z]"))
            return StatusCode(400, "Password must contain at least one lowercase letter.");
        if (!Regex.IsMatch(password, "[0-9]"))
            return StatusCode(400, "Password must contain at least one number.");
        if (password.Length < 8)
            return StatusCode(400, "Password must be at least 8 characters long.");
        if (password.Length > 24)
            return StatusCode(400, "Password must be less than 24 characters long.");
        data.UserHash = Config.HashString(data.Username);
        data.PasswordHash = Config.HashPassword(password);
        data.AuthKey = Config.HashString(data.Username + "|" + password);

        await DatabaseManager.InsertUser(data);
        return new JsonResult(new { success = true });
    }

  
}
