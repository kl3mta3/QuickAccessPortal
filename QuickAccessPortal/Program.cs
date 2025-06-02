using RemoteAccessPortal;
using RemoteAccessPortal.Classes;
using RemoteAccessPortal.Database;
using RemoteAccessPortal.Dashboard;
using RemoteAccessPortal.Config;
using System;
using System.Net.Sockets;
using System.Net;

public class Program
{
    static async Task Main(string[] args)
    {
        await Config.ConfigureApplication();
        await DatabaseManager.Initialize();

       Dictionary<string, string> clientData = await DatabaseManager.GetClientDictAsync();
        List<Alert> currentAlerts = await DatabaseManager.GetCurrentAlerts();



        if (clientData == null || clientData.Count == 0)
        {
            await DatabaseManager.SeedRandomClients(20);
        }

        if (currentAlerts == null || currentAlerts.Count == 0)
        {
            await DatabaseManager.SeedFakeAlerts();
        }

        DatabaseManager.Clients = await DatabaseManager.GetAllClients();

        var app = WebAppDashboard.CreateWebApp(args, WebAppDashboard.WebAppPort);
        var webApp = app.RunAsync();

        var rest = WebAppDashboard.CreateWebApp(args, WebAppDashboard.ClientPort);
        var restApp = rest.RunAsync();

        Console.WriteLine("Web application is running...");
        Console.WriteLine($"WebApp IP: http://{WebAppDashboard.WebAppIP}:{WebAppDashboard.WebAppPort}");
        await webApp;
        await restApp;
    }
}