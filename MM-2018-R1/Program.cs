using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        //TestPoints(5);
        //return;

        if (args.Length == 0)
        {
            //RunTest(328, saveImage: true);
            using (StreamWriter costsOutput = new StreamWriter("newCosts.txt"))
            {
                for (int i = 1; i <= 1000; i++)
                    costsOutput.WriteLine(RunTest(i));
            }
        }
        else if (args[0] == "-offline")
        {
            TestOffline();
        }
        else
        {
            ExportOfflineData(args[0]);
        }
    }

    private static void TestPoints(int pointsCount)
    {
        const int mapSize = 100;
        const int maxTests = 100000;
        Random random = new Random();
        int betterDistances = 0;
        int betterDistances1 = 0;
        int betterDistances2 = 0;

        for (int test = 0; test < maxTests; test++)
        {
            if (test % 1000 == 0)
                Console.Write($"{test * 100.0 / maxTests:0.00}%\r");

            City[] points = new City[pointsCount];
            for (int j = 0; j < points.Length; j++)
                points[j] = new City()
                {
                    X = random.Next(mapSize),
                    Y = random.Next(mapSize),
                };
            double regularDistance = RoadsAndJunctions.FindMinConnectionCost(points);

            // Start with Geometric median point and star distance
            City gmp = RoadsAndJunctions.FindGeometricMedianPoint(points);
            double minDistance = RoadsAndJunctions.StarDistance(gmp, points);
            City extendedPoint = gmp;

            // Try reducing to triangle + rest of the points
            City[] triangle = new City[3];
            City[] extendedPoints = new City[points.Length + 1];
            for (int i = 0; i < points.Length; i++)
                extendedPoints[i] = points[i];
            for (int i1 = 0; i1 < points.Length; i1++)
            {
                triangle[0] = points[i1];
                for (int i2 = i1 + 1; i2 < points.Length; i2++)
                {
                    triangle[1] = points[i2];
                    for (int i3 = i2 + 1; i3 < points.Length; i3++)
                    {
                        triangle[2] = points[i3];
                        extendedPoints[points.Length] = RoadsAndJunctions.FindGeometricMedianPoint(triangle);
                        double distance = RoadsAndJunctions.FindMinConnectionCost(extendedPoints);

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            extendedPoint = extendedPoints[points.Length];
                        }
                    }
                }
            }

            //// Try adding two points
            //int minx = points.Min(p => p.X);
            //int maxx = points.Max(p => p.X);
            //int miny = points.Min(p => p.Y);
            //int maxy = points.Max(p => p.Y);
            //extendedPoints = new City[points.Length + 2];
            //for (int i = 0; i < points.Length; i++)
            //    extendedPoints[i] = points[i];
            //double twoPointsMinDistance = double.MaxValue;
            //City point1 = new City();
            //City point2 = new City();
            //for (int x1 = minx; x1 <= maxx; x1++)
            //    for (int y1 = miny; y1 <= miny; y1++)
            //    {
            //        extendedPoints[points.Length] = new City() { X = x1, Y = y1 };
            //        if (points.Contains(extendedPoints[points.Length]))
            //            continue;
            //        for (int x2 = minx; x2 <= maxx; x2++)
            //            for (int y2 = miny; y2 <= maxy; y2++)
            //            {
            //                if (x1 >= x2 && y1 >= y2)
            //                    continue;
            //                extendedPoints[points.Length + 1] = new City() { X = x2, Y = y2 };
            //                if (points.Contains(extendedPoints[points.Length + 1]))
            //                    continue;
            //                double distance = RoadsAndJunctions.FindMinConnectionCost(extendedPoints);

            //                if (distance < twoPointsMinDistance)
            //                {
            //                    twoPointsMinDistance = distance;
            //                    point1 = extendedPoints[points.Length];
            //                    point2 = extendedPoints[points.Length + 1];
            //                }
            //            }
            //    }
            //double diff = minDistance - twoPointsMinDistance;
            //if (diff > 0)
            //{
            //    betterDistances2++;
            //}
#if false
            if (Debugger.IsAttached)
            {
                int scale = 5;

                using (Bitmap bmp = new Bitmap(mapSize * scale, mapSize * scale))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        g.FillRectangle(Brushes.White, 0, 0, bmp.Width, bmp.Height);

                        foreach (City p in points)
                            g.FillRectangle(Brushes.Blue, p.X * scale, p.Y * scale, scale, scale);
                        g.FillRectangle(Brushes.Red, gmp.X * scale, gmp.Y * scale, scale, scale);
                    }
                    bmp.Save("image.png");
                }
            }
