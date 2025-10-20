using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client1_M
{
    class Client
    {
        private TcpClient tcpClient;  // TCP-клиент для соединения
        private NetworkStream stream;  // Поток для обмена данными
        private string serverIp;  // IP-адрес сервера
        private int serverPort;  // Порт сервера
        public int ClientNumber { get; set; }  // Номер клиента для идентификации
        private bool isConnected = false;  // Флаг подключения

        public Client(string ip, int port, int clientNumber)
        {
            serverIp = ip;
            serverPort = port;
            ClientNumber = clientNumber;
            tcpClient = new TcpClient();  // Создаём экземпляр TCP-клиента
        }

        // Подкл. к серверу
        public async Task ConnectAsync()
        {
            try
            {
                Console.WriteLine($"Клиент #{ClientNumber}: Подключение к серверу {serverIp}:{serverPort}...");

                await tcpClient.ConnectAsync(serverIp, serverPort);
                stream = tcpClient.GetStream();
                isConnected = true;

                Console.WriteLine($"Клиент #{ClientNumber}: Успешно подключён!");

                // Параллельные чтение и отправка
                Task receiveTask = ReceiveMessagesAsync();  // Task 1: Получение сообщений
                Task sendTask = SendMessagesAsync();  // Task 2: Отправка сообщений

                // Ждём завершения любой из задач (когда пользователь введёт "exit")
                await Task.WhenAny(receiveTask, sendTask);

                isConnected = false;

                // Stream data
                //NetworkStream stream = tcpClient.GetStream();
                //byte[] buffer = new byte[1024]; // buffer data

                // READ data
                //int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                // Преобраз. байты в строку
                //string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                //Console.WriteLine($"Клиент #{ClientNumber}: Получено сообщение: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Клиент #{ClientNumber}: Ошибка подключения: {ex.Message}");
            }
        }

        // Task 1 получение сообщений от сервера
        private async Task ReceiveMessagesAsync()
        {
            byte[] buffer = new byte[1024];

            try
            {
                while (isConnected)
                {
                    // Асинхронно читаем данные из потока
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine("\nСервер разорвал соединение");
                        isConnected = false;
                        break;
                    }

                    // Преобраз байты в строку
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"\n[СЕРВЕР]: {message}");
                    Console.Write("Введите сообщение: ");
                }
            }
            catch ( Exception ex ) 
            {
                if (isConnected)
                {
                    Console.WriteLine($"\nОшибка получения сообщения: {ex.Message}");
                }
            }
        }

        // Task 2 асинхрон. отправка сообщений на сервер
        private async Task SendMessagesAsync()
        {
            try
            {
                while (isConnected)
                {
                    Console.Write("Введите сообщение: ");
                    string message = Console.ReadLine();

                    // if exit (break)
                    if (message?.ToLower() == "exit")
                    {
                        Console.WriteLine("Отключение от сервера...");
                        isConnected = false;
                        break;
                    }

                    // if zero in messages
                    if (string.IsNullOrEmpty(message))
                    {
                        Console.WriteLine("Сообщение не может быть пустым!");
                        continue;
                    }

                    // Преобраз. в байты и отправляем
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(data, 0, data.Length);
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine($"\nОшибка отправки сообщения: {ex.Message}");
                isConnected = false;
            }
        }

        // Откл. от сервера
        public void Disconnect()
        {
            try
            {
                isConnected = false;
                stream?.Close();
                tcpClient?.Close();  // Закрываем соединение
                Console.WriteLine($"Клиент #{ClientNumber}: Отключён от сервера");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Клиент #{ClientNumber}: Ошибка при отключении: {ex.Message}");
            }
        }
    }
}
