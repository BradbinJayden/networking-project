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

            List<Account> accounts = LoadAccounts();

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
                            data = data.Replace("<EOF>", "");
                            break;
                        }
                    }
                    
                    if (Receieved(data)[0] == "LOGIN") 
                    {
                        Console.WriteLine(data);
                        Account account = accounts.FirstOrDefault(a => a.AccountNumber == Receieved(data)[1] && a.Password == Receieved(data)[2]);
                        if (account != null)
                        {
                            response = "SUCCESS|" + account.AccountNumber + "|" + account.Balance;
                        }
                        else
                        {
                            response = "FAILURE";
                        }
                    }

                    else if (Receieved(data)[0] == "DEPOSIT")
                    {
                        Console.WriteLine(data);
                        string accountNumber = Receieved(data)[1];
                        string chequeNumber = Receieved(data)[2];
                        double amount = double.Parse(Receieved(data)[3]);

                        Account account = accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);

                        if (account == null)
                        {
                            response = "ERROR|Account not found.";
                        }
                        else if (amount <= 0)
                        {
                            response = "ERROR|Deposit amount must be greater than zero.";
                        }
                        else
                        {
                            account.Balance += amount;
                            SaveAccounts(accounts);
                            response = $"SUCCESS|Deposit complete.|{account.Balance}";
                        }
                    }

                    else if (Receieved(data)[0] == "BALANCE")
                    {
                        Console.WriteLine(data);
                        string accountNumber = Receieved(data)[1];

                        Account account = accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);

                        if (account == null)
                        {
                            response = "ERROR|Account not found.";
                        }
                        else if (account.Balance == null)
                        {
                            response = "ERROR|Error fetching balance.";
                        }
                        else
                        {
                            response = $"SUCCESS|{account.Balance}";
                        }
                    }

                    Console.WriteLine("Received : {0}", data);

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
        {
            Console.WriteLine("No Account Data Found!");
            return new List<Account>();
        }

        string json = File.ReadAllText("accounts.json");
        return JsonSerializer.Deserialize<List<Account>>(json);
    }

    private static void SaveAccounts(List<Account> accounts)
    {
        string json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("accounts.json", json);
    }
}