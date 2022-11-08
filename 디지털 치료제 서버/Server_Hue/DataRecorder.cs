using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;


namespace Server_Hue
{

    internal class DataRecorder
    {
        public void PrintData(ConcurrentQueue<string> datas, ushort id, ushort device)
        {
            string file_index = string.Empty;
            switch (device)
            {
                case 1:
                    file_index = "Watch";
                    break;
                case 2:
                    file_index = "Airpod";
                    break;
                case 3:
                    file_index = "Device";
                    break;
            }
            DirectoryInfo di = new DirectoryInfo(@"c:\MyDir\");
            var fileName = di + "CheckFile" + id.ToString() + "_" + file_index + ".txt";
        //    Console.WriteLine(fileName);

            // Write each directory name to a file.
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                for (int i = 0; datas.Count > 0; i++)
                {
                    datas.TryDequeue(out var data);
                    sw.WriteLine(data);
                }
                sw.Close();
            }
            if (datas.Count == 0)
            {
                if (!FileIsUse(fileName))
                {
                    Console.WriteLine("ERRROR");
                }
                else
                {
                  //  Console.WriteLine("Successfully Saved");
                }
            }
        }
        private bool FileIsUse(string strFilePath)
        {
            try
            {
                using (System.IO.FileStream fs = new System.IO.FileStream(strFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }
    }


}
