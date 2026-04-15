using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class SocketClient
{
    public static int Main(string[] args)
    {
        StartClient();
        return 0;
    }
    private static void StartClient()
    {
        try
        {
            Console.WriteLine("Starting client...");
            Console.Write("Enter an ip address to connect to (default is 10.0.0.131): ");
            string ipAddress = Console.ReadLine();

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = "10.0.0.131";
            }
            Console.WriteLine("Using IP address: {0}", ipAddress);
            IPAddress ip = IPAddress.Parse(ipAddress);
            IPEndPoint localEP = new IPEndPoint(ip, 10001);
            IPEndPoint remoteEP = new IPEndPoint(ip, 10001);

            Socket sender = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine("-------------------------------");
            try
            {
                sender.Connect(remoteEP);

                while (true)
                {
                    string input = null;
                    string[] response = null;
                    int bytesRec;
                    int bytesSent;

                    Console.Write("[L]ogin or [S]ignup : ");
                    input = Console.ReadLine().ToUpper();
                    Send(sender, input);
       
                    // Login
                    if (input == "L")
                    {
                        Console.Write("Enter account number: ");
                        string accountNumber = Console.ReadLine();

                        Console.Write("Enter password: ");
                        string password = Console.ReadLine();

                        response = Send(sender, $"LOGIN|{accountNumber}|{password}");
                        
                        if (response[0] == "SUCCESS")
                        {
                            Console.WriteLine("Welcome, {0}!", response[1]);
                        }
                        else
                        {
                            Console.WriteLine("Login failed: {0}", response[0]);
                        }
                    }

                    // Sign-up
                    else if (input == "S")
                    {
                        Console.Write("Enter a reference number from an existing account: ");
                        string refNumber = Console.ReadLine();

                        Console.Write("Choose a password: ");
                        string password = Console.ReadLine();

                        input = $"CREATE|{refNumber}|{password}";
                    }

                    // Kill
                    if (input == "kill")
                    {
                        Console.WriteLine("Closing connection...");

                        sender.Shutdown(SocketShutdown.Both);
                        sender.Close();
                        break;
                    }
                }


            }
            // Null / Set Value Errors
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException: {0}", ane.ToString());
            }
            // Socket Issues
            catch (SocketException se)
            {
                Console.WriteLine("SocketException: {0}", se.ToString());
            }
            // Catch Exceptions
            catch (Exception e)
            {
                Console.WriteLine("Unexpected Exception: {0}", e.ToString());
            }
        }
        // Catches Connection Errors
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
    static string[] Send(Socket sender, string command)
    {
        byte[] msg = Encoding.ASCII.GetBytes(command + "<EOF>");
        sender.Send(msg);

        string data = "";
        while (true)
        {
            byte[] buffer = new byte[1024];
            int bytesRec = sender.Receive(buffer);
            data += Encoding.ASCII.GetString(buffer, 0, bytesRec);
            if (data.IndexOf("<EOF>") > -1)
            {
                data = data.Substring(0, data.Length - 5);
                break;
            }
        }

        return data.Split('|');
    }
}