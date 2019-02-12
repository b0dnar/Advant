using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

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
            int n = list.Count / countInFile;

            for (int i = 0; i <= n; i++)
            {
                string header = "INSERT INTO `proposal` (`id`, `countryFrom`, `countryWhere`, `cityFrom`, `cityWhere`, `adults`, `children`, `hotelName`, `hotelRate`, `hotelDateFrom`, `hotelNights`, `hotelFood`, `hotelRoom`, `hotelPrice`, `operatorName`) VALUES";
                Random r = new Random();

                int count = 1;
                string path = Directory.GetCurrentDirectory();

                var time = DateTime.Now.ToString("HH_mm");
                using (StreamWriter sw = new StreamWriter($"{path}/database{i + 1}_{time}.sql"))
                {
                    try
                    {
                        sw.WriteLine(header);

                        foreach (var d in list)
                        {
                            StringBuilder str = new StringBuilder();

                            str.AppendFormat("('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}'),", count, d.CountryFrom, d.CountryTo, d.CityFrom, d.CityTo, d.CountAdults, d.CountChildren, d.NameHotel, d.CountStart, d.DataFrom, d.CountNight, d.Food, d.TypeRoom, d.Price, d.Operator);

                            if (count++ == list.Count)
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
            }


            
        }
    }
}
