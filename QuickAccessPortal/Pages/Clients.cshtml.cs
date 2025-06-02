using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using RemoteAccessPortal;
using RemoteAccessPortal.Dashboard;
using RemoteAccessPortal.Database;
using RemoteAccessPortal.Classes;
using Microsoft.Data.Sqlite;


public class ClientsModel : PageModel
{
    private readonly ILogger<ClientsModel> _logger;
    private readonly DatabaseManager _dbContext;


    public Dictionary<string, string> ClientNames { get; set; }

    public List<Client> Clients { get; set; } = new List<Client>();

    public List<Alert> CurrentAlerts { get; set; }

    [BindProperty]
    public string SelectedClient { get; set; }

    public int TotalAlerts { get; set; }


    public ClientsModel(ILogger<ClientsModel> logger, DatabaseManager dbContext)
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
        ClientNames = await DatabaseManager.GetClientDictAsync();
        Clients = await DatabaseManager.GetAllClients();
        return Page();

    }

    
    public async Task<IActionResult> OnGetClientModal2(string target)
    {
        try
        {
            var client = await DatabaseManager.GetClient(target);

            if (client == null)
                return Content("<p class='text-danger'>No data available for this client.</p>");

            var html = $@"
            <form id='clientForm'>
                <input type='text' class='form-control' id='clientNameDisplay2' value='{client.ClientName}' disabled />
                <input type='hidden' id='clientName2' value='{client.ClientName}' />
                    <div class='mb-3'>
                    <label for='kbaUrl' class='form-label'>KBA URL</label>
                    <input type='text' class='form-control' id='kbaUrl2' value='{client.KbaUrl}' />
                </div>
                <div class='mb-3'>
                    <label for='remoteLocation' class='form-label'>Remote Location</label>
                    <input type='text' class='form-control' id='remoteLocation2' value='{client.RemoteLocation}' />
                </div>
                <input type='hidden' id='addedBy2' value='{client.AddedBy}' />
                <input type='hidden' id='timestamp2' value='{client.Timestamp:yyyy-MM-ddTHH:mm:ssZ}' />

            <div class='modal-footer justify-content-between mt-4'>
                <small class='text-muted'>Originally added by: <strong>{client.AddedBy}</strong></small>
                    <div>
                    <button type='button' class='btn btn-danger me-2' onclick='deleteClient(""{client.ClientName}"")'>Delete</button>
                    <button type='button' class='btn btn-primary' onclick='saveClient()'>Save</button>
                </div>
            </div>
            </form>";

            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching client data for modal");
            return Content("<p class='text-danger'>An error occurred while fetching client data.</p>");
        }
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnDeleteClient(string name)
    {
        try
        {
            var result = await DatabaseManager.DeleteClientByName(name);
            if (result)
                return new JsonResult(new { success = true });
            else
                return StatusCode(500, "Failed to delete client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting client");
            return StatusCode(500, "Server error");
        }
    }

    
}
