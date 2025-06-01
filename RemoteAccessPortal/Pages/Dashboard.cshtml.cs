using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using RemoteAccessPortal;
using RemoteAccessPortal.Dashboard;
using RemoteAccessPortal.Database;
using RemoteAccessPortal.Classes;
using Microsoft.Data.Sqlite;


public class DashboardModel : PageModel
{
    private readonly ILogger<DashboardModel> _logger;
    private readonly DatabaseManager _dbContext;


    public Dictionary<string, string> ClientNames { get; set; }

    public List<Client> Clients { get; set; } = new List<Client>();

    public List<Alert> CurrentAlerts { get; set; }

    [BindProperty]
    public string SelectedClient { get; set; }

    public int TotalAlerts { get; set; }


    public DashboardModel(ILogger<DashboardModel> logger, DatabaseManager dbContext)
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
        CurrentAlerts = await DatabaseManager.GetCurrentAlerts();
        Clients = await DatabaseManager.GetAllClients();
        TotalAlerts = CurrentAlerts.Count;
        return Page();

    }

    
    public async Task<IActionResult> OnGetClientModal(string target)
    {
        try
        {
            var client = await DatabaseManager.GetClient(target);

            if (client == null)
                return Content("<p class='text-danger'>No data available for this client.</p>");

            var html = $@"
            <form id='clientForm'>
                <input type='text' class='form-control' id='clientNameDisplay' value='{client.ClientName}' disabled />
                <input type='hidden' id='clientName' value='{client.ClientName}' />
                    <div class='mb-3'>
                    <label for='kbaUrl' class='form-label'>KBA URL</label>
                    <input type='text' class='form-control' id='kbaUrl' value='{client.KbaUrl}' />
                </div>
                <div class='mb-3'>
                    <label for='remoteLocation' class='form-label'>Remote Location</label>
                    <input type='text' class='form-control' id='remoteLocation' value='{client.RemoteLocation}' />
                </div>
                <input type='hidden' id='addedBy' value='{client.AddedBy}' />
                <input type='hidden' id='timestamp' value='{client.Timestamp:yyyy-MM-ddTHH:mm:ssZ}' />
                <div class='modal-footer justify-content-between mt-4'>
                    <small class='text-muted'>Originally added by: <strong>{client.AddedBy}</strong></small>
                    <button type='button' class='btn btn-primary' onclick='saveClient()'>Save</button>
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
    public async Task<IActionResult> OnPostSaveClient([FromBody] Client client)
    {

        if (client == null)
        {
            return BadRequest("Client data not parsed.");
        }

        await DatabaseManager.UpdateClient(client);
        return new JsonResult(new { success = true });

    }


    public async Task<IActionResult> OnGetAlertModal(string id)
    {
        try
        {
            var alert = await DatabaseManager.GetAlertById(id);

            if (alert == null)
                return Content("<p class='text-danger'>No alert found with that ID.</p>");

            var html = $@"
        <form id='alertForm'>
            <input type='hidden' id='alertId' value='{alert.AlertID}' />

            <div class='mb-3'>
                <label for='alertClientName' class='form-label'>Client Name</label>
                <input type='text' class='form-control' id='alertClientName' value='{alert.ClientName}' disabled />
            </div>

            <div class='mb-3'>
                <label for='alertStatus' class='form-label'>Status</label>
                <select class='form-select' id='alertStatus'>
                    <option value='New' {(alert.Status == "New" ? "selected" : "")}>New</option>
                    <option value='InProgress' {(alert.Status == "InProgress" ? "selected" : "")}>In Progress</option>
                    <option value='Resolved' {(alert.Status == "Resolved" ? "selected" : "")}>Resolved</option>
                </select>
            </div>

            <div class='mb-3'>
                <label for='alertResolution' class='form-label'>Resolution</label>
                <input type='text' class='form-control' id='alertResolution' value='{alert.Resolution}' placeholder='Enter resolution notes if resolved' />
            </div>

            <div class='mb-3'>
                <label for='alertMessage' class='form-label'>Message</label>
                <textarea class='form-control' id='alertMessage' rows='3' placeholder='Enter details or notes'>{alert.Message}</textarea>
            </div>

            <div class='modal-footer justify-content-between mt-4'>
                <small class='text-muted'>Added by: <strong>{alert.AddedBy}</strong> on {alert.AddedOn}</small>
                <button type='button' class='btn btn-primary' onclick='resolveAlert()'>Save Changes</button>
            </div>
        </form>";

            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching alert data for modal");
            return Content("<p class='text-danger'>An error occurred while fetching alert data.</p>");
        }
    }


    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostSaveAlert([FromBody] Alert alert)
    {

        if (alert == null)
        {
            return BadRequest("Alert data not parsed.");
        }

        await DatabaseManager.UpdateAlert(alert);
        return new JsonResult(new { success = true });

    }



    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostAddClientAsync([FromBody] NewClientRequest data)
    {
    
        if (data == null || string.IsNullOrEmpty(data.ClientName) || string.IsNullOrEmpty(data.AddedBy))
        {
            return BadRequest("Client data not parsed.");
        }

        await DatabaseManager.InsertClient(data.ClientName, data.AddedBy, data.KbaUrl, data.RemoteLocation);
        return new JsonResult(new { success = true });
    }
}
