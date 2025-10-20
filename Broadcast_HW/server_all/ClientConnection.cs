using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server_M
{
    class ClientConnection
    {
        public TcpClient TcpClient { get; set; }  // TCP-соединение с клиентом
        public int Id { get; set; }  // Уникальный идентификатор клиента
        public NetworkStream Stream { get; set; }  // Поток для чтения/записи данных

        public bool IsConnected { get; set; } // флаг подкл

        public ClientConnection(TcpClient client, int id)
        {
            TcpClient = client;
            Id = id;
            Stream = client.GetStream();  // Получаем поток для обмена данными
            IsConnected = true;
        }
    }
}
