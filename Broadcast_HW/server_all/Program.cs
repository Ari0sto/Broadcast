using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server_M
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Server server = new Server("127.0.0.1", 8888);

            // Запускаем сервер в отдельной задаче
            Task serverTask = server.StartAsync();

            Console.WriteLine("Нажмите любую клавишу для остановки сервера...");
            Console.ReadKey();

            server.Stop();  // Останавливаем сервер
            await Task.Delay(500);  // pause
        }
    }
}