using System.Collections.Generic;
using System.IO;
using System.Threading;
using Telegram.Bot;
using System.Text;
using System.Text.Json;

namespace BotProcessing;

public class JSONProcessing
{
    public static List<GasStation> Read(MemoryStream stream, ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        // Перемещаем позицию в потоке в начало
        stream.Seek(0, SeekOrigin.Begin); 
        using (StreamReader reader = new StreamReader(stream))
        {
            var jsonData = reader.ReadToEnd(); 
            if (!string.IsNullOrEmpty(jsonData))
            {
                // Преобразуем данные в список объектов
                return JsonSerializer.Deserialize<List<GasStation>>(jsonData);
            }
        }
        return new List<GasStation>(); 
    }
    public static MemoryStream Write(List<GasStation> data)
    {
        MemoryStream stream = new MemoryStream();
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
        // Записываем массив байтов в MemoryStream
        stream.Write(jsonBytes, 0, jsonBytes.Length);
        // Перемещаем позицию в MemoryStream в начало потока
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}   