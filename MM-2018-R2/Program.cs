using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MM_2018_R2
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                //RunTest(3, TimeSpan.FromSeconds(30));
                //RunTest(755);
                //RunTest(993);
                RunTest(53);
            }
            else if (args.Length == 1 && args[0] == "-offline")
            {
                try
                {
                    Console.SetError(TextWriter.Null);
                    OfflineTester();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Exception: {0}", ex);
                }
                return;
            }
            else
            {
                ExportOfflineData(args[0]);
            }
        }

        private static void RunTest(int test)
        {
            RunTest(test, TimeSpan.Zero);
        }

        private static void RunTest(int test, TimeSpan testRepeatTime)
        {
            string filename = $"inputs\\{test}.txt";

            using (StreamReader input = new StreamReader(filename))
            {
                int H = int.Parse(input.ReadLine());
                string[] targetBoard = new string[H];
                for (int i = 0; i < H; ++i)
                    targetBoard[i] = input.ReadLine();
                int costLantern = int.Parse(input.ReadLine());
                int costMirror = int.Parse(input.ReadLine());
                int costObstacle = int.Parse(input.ReadLine());

                int maxMirrors = int.Parse(input.ReadLine());
                int maxObstacles = int.Parse(input.ReadLine());
                Stopwatch sw = Stopwatch.StartNew();
                string[] ret = new CrystalLighting().placeItems(targetBoard, costLantern, costMirror, costObstacle, maxMirrors, maxObstacles);
                if (testRepeatTime > TimeSpan.Zero)
                    while (sw.Elapsed < testRepeatTime)
                        ret = new CrystalLighting().placeItems(targetBoard, costLantern, costMirror, costObstacle, maxMirrors, maxObstacles);
                Console.Error.WriteLine(string.Join("\n", ret));
            }
        }

        private static void ExportOfflineData(string filename)
        {
            using (StreamWriter output = new StreamWriter(filename))
            {
                int H = int.Parse(Console.ReadLine());
                string[] targetBoard = new string[H];
                output.WriteLine(H);
                for (int i = 0; i < H; ++i)
                {
                    targetBoard[i] = Console.ReadLine();
                    output.WriteLine(targetBoard[i]);
                }
                int costLantern = int.Parse(Console.ReadLine());
                int costMirror = int.Parse(Console.ReadLine());
                int costObstacle = int.Parse(Console.ReadLine());
                output.WriteLine(costLantern);
                output.WriteLine(costMirror);
                output.WriteLine(costObstacle);

                int maxMirrors = int.Parse(Console.ReadLine());
                int maxObstacles = int.Parse(Console.ReadLine());
                output.WriteLine(maxMirrors);
                output.WriteLine(maxObstacles);

                CrystalLighting cl = new CrystalLighting();
                string[] ret = new string[0];

                Console.WriteLine(ret.Length);
                for (int i = 0; i < ret.Length; ++i)
                {
                    Console.WriteLine(ret[i]);
                }
            }
        }

        private static void OfflineTester()
        {
            int H = int.Parse(Console.ReadLine());
            string[] targetBoard = new string[H];
            for (int i = 0; i < H; ++i)
            {
                targetBoard[i] = Console.ReadLine();
            }
            int costLantern = int.Parse(Console.ReadLine());
            int costMirror = int.Parse(Console.ReadLine());
            int costObstacle = int.Parse(Console.ReadLine());

            int maxMirrors = int.Parse(Console.ReadLine());
            int maxObstacles = int.Parse(Console.ReadLine());

            CrystalLighting cl = new CrystalLighting();
            string[] ret = cl.placeItems(targetBoard, costLantern, costMirror, costObstacle, maxMirrors, maxObstacles);

            Console.WriteLine(ret.Length);
            for (int i = 0; i < ret.Length; ++i)
            {
                Console.WriteLine(ret[i]);
            }
        }
    }
}
