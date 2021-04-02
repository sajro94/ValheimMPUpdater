using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValheimMPUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            MpUpdater updater = new MpUpdater();
            try
            {
                updater.Run();
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }
    }
}
