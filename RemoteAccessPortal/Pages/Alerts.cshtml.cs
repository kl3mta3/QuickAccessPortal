using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using RemoteAccessPortal;
using RemoteAccessPortal.Classes;
using RemoteAccessPortal.Database;
using RemoteAccessPortal.Dashboard;
using Microsoft.Data.Sqlite;
using System.Globalization;


public class AlertsModel : PageModel
{
    private readonly ILogger<AlertsModel> _logger;
    private readonly DatabaseManager _dbContext;


    public Dictionary<string, string> ClientNames { get; set; }

    public List<Client> Clients { get; set; } = new List<Client>();

    public List<Alert> CurrentAlerts { get; set; }

    [BindProperty]
    public string SelectedClient { get; set; }

    public int TotalAlerts { get; set; }


    public AlertsModel(ILogger<AlertsModel> logger, DatabaseManager dbContext)
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

        var alerts = await DatabaseManager.GetAllAlerts();

        bool descending = sortDir == "desc";

        CurrentAlerts = sortBy switch
        {
            "status" => descending
                ? alerts.OrderByDescending(a => a.Status).ToList()
                : alerts.OrderBy(a => a.Status).ToList(),

            "client" => descending
                ? alerts.OrderByDescending(a => a.ClientName).ToList()
                : alerts.OrderBy(a => a.ClientName).ToList(),

            "elapsed" => descending
                ? alerts.OrderByDescending(a =>
                {
                    DateTime.TryParse(a.AddedOn, out var dt);
                    return dt;
                }).ToList()
                : alerts.OrderBy(a =>
                {
                    DateTime.TryParse(a.AddedOn, out var dt);
                    return dt;
                }).ToList(),

            _ => alerts.OrderByDescending(a => a.AlertID).ToList()
        };


        TotalAlerts = CurrentAlerts.Count;
        return Page();

    }





    public async Task<IActionResult> OnGetAlertModal2(string id)
    {
        try
        {
            var alert = await DatabaseManager.GetAlertById(id);

            if (alert == null)
                return Content("<p class='text-danger'>No alert found with that ID.</p>");

            var html = $@"
        <form id='alertForm'>
            <input type='hidden' id='alertId2' value='{alert.AlertID}' />

            <div class='mb-3'>
                <label for='alertClientName' class='form-label'>Client Name</label>
                <input type='text' class='form-control' id='alertClientName2' value='{alert.ClientName}' disabled />
            </div>

            <div class='mb-3'>
                <label for='alertStatus' class='form-label'>Status</label>
                <select class='form-select' id='alertStatus2'>
                    <option value='New' {(alert.Status == "New" ? "selected" : "")}>New</option>
                    <option value='InProgress' {(alert.Status == "InProgress" ? "selected" : "")}>In Progress</option>
                    <option value='Resolved' {(alert.Status == "Resolved" ? "selected" : "")}>Resolved</option>
                </select>
            </div>

            <div class='mb-3'>
                <label for='alertResolution' class='form-label'>Resolution</label>
                <input type='text' class='form-control' id='alertResolution2' value='{alert.Resolution}' placeholder='Enter resolution notes if resolved' />
            </div>

            <div class='mb-3'>
                <label for='alertMessage' class='form-label'>Message</label>
                <textarea class='form-control' id='alertMessage2' rows='3' placeholder='Enter details or notes'>{alert.Message}</textarea>
            </div>

        <div class='modal-footer justify-content-between mt-4'>
            <small class='text-muted'>Added by: <strong>{alert.AddedBy}</strong> on {alert.AddedOn}</small>

            <div>
                <button type='button' class='btn btn-danger me-2' onclick='deleteAlert(""{alert.AlertID}"")'>Delete</button>
                <button type='button' class='btn btn-primary' onclick='resolveAlert2()'>Save Changes</button>
            </div>
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
    public async Task<IActionResult> OnDeleteCurrentAlert(string id)
    {
        try
        {
            var result = await DatabaseManager.DeleteAlertById(id);
            if (result)
                return new JsonResult(new { success = true });
            else
                return StatusCode(500, "Failed to delete alert");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting alert");
            return StatusCode(500, "Server error");
        }
    }

}
