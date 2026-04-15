using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class SocketListener
{
    // Account Class
    public class Account
    {
        public string AccountNumber { get; set; }
        public string Password { get; set; }
        public string ReferenceNumber { get; set; }
        public double Balance { get; set; }
    }

    // Server Code
    public static int Main(string[] args)
    {
        StartServer();
        return 0;
    }

    public static void StartServer()
    {
        try
        {
            Console.WriteLine("Starting server...");
            IPAddress ip = IPAddress.Parse("10.0.0.131");
            IPEndPoint localEP = new IPEndPoint(ip, 10001);

            Socket listener = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(localEP);
            listener.Listen(10);

            while (true)
            {
                Socket handler = listener.Accept();
                Console.WriteLine("Client connected.");
                Console.WriteLine("------------------------------");
                while (true)
                {
                    string data = "";
                    byte[] bytes = null;
                    string response = null;

                    while (true)
                    {
                        bytes = new byte[1024];
                        int bytesRec = handler.Receive(bytes);



                        // Disconnect
                        if (bytesRec == 0)
                        {
                            Console.WriteLine("Client disconnected.");
                            handler.Close();
                            break;
                        }

                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                        if (data.IndexOf("<EOF>") > -1)
                        {
                            break;
                        }
                    }

                    List<Account> accounts = LoadAccounts();

                    // Recieved Blank
                    if (data == "")
                    {
                        break;
                    }

                    // Recieved Login
                    

                    Console.WriteLine("Received : {0}", data);


                    if (response == null)
                    {
                        response = "";
                    }

                    byte[] msg = Encoding.ASCII.GetBytes(response + "<EOF>");
                    handler.Send(msg);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    static string[] Receieved(string command)
    {
        return command.Split('|');
    }

    private static List<Account> LoadAccounts()
    {
        if (!File.Exists("accounts.json"))
            return new List<Account>();

        string json = File.ReadAllText("accounts.json");
        return JsonSerializer.Deserialize<List<Account>>(json);
    }
}