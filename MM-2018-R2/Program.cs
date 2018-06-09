using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MM_2018_R2
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "-offline")
            {
                try
                {
                    OfflineTester();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Exception: {0}", ex);
                }
                return;
            }

            int costLantern = 2;
            int costMirror = 6;
            int costObstacle = 15;
            int maxMirrors = 3;
            int maxObstacles = 3;
            string[] targetBoard = @"6.XX..2..X
.3...6X.X.
.2XXX..2..
..X1.X364.
..X6..X.3X
3.6...X.X.
...X3X4...
X.6...4XX.
.6XXX..X..
...11.1..6".Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            string[] ret = new CrystalLighting().placeItems(targetBoard, costLantern, costMirror, costObstacle, maxMirrors, maxObstacles);
            Console.Error.WriteLine(string.Join("\n", ret));
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
