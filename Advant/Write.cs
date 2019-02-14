using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace Advant
{
    public static class Write
    {
        public static void Logs(string log)
        {
            string path = Directory.GetCurrentDirectory();

            using (var wr = new StreamWriter(path + @"/log.txt", true))
            {
                string data = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " -  ";
                wr.WriteLine(data + log);
            }
        }


        public static void Result(List<DataOutput> list)
        {
            int countInFile = 500;

            var lRez = list.Select((x, i) => new { Index = i, Value = x })
            .GroupBy(x => x.Index / countInFile)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();

            int j = 1;
            foreach (var item in lRez)
            {
                string header = "INSERT INTO `proposal` (`id`, `countryFrom`, `countryWhere`, `cityFrom`, `cityWhere`, `adults`, `children`, `hotelName`, `hotelRate`, `hotelDateFrom`, `hotelNights`, `hotelFood`, `hotelRoom`, `hotelPrice`, `operatorName`) VALUES";
                Random r = new Random();

                int count = 1;
                string path = Directory.GetCurrentDirectory();

                var time = DateTime.Now.ToString("HH_mm");
                using (StreamWriter sw = new StreamWriter($"{path}/database{j}_{time}.sql"))
                {
                    try
                    {
                        sw.WriteLine(header);

                        foreach (var d in item)
                        {
                            StringBuilder str = new StringBuilder();

                            str.AppendFormat("('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}'),", count, d.CountryFrom, d.CountryTo, d.CityFrom, d.CityTo, d.CountAdults, d.CountChildren, d.NameHotel, d.CountStart, d.DataFrom, d.CountNight, d.Food, d.TypeRoom, d.Price, d.Operator);

                            if (count++ == item.Count)
                            {
                                str.Replace("),", ");");
                            }

                            sw.WriteLine(str.ToString());
                        }
                    }
                    catch
                    {
                        //WriteLog("Error in Write Data to file\n" + e.ToString());
                    }
                }
                j++;
            }
        }
    }
}
