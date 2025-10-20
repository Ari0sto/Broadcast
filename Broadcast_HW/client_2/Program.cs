using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client2_M
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Client client = new Client("127.0.0.1", 8888, 2);

            await client.ConnectAsync();  // Подключаемся к серверу

            //Console.WriteLine("\nНажмите любую клавишу для отключения...");
            //Console.ReadKey();

            client.Disconnect();  // Отключаемся от сервера
        }
    }
}