#endif

            if (minDistance < regularDistance)
            {
                betterDistances++;
            }
            if (minDistance / regularDistance < 0.95)
            {
                betterDistances1++;
            }
        }
        Console.WriteLine($"Summary: {betterDistances} / {maxTests} [{betterDistances * 100.0 / maxTests:0.00}%]");
        Console.WriteLine($"1: {betterDistances1} / {maxTests} [{betterDistances1 * 100.0 / maxTests:0.00}%]");
        Console.WriteLine($"2: {betterDistances2} / {maxTests} [{betterDistances2 * 100.0 / maxTests:0.00}%]");
    }

    private static double RunTest(int test, bool saveImage = false)
    {
        TimeSpan testTime;
        double cost;
        double junctionCost = -1;
        double failureProbability = -1;

        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            cost = RunTest($"inputs\\{test}.txt", out testTime, saveImage ? $"images\\{test}.png" : null, out junctionCost, out failureProbability);
        }
        catch (Exception ex)
        {
            cost = -1;
            testTime = sw.Elapsed;
            Console.Error.WriteLine($"Error: {ex}");
        }

        double expectedCost = -1;

        try
        {
            string[] lines = File.ReadAllLines("previousCosts.txt");
            string line = lines[test - 1];
            expectedCost = double.Parse(line);
        }
        catch
        {
        }

        var previousColor = Console.ForegroundColor;
        if (cost < expectedCost * 0.99)
            Console.ForegroundColor = ConsoleColor.Green;
        else if (cost < expectedCost * 0.9995)
            Console.ForegroundColor = ConsoleColor.Yellow;
        else if (cost > expectedCost * 1.01)
            Console.ForegroundColor = ConsoleColor.DarkRed;
        else if (cost > expectedCost * 1.0005)
            Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{test,4}: {cost,10:0.0000} / {expectedCost,10:0.0000} [{cost / expectedCost * 100,6:0.00}%] ({testTime.TotalSeconds,6:0.000}s)   |  {junctionCost:0.00} / {failureProbability:0.00}%");
        Console.ForegroundColor = previousColor;
        return cost;
    }

    private static void ParseTest(string testFile, out int mapSize, out City[] cities, out double junctionCost, out double failureProbability, out bool[] junctionStatus)
    {
        using (StreamReader reader = new StreamReader(testFile))
        {
            mapSize = int.Parse(reader.ReadLine());
            int C = int.Parse(reader.ReadLine());
            cities = new City[C / 2];
            for (int i = 0; i < C / 2; ++i)
            {
                int X = int.Parse(reader.ReadLine());
                int Y = int.Parse(reader.ReadLine());

                cities[i] = new City()
                {
                    X = X,
                    Y = Y,
                };
            }
            junctionCost = double.Parse(reader.ReadLine());
            failureProbability = double.Parse(reader.ReadLine());

            int J = int.Parse(reader.ReadLine());
            junctionStatus = new bool[J];
            for (int i = 0; i < J; ++i)
                junctionStatus[i] = int.Parse(reader.ReadLine()) != 0;
        }
    }

    private static double RunTest(string testFile, out TimeSpan testTime, string imageFile = null)
    {
        double junctionCost;
        double failureProbability;

        return RunTest(testFile, out testTime, imageFile, out junctionCost, out failureProbability);
    }

    private static double RunTest(string testFile, out TimeSpan testTime, string imageFile, out double junctionCost, out double failureProbability)
    {
        // Parse test
        int mapSize;
        City[] cities;
        bool[] junctionStatus;

        ParseTest(testFile, out mapSize, out cities, out junctionCost, out failureProbability, out junctionStatus);

        // Solve test
        Junction[] junctions;
        RoadsAndJunctions solver = new RoadsAndJunctions();
        int[] citiesArray = new int[cities.Length * 2];

        for (int i = 0; i < cities.Length; i++)
        {
            citiesArray[2 * i] = cities[i].X;
            citiesArray[2 * i + 1] = cities[i].Y;
        }

        Stopwatch sw = Stopwatch.StartNew();
        int[] junctionsArray = solver.buildJunctions(mapSize, citiesArray, junctionCost, failureProbability);
        sw.Stop();

        if (junctionsArray.Length % 2 == 1)
            throw new Exception($"Invalid junctions array size ({junctionsArray.Length}) - it has odd number of elements");

        junctions = new Junction[junctionsArray.Length / 2];
        for (int i = 0; i < junctionsArray.Length / 2; i++)
        {
            junctions[i] = new Junction()
            {
                X = junctionsArray[2 * i],
                Y = junctionsArray[2 * i + 1],
            };
        }

        // Verify junctions
        if (junctions.Length > cities.Length * 2)
            throw new Exception($"Too many junctions");
        foreach (Junction junction in junctions)
        {
            if (junction.X < 0 || junction.X >= mapSize || junction.Y < 0 || junction.Y >= mapSize)
                throw new Exception($"Junction coordinates are incorrect: {junction}");
            if (cities.Any(c => c.X == junction.X && c.Y == junction.Y))
                throw new Exception($"Junction cannot be built in a city: {junction}");
        }

        // Build roads
        sw.Start();
        int[] roadsArray = solver.buildRoads(junctionStatus.Take(junctions.Length).Select(j => j ? 1 : 0).ToArray());
        sw.Stop();
        testTime = sw.Elapsed;

        // Verify that roads are correct
        if (roadsArray.Length % 2 == 1)
            throw new Exception($"Invalid roads array size ({roadsArray.Length}) - it has odd number of elements");

        for (int i = 0; i < roadsArray.Length; i++)
        {
            if (roadsArray[i] < 0 || roadsArray[i] >= cities.Length + junctions.Length)
                throw new Exception($"Invalid road index: {roadsArray[i]}");
            if (roadsArray[i] >= cities.Length && !junctionStatus[roadsArray[i] - cities.Length])
                throw new Exception($"You can not build a road to a dysfunctional junction: {roadsArray[i] - cities.Length}");
        }

        // Convert roads
        Tuple<int, int>[] roads = new Tuple<int, int>[roadsArray.Length / 2];
        for (int i = 0; i < roads.Length; i++)
            roads[i] = Tuple.Create(roadsArray[2 * i], roadsArray[2 * i + 1]);
        Tuple<City, City>[] roadsCities = new Tuple<City, City>[roads.Length];
        for (int i = 0; i < roads.Length; i++)
        {
            int i1 = roads[i].Item1;
            int i2 = roads[i].Item2;
            City first, second;

            if (i1 < cities.Length)
                first = cities[i1];
            else
                first = new City()
                {
                    X = junctions[i1 - cities.Length].X,
                    Y = junctions[i1 - cities.Length].Y,
                };
            if (i2 < cities.Length)
                second = cities[i2];
            else
                second = new City()
                {
                    X = junctions[i2 - cities.Length].X,
                    Y = junctions[i2 - cities.Length].Y,
                };
            roadsCities[i] = Tuple.Create(first, second);
        }

        // TODO: Verify that all cities are connected

        // Draw image
        if (!string.IsNullOrEmpty(imageFile))
        {
            const int scale = 5;
            float zoom = Math.Max(1, mapSize / 200.0f);
            float roadPenWidth = 1.5f * zoom;
            float cityRadius = scale * zoom;
            float junctionRadius = scale * zoom;
            float xOffset = scale / 2.0f;
            float yOffset = scale / 2.0f;
            Pen roadPen = new Pen(Color.Black, roadPenWidth);
            using (Bitmap bmp = new Bitmap(mapSize * scale, mapSize * scale))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.FillRectangle(Brushes.White, 0, 0, bmp.Width, bmp.Height);

                    // Draw roads
                    foreach (var road in roadsCities)
                    {
                        City first = road.Item1;
                        City second = road.Item2;
                        float x1 = first.X * scale + xOffset;
                        float y1 = first.Y * scale + yOffset;
                        float x2 = second.X * scale + xOffset;
                        float y2 = second.Y * scale + yOffset;

                        g.DrawLine(roadPen, x1, y1, x2, y2);
                    }

                    // Draw junctions
                    for (int i = 0; i < junctions.Length; i++)
                    {
                        var junction = junctions[i];
                        float x = junction.X * scale + xOffset;
                        float y = junction.Y * scale + yOffset;

                        g.FillEllipse(junctionStatus[i] ? Brushes.Green : Brushes.Red, x - junctionRadius / 2, y - junctionRadius / 2, junctionRadius, junctionRadius);
                    }

                    // Draw cities
                    foreach (var city in cities)
                    {
                        float x = city.X * scale + xOffset;
                        float y = city.Y * scale + yOffset;

                        g.FillEllipse(Brushes.Blue, x - cityRadius / 2, y - cityRadius / 2, cityRadius, cityRadius);
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(imageFile));
                bmp.Save(imageFile);
            }
        }

        // Calculate cost
        double cost = junctionCost * junctions.Length;

        for (int i = 0; i < roads.Length; i++)
        {
            cost += roadsCities[i].Item1.Distance(roadsCities[i].Item2);
        }
        return cost;
    }

    static void ExportOfflineData(string filename)
    {
        using (StreamWriter output = new StreamWriter(filename))
        {
            // Parse input
            int S = int.Parse(Console.ReadLine());
            int C = int.Parse(Console.ReadLine());
            output.WriteLine(S);
            output.WriteLine(C);
            City[] cities = new City[C / 2];
            for (int i = 0; i < C / 2; ++i)
            {
                int X = int.Parse(Console.ReadLine());
                int Y = int.Parse(Console.ReadLine());

                cities[i] = new City()
                {
                    X = X,
                    Y = Y,
                };
                output.WriteLine(X);
                output.WriteLine(Y);
            }
            double junctionCost = double.Parse(Console.ReadLine());
            double failureProbability = double.Parse(Console.ReadLine());
            output.WriteLine(junctionCost);
            output.WriteLine(failureProbability);

            // Make all possible junctions to store junction status
            List<Junction> junctions = new List<Junction>();
            Random random = new Random();

            for (int i = 0; i < cities.Length * 2; i++)
            {
                Junction j = new Junction();
                bool found = true;

                while (found)
                {
                    j = new Junction()
                    {
                        X = random.Next(S),
                        Y = random.Next(S),
                    };

                    found = cities.Any(c => c.X == j.X && c.Y == j.Y)
                        || junctions.Contains(j);
                }
                junctions.Add(j);
            }

            Console.WriteLine(junctions.Count * 2);
            foreach (Junction j in junctions)
            {
                Console.WriteLine(j.X);
                Console.WriteLine(j.Y);
            }
            Console.Out.Flush();

            // Redirect junction statuses
            int J = int.Parse(Console.ReadLine());
            output.WriteLine(J);
            int[] junctionStatus = new int[J];
            for (int i = 0; i < J; ++i)
            {
                junctionStatus[i] = int.Parse(Console.ReadLine());
                output.WriteLine(junctionStatus[i]);
            }
        }
    }

    static void TestOffline()
    {
        int S = int.Parse(Console.ReadLine());
        int C = int.Parse(Console.ReadLine());
        int[] cities = new int[C];
        for (int i = 0; i < C; ++i)
        {
            cities[i] = int.Parse(Console.ReadLine());
        }
        double junctionCost = double.Parse(Console.ReadLine());
        double failureProbability = double.Parse(Console.ReadLine());

        RoadsAndJunctions rj = new RoadsAndJunctions();
        int[] ret = rj.buildJunctions(S, cities, junctionCost, failureProbability);
        Console.WriteLine(ret.Length);
        for (int i = 0; i < ret.Length; ++i)
        {
            Console.WriteLine(ret[i]);
        }
        Console.Out.Flush();

        int J = int.Parse(Console.ReadLine());
        int[] junctionStatus = new int[J];
        for (int i = 0; i < J; ++i)
        {
            junctionStatus[i] = int.Parse(Console.ReadLine());
        }

        ret = rj.buildRoads(junctionStatus);
        Console.WriteLine(ret.Length);
        for (int i = 0; i < ret.Length; ++i)
        {
            Console.WriteLine(ret[i]);
        }
    }
}
