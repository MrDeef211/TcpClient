using System.Net;
using System.Net.Sockets;
using System.Text;
using Client = System.Net.Sockets.TcpClient;

namespace TcpClient
{
    internal class Program
    {
        private const int ConectTimeout = 3;

        private const int Port = 8888;

        private const string BroadcastIp = "10.1.1.";

        static async Task Main(string[] args)
        {
            // Поиск и подключение
            #region 

            var users = await GetActiveUsersAsync();

            IPEndPoint server;

            if (users.Count == 0)
            {
                Console.WriteLine("Нет доступных пользователей для подключения");
                return;
            }

            Console.WriteLine("Доступные пользователи:");

            foreach (var user in users)
            {
                Console.WriteLine(user.Address.AddressFamily.ToString());
            }

            Console.WriteLine("\nВведите желаемого пользователя для подключения:");

            while (true)
            {
                var ip = Console.ReadLine();

                server = users.Where(x => x.Address.ToString() == ip).First();

                if (server != null)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Адресс не найден");
                }
            }

            using var client = new Client(server);
            using NetworkStream stream = client.GetStream();

            #endregion

            // Работа
            #region

            string? message = "";
            ValueTask sending = ValueTask.CompletedTask;

            while (!"!exit".StartsWith(message ?? "null") || message == "")
            {
                message = Console.ReadLine();
                if (message == null || message == "")
                    continue;
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                await sending;
                sending = stream.WriteAsync(bytes);
            }

            #endregion
        }

        private static async Task<List<IPEndPoint>> GetActiveUsersAsync()
        {
            List<Task<bool>> tasks = new List<Task<bool>>();
            for (int i = 0; i < 256; i++)
            {
                tasks.Add(TryConnectAsync(new(IPAddress.Parse($"{BroadcastIp}{i}"), Port)));
            }
            bool[] success = await Task.WhenAll(tasks);

            List<IPEndPoint> activeUsers = new List<IPEndPoint>();
            for (int i = 0; i < success.Length; i++)
            {
                if (!success[i])
                    continue;

                var myIpAddresses = Dns.GetHostAddresses(Dns.GetHostName());
                IPAddress currentIp = IPAddress.Parse($"{BroadcastIp}{i}");
                bool isNotMyIp = !IPAddress.IsLoopback(currentIp) 
                    && !myIpAddresses.Any(addr => addr.ToString() == currentIp.ToString());

                if (isNotMyIp)
                {
                    activeUsers.Add(new IPEndPoint(IPAddress.Parse($"{BroadcastIp}{i}"), Port));
                }
            }

            return activeUsers;
        }

        private static async Task<bool> TryConnectAsync(IPEndPoint ip)
        {
            try
            {
                using CancellationTokenSource cts = new(
                    TimeSpan.FromSeconds(ConectTimeout));

                using Client client = new();
                await client.ConnectAsync(ip, cts.Token).ConfigureAwait(false);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
