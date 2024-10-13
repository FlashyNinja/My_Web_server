using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace My_Web_server.Server;

/// <summary>
  /// A lean and mean web server.
/// </summary>
public static class Server
{
    private static HttpListener listener;
    public static int maxSimultaneosConnections = 20;
    private static Semaphore sem = new Semaphore(maxSimultaneosConnections, maxSimultaneosConnections);

    ///<summary>
        ///Assuming server is working in intranet getting list of local IPs
    ///<summary>
    private static List<IPAddress> GetLocalHostIPs()
    {
        IPHostEntry host;
        host = Dns.GetHostEntry(Dns.GetHostName());
        var localIPList = host.AddressList.Where(x=>x.AddressFamily == AddressFamily.InterNetwork).ToList();

        return localIPList;
    }

    private static HttpListener InitializeHttpListner(List<IPAddress> localhostIPs){
        HttpListener httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost/");

        //Listen to IP addresses as well
        localhostIPs.ForEach(ip =>
        {
            Console.WriteLine("Listening on IP " + "http://" + ip.ToString() + "/");
            listener.Prefixes.Add("http://" + ip.ToString() + "/");
        });

        return listener;
    }

    ///<summary>
        ///Start listening in a separate worker thread
    ///<summary>
    private static void Start(HttpListener listener)
    {
        listener.Start();
        Task.Run(()=>RunServer(listener));
    }

    /// <summary>
        /// Start awaiting for connections, up to the "maxSimultaneousConnections" value.
        /// This code runs in a separate thread.
    /// </summary>
    private static void RunServer(HttpListener listener)
    {
        while (true)
        {
            sem.WaitOne();
            StartConnectionListener(listener);
        }
    }

    /// <summary>
        /// Await connections.
    /// </summary>
    private static async void StartConnectionListener(HttpListener listener)
    {
        // Wait for a connection. Return to caller while we wait.
        HttpListenerContext context = await listener.GetContextAsync();

        // Release the semaphore so that another listener can be immediately started up.
        sem.Release();

        // We have a connection, do something...
        DoSomething(context);
    }

    private static void DoSomething(HttpListenerContext context){
        string response = "Hello Browser!";
        byte[] encoded = Encoding.UTF8.GetBytes(response);
        context.Response.ContentLength64 = encoded.Length;
        context.Response.OutputStream.Write(encoded, 0, encoded.Length);
        context.Response.OutputStream.Close();
    }

    /// <summary>
    /// Starts the web server.
    /// </summary>
    public static void Start()
    {
        List<IPAddress> localHostIPs = GetLocalHostIPs();
        HttpListener listener = InitializeHttpListner(localHostIPs);
        Start(listener);
    }
}
