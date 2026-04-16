using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

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

                    if (data == "") break;

                    Console.WriteLine("Received: {0}", data);

                    string[] parts = Receieved(data);
                    string command = parts[0];

                    // LOGIN
                    if (command == "LOGIN")
                    {
                        if (!HasParts(parts, 3))
                        {
                            response = "ERROR|Invalid command format.";
                        }
                        else
                        {
                            string accountNumber = parts[1];
                            string password = parts[2];

                            if (!IsValid(accountNumber, password))
                            {
                                response = "ERROR|Invalid or missing fields.";
                            }
                            else
                            {
                                Account account = accounts.FirstOrDefault(a => a.AccountNumber == accountNumber && a.Password == password);
                               
                                if (account != null)
                                {
                                    response = $"SUCCESS|{account.AccountNumber}|{account.Balance}";
                                }
                                else
                                {
                                    response = "ERROR|Invalid account number or password.";
                                }
                            }
                        }
                    }

                    // SIGNUP
                    else if (command == "SIGNUP")
                    {
                        if (!HasParts(parts, 3))
                        {
                            response = "ERROR|Invalid command format.";
                        }
                        else
                        {
                            string referenceNumber = parts[1];
                            string password = parts[2];

                            if (!IsValid(referenceNumber, password))
                            {
                                response = "ERROR|Invalid or missing fields.";
                            }
                            else
                            {
                                Account existing = accounts.FirstOrDefault(a => a.ReferenceNumber == referenceNumber);

                                if (existing == null)
                                {
                                    response = "ERROR|Invalid reference number.";
                                }
                                else
                                {
                                    string newAccountNumber;
                                    do { newAccountNumber = new Random().Next(100000000, 999999999).ToString(); }
                                    while (accounts.Any(a => a.AccountNumber == newAccountNumber));

                                    string newReferenceNumber;
                                    do { newReferenceNumber = GenerateCode(7); }
                                    while (accounts.Any(a => a.ReferenceNumber == newReferenceNumber));

                                    accounts.Add(new Account
                                    {
                                        AccountNumber = newAccountNumber,
                                        Password = password,
                                        ReferenceNumber = newReferenceNumber,
                                        Balance = 0.00
                                    });

                                    SaveAccounts(accounts);
                                    response = $"SUCCESS|{newAccountNumber}|{newReferenceNumber}";
                                }
                            }
                        }
                    }

                    // DEPOSIT
                    else if (command == "DEPOSIT")
                    {
                        if (!HasParts(parts, 4))
                        {
                            response = "ERROR|Invalid command format.";
                        }
                        else if (!double.TryParse(parts[3], out double amount))
                        {
                            response = "ERROR|Amount must be a valid number.";
                        }
                        else
                        {
                            string accountNumber = parts[1];
                            string chequeNumber = parts[2];

                            if (!IsValid(accountNumber, chequeNumber, parts[3]))
                            {
                                response = "ERROR|Invalid or missing fields.";
                            }
                            else if (chequeNumber.Length != 6)
                            {
                                response = "ERROR|Cheque number must be 6 characters.";
                            }
                            else
                            {
                                Account account = accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);
                                
                                if (account == null)
                                {
                                    response = "ERROR|Account not found.";
                                }
                                else
                                {
                                    account.Balance += amount;
                                    SaveAccounts(accounts);
                                    response = $"SUCCESS|Deposit complete.|{account.Balance}";
                                }
                            }
                        }
                    }

                    // BALANCE
                    else if (command == "BALANCE")
                    {
                        if (!HasParts(parts, 2))
                        {
                            response = "ERROR|Invalid command format.";
                        }
                        else
                        {
                            string accountNumber = parts[1];

                            if (!IsValid(accountNumber))
                            {
                                response = "ERROR|Invalid or missing fields.";
                            }
                            else
                            {
                                Account account = accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);
                                
                                if (account != null)
                                {
                                    response = $"SUCCESS|{account.Balance}";
                                }
                                else
                                {
                                    response = "ERROR|Account not found.";
                                }
                            }
                        }
                    }

                    // WITHDRAW
                    else if (command == "WITHDRAW")
                    {
                        if (!HasParts(parts, 3))
                        {
                            response = "ERROR|Invalid command format.";
                        }
                        else if (!double.TryParse(parts[2], out double amount))
                        {
                            response = "ERROR|Amount must be a valid number.";
                        }
                        else
                        {
                            string accountNumber = parts[1];

                            if (!IsValid(accountNumber, parts[2]))
                            {
                                response = "ERROR|Invalid or missing fields.";
                            }
                            else
                            {
                                Account account = accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);
                                if (account == null)
                                    response = "ERROR|Account not found.";
                                else if (amount > account.Balance)
                                    response = "ERROR|Insufficient funds.";
                                else
                                {
                                    account.Balance -= amount;
                                    SaveAccounts(accounts);
                                    response = $"SUCCESS|Withdrawal complete.|{account.Balance}";
                                }
                            }
                        }
                    }

                    // TRANSFER
                    else if (command == "TRANSFER")
                    {
                        if (!HasParts(parts, 4))
                        {
                            response = "ERROR|Invalid command format.";
                        }
                        else if (!double.TryParse(parts[3], out double amount))
                        {
                            response = "ERROR|Amount must be a valid number.";
                        }
                        else
                        {
                            string accountNumber = parts[1];
                            string recipientAccountNumber = parts[2];

                            if (!IsValid(accountNumber, recipientAccountNumber, parts[3]))
                            {
                                response = "ERROR|Invalid or missing fields.";
                            }
                            
                            else
                            {
                                Account account = accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);
                                Account recipient = accounts.FirstOrDefault(a => a.AccountNumber == recipientAccountNumber);

                                if (account == null)
                                {
                                    response = "ERROR|Account not found.";

                                }

                                else if (recipient == null)
                                {
                                    response = "ERROR|Recipient account not found.";
                                }

                                else if (amount > account.Balance)
                                {
                                    response = "ERROR|Insufficient funds.";

                                }

                                else
                                {
                                    account.Balance -= amount;
                                    recipient.Balance += amount;
                                    SaveAccounts(accounts);
                                    response = $"SUCCESS|{account.Balance}";
                                }
                            }
                        }
                    }

                    else
                    {
                        response = "ERROR|Unknown command.";
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

    static bool HasParts(string[] parts, int expected)
    {
        return parts.Length >= expected;
    }

    static string[] Receieved(string command)
    {
        if (string.IsNullOrEmpty(command))
        {
            return new string[] { "ERROR", "No command received." };

        }

        return command.Split('|');
    }

    static bool IsValid(params object[] values)
    {
        foreach (object value in values)
        {
            if (value == null) return false;

            if (value is string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return false;

                if (double.TryParse(s, out double parsed))
                    if (parsed <= 0) return false;
            }
        }
        return true;
    }

    static string GenerateCode(int length)
    {
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        Random random = new Random();
        string result = "";
        for (int i = 0; i < length; i++)
            result += chars[random.Next(chars.Length)];
        return result;
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