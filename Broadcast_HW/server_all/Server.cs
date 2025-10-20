using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server_M
{
    class Server
    {
        private TcpListener listener;  // Слушатель входящих подключений
        private List<ClientConnection> clients;  // Список подключенных клиентов
        private int clientIdCounter = 0;  // Счётчик для генерации уникальных ID
        private bool isRunning = false;  // Флаг работы сервера
        private object lockObject = new object();  // Объект для синхронизации доступа к списку

        public Server(string ip, int port)
        {
            listener = new TcpListener(IPAddress.Parse(ip), port);  // Создаём слушателя на указанном IP и порту
            clients = new List<ClientConnection>();  // Инициализируем список клиентов
        }

        // Start Server
        public async Task StartAsync()
        {
            listener.Start();  // Запускаем прослушивание
            isRunning = true;
            Console.WriteLine("Сервер запущен. Ожидание подключений...");

            while (isRunning)
            {
                try
                {
                    // Асинхронно ожидаем подключение клиента
                    TcpClient client = await listener.AcceptTcpClientAsync();

                    // Создаём объект для нового клиента
                    clientIdCounter++;
                    ClientConnection clientConnection = new ClientConnection(client, clientIdCounter);
                    //clients.Add(clientConnection);  // Добавляем в список

                    // Используем lock для безопасного добавления в список (многопоточность)
                    lock (lockObject)
                    {
                        clients.Add(clientConnection);
                    }

                    Console.WriteLine($"Клиент #{clientConnection.Id} подключился");

                    // Запускаем обработку клиента в отдельной задаче
                    _ = Task.Run(() => HandleClientAsync(clientConnection));
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    Console.WriteLine($"Ошибка при подключении: {ex.Message}");
                }
            }
        }

        // Метод обработки клиента (асинхронный)
        private async Task HandleClientAsync(ClientConnection clientConnection)
        {
            try
            {
                // Сообщение от сервера (начальное)
                await SendMessageToClientAsync(clientConnection, "Hello from server!");

                // Постоянное чтение client
                Task readTask = ReadMessagesAsync(clientConnection);

                // ожидаем окончания чтения
                await readTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке клиента #{clientConnection.Id}: {ex.Message}");
            }
            finally
            {
                // delete client from list
                RemoveClient(clientConnection);
            }
        }

        // Чтение сообщений от клиента
        private async Task ReadMessagesAsync(ClientConnection clientConnection)
        {
            byte[] buffer = new byte[1024];

            try
            {
                // infinity read cicle
                while (clientConnection.IsConnected)
                {
                    // чтение данных из потока
                    int bytesRead = await clientConnection.Stream.ReadAsync(buffer, 0, buffer.Length);

                    // Если получено 0 байт то клиент отключился
                    if (bytesRead == 0)
                    {
                        Console.WriteLine($"Клиент #{clientConnection.Id} отключился");
                        clientConnection.IsConnected = false;
                        break;
                    }

                    // Конвертируем полученные байты в строку
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Получено от клиента #{clientConnection.Id}: {message}");

                    // Broadcast типа
                    string broadcastMessage = $"Клиент #{clientConnection.Id} отправил: {message}";
                    await BroadcastMessageAsync(broadcastMessage, clientConnection.Id);
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения от клиента #{clientConnection.Id}: {ex.Message}");
                clientConnection.IsConnected = false;
            }
        }

        // Отправка сообщ. конкретному клиенту
        private async Task SendMessageToClientAsync(ClientConnection clientConnection, string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await clientConnection.Stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки клиенту #{clientConnection.Id}: {ex.Message}");
                clientConnection.IsConnected = false;
            }
        }

        // Снова типа Бродкаст (отправка сообщений всем клиентам)
        private async Task BroadcastMessageAsync(string message, int senderId)
        {
            List<ClientConnection> clientsCopy;

            // Создаём копию списка для безопасной итерации
            lock (lockObject)
            {
                clientsCopy = new List<ClientConnection>(clients);
            }
            // Отправляем сообщение всем клиентам асинхронно
            List<Task> sendTasks = new List<Task>();

            foreach (var client in clientsCopy)
            {
                // Отправляем всем, кроме отправителя
                if (client.IsConnected && client.Id != senderId)
                {
                    sendTasks.Add(SendMessageToClientAsync(client, message));
                }
            }
            // pause
            await Task.WhenAll(sendTasks);
        }

        // Удаление клиента из списка
        private void RemoveClient(ClientConnection clientConnection)
        {
            lock (lockObject)
            {
                clients.Remove(clientConnection);
            }

            try
            {
                clientConnection.Stream?.Close();
                clientConnection.TcpClient?.Close();
            }
            catch { }

            Console.WriteLine($"Клиент #{clientConnection.Id} удалён из списка");
        }

        // Метод для отключения всех клиентов (проход по списку)
        public void DisconnectAllClients()
        {
            Console.WriteLine("\nОтключение всех клиентов...");

            List<ClientConnection> clientsCopy;
            lock (lockObject)
            {
                clientsCopy = new List<ClientConnection>(clients);
            }

            // Цикл
            foreach (var client in clientsCopy)
            {
                try
                {
                    client.IsConnected = false;
                    client.Stream?.Close();
                    client.TcpClient?.Close();
                    Console.WriteLine($"Клиент #{client.Id} отключён");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при отключении клиента #{client.Id}: {ex.Message}");
                }
            }
            lock (lockObject)
            {
                clients.Clear();
            } // Очищаем список клиентов
        }

        // Откл сервер
        public void Stop()
        {
            isRunning = false;
            DisconnectAllClients();  // Отключаем всех клиентов
            listener.Stop();  // Останавливаем прослушивание
            Console.WriteLine("Сервер остановлен");
        }
    }
}
