namespace BotProcessing;
using System;
using System.Collections.Generic;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class CSVProcessing
{
    public static List<GasStation> Read(Stream stream)
    {
        List<string> data = new List<string>();
        try
        {
            // Считываем данные через поток
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    data.Add(line);
                }
            }
            // Проверка на соответствие формату варианта.
            string expectedFirstLine =
                $"\"ID\";\"FullName\";\"global_id\";\"ShortName\";\"AdmArea\";\"District" +
                $"\";\"Address\";\"Owner\";\"TestDate\";\"geodata_center\";\"geoarea\";";
            string expectedSecondLine = $"\"Код\";\"Полное официальное наименование\";\"global_id\";\"Сокращенное наименование\";" +
                                        $"\"Административный округ\";\"Район\";\"Адрес\";\"Наименование компании\";\"Дата проверки\";\"geodata_center\";\"geoarea\";";
            if (data != null  && data.Count > 2 && data[0] == expectedFirstLine && data[1] == expectedSecondLine)
            {
                // Убираем две строки заголовка
                data.RemoveRange(0, 2);
                List<GasStation> list = HelperMethods.SplitToListOfObjects(data);
                return list;

            }
            else
            {
                return null;
            }
        }
        catch
        {
            return null;
        }
        
    }

    // Метод для записи в файл. Возвращает поток для записи
    public static Stream Write(List<GasStation> data)
    {
        MemoryStream stream = new MemoryStream();
        // Указываем кодировку для корректной записи данных
        StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
        // Добавляем две строки заголовка
        string firstLine =
            $"\"ID\";\"FullName\";\"global_id\";\"ShortName\";\"AdmArea\";\"District" +
            $"\";\"Address\";\"Owner\";\"TestDate\";\"geodata_center\";\"geoarea\";";
        string secondLine = $"\"Код\";\"Полное официальное наименование\";\"global_id\";\"Сокращенное наименование\";" +
                            $"\"Административный округ\";\"Район\";\"Адрес\";\"Наименование компании\";\"Дата проверки\";\"geodata_center\";\"geoarea\";";
        writer.WriteLine(firstLine);
        writer.WriteLine(secondLine);
        foreach (var obj in data)
        {
            // Записываем данные в файл
            writer.WriteLine(
                $"\"{obj.Id}\";\"{obj.FullName}\";\"{obj.GlobalId}\";\"{obj.ShortName}\";\"{obj.AdmArea}\";" +
                $"\"{obj.District}\";\"{obj.Address}\";\"{obj.Owner}\";\"{obj.TestDate}\";\"{obj.GeoDataCenter}\";\"{obj.GeoArea}\";");
        }

        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}