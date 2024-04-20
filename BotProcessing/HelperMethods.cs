using System;
using System.Collections.Generic;
using System.Linq;

namespace BotProcessing;

public class HelperMethods
{
    // Метод преобразовывет список строк в список объектов
    public static List<GasStation> SplitToListOfObjects(List<string> data)
    {
        try
        {
            var splittedData = new List<GasStation>();
            foreach (var line in data)
            {
                string[] lineData = line.Split(';');
                for (int i = 0; i < lineData.Length; i++)
                {
                    lineData[i] = lineData[i].Trim('"');
                }
                if (lineData.Any(i => (i != null || i != "")))
                {
                    try
                    {
                        splittedData.Add(new GasStation(lineData[0], lineData[1], lineData[2], lineData[3],
                            lineData[4],
                            lineData[5], lineData[6], lineData[7], lineData[8], lineData[9],
                            lineData[10]));
                    } 
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        return null;
                    }
                }
            }

            return splittedData;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return null;
        }
        
    }
}