using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

struct City
{
    public int X;
    public int Y;

    public int Distance2(City other)
    {
        return (X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y);
    }

    public double Distance(City other)
    {
        return Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));
    }

    public double Distance(Junction other)
    {
        return Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Junction))
            return false;
        Junction other = (Junction)obj;
        return X == other.X && Y == other.Y;
    }

#if LOCAL
    public override string ToString()
    {
        return $"({X}, {Y})";
    }
#endif
}

struct Junction
{
    public int X;
    public int Y;

    public double Distance(Junction other)
    {
        return Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));
    }

    public double Distance(City other)
    {
        return Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Junction))
            return false;
        Junction other = (Junction)obj;
        return X == other.X && Y == other.Y;
    }

#if LOCAL
    public override string ToString()
    {
        return $"({X}, {Y})";
    }
#endif
}

struct CityDistance
{
    public int CityId;
    public double Distance;

#if LOCAL
    public override string ToString()
    {
        return $"({CityId} -> {Distance})";
    }
#endif
}

struct Road
{
    public int Id1;
    public int Id2;
}

class State
{
    internal List<City> cities;
    internal List<List<CityDistance>> cityDistances;
    internal List<Junction> junctions;
    internal double solutionCost = double.MaxValue;
    internal double extectedValueSolutionCost = double.MaxValue;
    internal int mapSize;

    private State()
    {
    }

    public State Clone()
    {
        State s = new State();

        s.cities = new List<City>(cities.Capacity);
        s.cities.AddRange(cities);
        s.cityDistances = new List<List<CityDistance>>(cities.Capacity);
        for (int i = 0; i < cities.Count; i++)
        {
            s.cityDistances.Add(new List<CityDistance>(cities.Capacity));
            s.cityDistances[i].AddRange(cityDistances[i]);
        }
        s.mapSize = mapSize;
        s.junctions = new List<Junction>(cities.Capacity);
        s.junctions.AddRange(junctions);
        s.solutionCost = solutionCost;
        s.extectedValueSolutionCost = extectedValueSolutionCost;
        return s;
    }

    public static State ProcessInputData(int S, int[] citiesArray)
    {
        State s = new State();
        s.mapSize = S;
        s.cities = new List<City>(citiesArray.Length / 2 * 3);
        for (int i = 0; i < citiesArray.Length / 2; i++)
            s.cities.Add(new City
            {
                X = citiesArray[2 * i],
                Y = citiesArray[2 * i + 1],
            });
        s.cityDistances = new List<List<CityDistance>>(s.cities.Capacity);
        for (int i = 0; i < s.cities.Count; i++)
        {
            s.cityDistances.Add(new List<CityDistance>(s.cities.Capacity));
            s.cityDistances[i].Add(new CityDistance() { CityId = i, Distance = double.MaxValue });
        }
        for (int i = 0; i < s.cities.Count; i++)
        {
            for (int j = i + 1; j < s.cities.Count; j++)
            {
                double distance = s.cities[i].Distance(s.cities[j]);

                s.cityDistances[i].Add(new CityDistance() { CityId = j, Distance = distance });
                s.cityDistances[j].Add(new CityDistance() { CityId = i, Distance = distance });
            }
        }
        for (int i = 0; i < s.cities.Count; i++)
            s.cityDistances[i].Sort((a, b) => Comparer<double>.Default.Compare(a.Distance, b.Distance));
        s.junctions = new List<Junction>(s.cities.Capacity);
        return s;
    }

    public void ExpandJunctionsIntoCities(List<int> jidConversion)
    {
        // Include junctions into cities list
        int citiesCount = cities.Count;

        for (int i = cities.Count; i < jidConversion.Count; i++)
        {
            int jid = jidConversion[i] - citiesCount;
            Junction junction = junctions[jid];

            cities.Add(new City() { X = junction.X, Y = junction.Y });
        }

        // Expand city distances cache
        for (int i = 0; i < citiesCount; i++)
        {
            for (int j = citiesCount; j < cities.Count; j++)
                cityDistances[i].Add(new CityDistance() { CityId = j, Distance = cities[i].Distance(cities[j]) });
        }
        for (int i = citiesCount; i < jidConversion.Count; i++)
        {
            cityDistances.Add(new List<CityDistance>(cities.Capacity));
            for (int j = 0; j < cities.Count; j++)
                cityDistances[i].Add(new CityDistance() { CityId = j, Distance = i != j ? cities[i].Distance(cities[j]) : double.MaxValue });
        }
        for (int i = 0; i < cities.Count; i++)
            cityDistances[i].Sort((a, b) => Comparer<double>.Default.Compare(a.Distance, b.Distance));
    }

    public List<Road> FindMinConnectionCostWithoutLastCity(out double cost, int[] distanceIndexes, int startingCity = 0)
    {
        ClearUpdatedExtendedDataForOneCity(distanceIndexes);
        cities.RemoveAt(cities.Count - 1);
        var roads = FindMinConnectionCost(out cost, startingCity);
        cities.Add(new City());
        return roads;
    }

    public List<Road> FindMinConnectionCost(out double cost, int startingCity = 0)
    {
        List<Road> roads = new List<Road>(cities.Capacity);
        bool[] picked = new bool[cities.Count];
        int[] q = new int[cities.Count];
        int[] c = new int[cities.Count];
        cost = 0;
        q[0] = startingCity;
        c[0] = 0;
        picked[startingCity] = true;
        for (int selected = 1; selected < cities.Count; selected++)
        {
            double minDistance = double.MaxValue;
            int i1 = -1, i2 = -1;

            for (int qi = 0; qi < selected; qi++)
            {
                int i = q[qi];
                List<CityDistance> cdd = cityDistances[i];

                for (int j = c[i]; j < cities.Count; j++)
                    if (!picked[cdd[j].CityId])
                    {
                        double distance = cdd[j].Distance;

                        c[i] = j;
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            i1 = i;
                            i2 = cdd[j].CityId;
                        }
                        break;
                    }
            }
            cost += minDistance;
            picked[i2] = true;
            q[selected] = i2;
            c[selected] = 0;
            c[i1]++;
            roads.Add(new Road() { Id1 = i1, Id2 = i2 });
        }
        return roads;
    }

    public int[] ExtendDataForOneCity()
    {
        int newCityIndex = cities.Count;
        int[] distanceIndexes = new int[newCityIndex];

        cities.Add(new City() { });
        for (int i = 0; i < newCityIndex; i++)
        {
            cityDistances[i].Add(new CityDistance());
            distanceIndexes[i] = newCityIndex;
        }
        cityDistances.Add(new List<CityDistance>(cities.Capacity));
        for (int j = 0; j < cities.Count; j++)
            cityDistances[newCityIndex].Add(new CityDistance());
        return distanceIndexes;
    }

    public void UpdateExtendedDataForOneCity(City city, int[] distanceIndexes)
    {
        int newCityIndex = cities.Count - 1;

        cities[newCityIndex] = city;
        for (int i = 0; i < newCityIndex; i++)
        {
            cityDistances[i].RemoveAt(distanceIndexes[i]);
            var cd = new CityDistance() { CityId = newCityIndex, Distance = cities[i].Distance(cities[newCityIndex]) };
            int index = cityDistances[i].BinarySearch(cd, CityDistanceComparer);

            if (index < 0)
                index = ~index;
            distanceIndexes[i] = index;
            cityDistances[i].Insert(index, cd);
        }
        for (int i = 0; i < cities.Count; i++)
            cityDistances[newCityIndex][i] = new CityDistance() { CityId = i, Distance = newCityIndex != i ? cities[newCityIndex].Distance(cities[i]) : double.MaxValue };
        cityDistances[newCityIndex].Sort((a, b) => Comparer<double>.Default.Compare(a.Distance, b.Distance));
    }

    public void ClearUpdatedExtendedDataForOneCity(int[] distanceIndexes)
    {
        int newCityIndex = cities.Count - 1;

        cities[newCityIndex] = new City();
        for (int i = 0; i < newCityIndex; i++)
        {
            cityDistances[i].RemoveAt(distanceIndexes[i]);
            cityDistances[i].Add(new CityDistance());
            distanceIndexes[i] = newCityIndex;
        }
    }

    private class CDComparer : IComparer<CityDistance>
    {
        public int Compare(CityDistance x, CityDistance y)
        {
            return Comparer<double>.Default.Compare(x.Distance, y.Distance);
        }
    }

    private static CDComparer CityDistanceComparer = new CDComparer();
}

public class RoadsAndJunctions
{
#if LOCAL
  #if DEBUG
    public static readonly TimeSpan MaxExecutionTime = TimeSpan.FromSeconds(5000);
  #else
    public static readonly TimeSpan MaxExecutionTime = TimeSpan.FromSeconds(2.5);
#endif
#else
    public static readonly TimeSpan MaxExecutionTime = TimeSpan.FromSeconds(9.5);
#endif
    private int SearchClosestCities = 7;
    private State State;
    private State CleanState;
    private Stopwatch Stopwatch;

    bool HasTime
    {
        get
        {
            return Stopwatch.Elapsed < MaxExecutionTime;
        }
    }

    private void ProcessInputData(int S, int[] cities)
    {
        Stopwatch = Stopwatch.StartNew();
        State = State.ProcessInputData(S, cities);
        CleanState = State.Clone();
    }

    public int[] buildJunctions(int S, int[] cities, double junctionCost, double failureProbability)
    {
        ProcessInputData(S, cities);

        // Solve
        //Solve1(junctionCost, failureProbability);
        //Solve2(junctionCost, failureProbability);
        SearchClosestCities = 4;
        Solve4(junctionCost, failureProbability);
        SearchClosestCities = 7;
        Solve4(junctionCost, failureProbability);
        if (failureProbability <= 0.01)
            Solve3(junctionCost, failureProbability);
        SearchClosestCities = 10;
        Solve4(junctionCost, failureProbability);
        SearchClosestCities = Math.Min(State.cities.Count, 15);
        Solve4(junctionCost, failureProbability);
        SearchClosestCities = State.cities.Count;
        Solve4(junctionCost, failureProbability);

        // Return list of junctions
        int[] junctionsArray = new int[State.junctions.Count * 2];

        for (int i = 0; i < State.junctions.Count; i++)
        {
            junctionsArray[2 * i] = State.junctions[i].X;
            junctionsArray[2 * i + 1] = State.junctions[i].Y;
        }
        return junctionsArray;
    }

    public int[] buildRoads(int[] junctionStatus)
    {
        List<int> idConversion = new List<int>(State.cities.Capacity);
        for (int i = 0; i < State.cities.Count; i++)
            idConversion.Add(i);

        // We need to connect all junctions that have been built
        for (int i = 0; i < junctionStatus.Length; i++)
            if (junctionStatus[i] != 0)
                idConversion.Add(State.cities.Count + i);

        // Include junctions into cities list
        State.ExpandJunctionsIntoCities(idConversion);

        // Find minimal cost spanning tree that picks all cities
        double totalCost;
        List<Road> roads = State.FindMinConnectionCost(out totalCost);

        // Convert roads
        int[] roadsArray = new int[roads.Count * 2];

        for (int i = 0; i < roads.Count; i++)
        {
            roadsArray[2 * i] = roads[i].Id1;
            roadsArray[2 * i + 1] = roads[i].Id2;
        }

        // Convert road ids
        for (int i = 0; i < roadsArray.Length; i++)
            roadsArray[i] = idConversion[roadsArray[i]];
        return roadsArray;
    }

    class JunctionBuilderTester
    {
        public double JunctionCost;
        public double FailureProbability;
        private double[] failureProbabilities;
        private double[] testingJunctionCosts;

        public JunctionBuilderTester(double junctionCost, double failureProbability)
        {
            JunctionCost = junctionCost;
            FailureProbability = failureProbability;
            failureProbabilities = new double[5];
            failureProbabilities[0] = failureProbability;
            for (int i = 1; i < failureProbabilities.Length; i++)
                failureProbabilities[i] = failureProbabilities[i - 1] * failureProbability;
            testingJunctionCosts = new double[failureProbabilities.Length];
            for (int i = 0; i < failureProbabilities.Length; i++)
                testingJunctionCosts[i] = failureProbabilities[i] > 0 ? (junctionCost * (i + 1) + i) / (1 - failureProbabilities[i]) : 0;
        }

        public bool ShouldBuildJunction(double bestSavings)
        {
            return bestSavings > 0 && bestSavings > testingJunctionCosts[0];
        }

        public int GetRecommendedGroupSize(double bestSavings)
        {
            //// Select group size
            //int groupSize = 1;

            //for (int i = 0; i < failureProbabilities.Length; i++)
            //    if (bestSavings > testingJunctionCosts[i])
            //    {
            //        groupSize = i + 1;
            //        if (failureProbabilities[i] < 0.03)
            //            break;
            //    }

            //if (groupSize > 1)
            //    groupSize = 1;

            //if (bestSavings > junctionCost * 3 + 15 && failureProbabilities[1] > 0.04)
            //    groupSize = 3;
            //else if (bestSavings > junctionCost * 2 + 10 && failureProbabilities[0] > 0.04)
            //    groupSize = 2;
            //return groupSize;

            // TODO:
            if (bestSavings > JunctionCost * 3)
                return 3;
            if (bestSavings > JunctionCost * 2)
                return 2;
            return 1;
        }
    }

    private void Solve4(double junctionCost, double failureProbability)
    {
        JunctionBuilderTester jbTester = new JunctionBuilderTester(junctionCost, failureProbability);
        State state = CleanState.Clone();
        double solutionCost = double.MaxValue;
        List<JunctionPoint> junctionPoints = new List<JunctionPoint>();
        City[] locals = new City[3];
        double expectedValueSavings = 0;
        Dictionary<Junction, double> costWithJunction = new Dictionary<Junction, double>();
        Dictionary<int, Dictionary<int, Dictionary<int, List<JunctionPoint>>>> allJunctionPoints = new Dictionary<int, Dictionary<int, Dictionary<int, List<JunctionPoint>>>>();

        while (HasTime && state.junctions.Count < this.State.cities.Count * 2)
        {
            var roads = state.FindMinConnectionCost(out solutionCost);
            double bestSavings = 0;
            List<JunctionPoint> bestJunctionPoints = new List<JunctionPoint>();
            int[] distancesIndexes = state.ExtendDataForOneCity();

            costWithJunction.Clear();
            for (int i = 0; i < state.cities.Count - 1 && HasTime; i++)
            {
                int city1id = i;
                locals[0] = state.cities[i];
                for (int j = 0; j < SearchClosestCities; j++)
                {
                    int city2id = state.cityDistances[i][j].CityId;
                    locals[1] = state.cities[city2id];
                    double d01 = locals[0].Distance(locals[1]);
                    for (int k = j + 1; k < SearchClosestCities; k++)
                    {
                        int city3id = state.cityDistances[i][k].CityId;
                        locals[2] = state.cities[city3id];

                        int minCityid, maxCityid, midCityid;
                        if (city1id < city2id)
                        {
                            if (city2id < city3id)
                            {
                                minCityid = city1id;
                                midCityid = city2id;
                                maxCityid = city3id;
                            }
                            else // city3id < city2id
                            {
                                if (city1id < city3id)
                                {
                                    minCityid = city1id;
                                    midCityid = city3id;
                                    maxCityid = city2id;
                                }
                                else // city3id < city1id
                                {
                                    minCityid = city3id;
                                    midCityid = city1id;
                                    maxCityid = city2id;
                                }
                            }
                        }
                        else // city2id < city1id
                        {
                            if (city1id < city3id)
                            {
                                minCityid = city2id;
                                midCityid = city1id;
                                maxCityid = city3id;
                            }
                            else // city3id < city1id
                            {
                                if (city2id < city3id)
                                {
                                    minCityid = city2id;
                                    midCityid = city3id;
                                    maxCityid = city1id;
                                }
                                else // city3id < city2id
                                {
                                    minCityid = city3id;
                                    midCityid = city2id;
                                    maxCityid = city1id;
                                }
                            }
                        }

                        Dictionary<int, Dictionary<int, List<JunctionPoint>>> minCityDict;
                        if (!allJunctionPoints.TryGetValue(minCityid, out minCityDict))
                        {
                            minCityDict = new Dictionary<int, Dictionary<int, List<JunctionPoint>>>();
                            allJunctionPoints.Add(minCityid, minCityDict);
                        }
                        Dictionary<int, List<JunctionPoint>> midCityDict;
                        if (!minCityDict.TryGetValue(midCityid, out midCityDict))
                        {
                            midCityDict = new Dictionary<int, List<JunctionPoint>>();
                            minCityDict.Add(midCityid, midCityDict);
                        }
                        List<JunctionPoint> maxCityJunctionPoints;
                        double d02 = locals[0].Distance(locals[2]);
                        double d12 = locals[1].Distance(locals[2]);
                        double connectionCost = Math.Min(d01 + d02, Math.Min(d01 + d12, d02 + d12));
                        if (!midCityDict.TryGetValue(maxCityid, out maxCityJunctionPoints))
                        {
                            maxCityJunctionPoints = new List<JunctionPoint>();
                            FindGeometricMedianTopPoints(locals, maxCityJunctionPoints);
                            midCityDict.Add(maxCityid, maxCityJunctionPoints);
                        }
                        junctionPoints = maxCityJunctionPoints;

                        // Find best point inside the triangle
                        int minIndex = 0;
                        for (int l = 1; l < junctionPoints.Count; l++)
                            if (junctionPoints[l].Savings < junctionPoints[minIndex].Savings)
                                minIndex = l;

                        // Check potential savings
                        double potentialSavings = connectionCost - junctionPoints[minIndex].Savings;

                        if (potentialSavings < bestSavings)
                            continue;

                        // Check if we have already checked this junction
                        if (costWithJunction.ContainsKey(junctionPoints[minIndex].Junction))
                            continue;

                        // Has time elapsed?
                        if (!HasTime)
                        {
                            i = state.cities.Count - 2;
                            continue;
                        }

                        // Prepare processing data for new city
                        state.UpdateExtendedDataForOneCity(new City() { X = junctionPoints[minIndex].Junction.X, Y = junctionPoints[minIndex].Junction.Y }, distancesIndexes);

                        // Find junction point savings
                        double tempSolutionCost;
                        var roads2 = state.FindMinConnectionCost(out tempSolutionCost);
                        double savings = solutionCost - tempSolutionCost;

                        if (savings > bestSavings)
                        {
                            bestSavings = savings;
                            bestJunctionPoints.Clear();
                            bestJunctionPoints.AddRange(junctionPoints);
                        }

                        // Add junction into dictionary
                        costWithJunction.Add(junctionPoints[minIndex].Junction, tempSolutionCost);

                        // Clear updated data
                        state.ClearUpdatedExtendedDataForOneCity(distancesIndexes);
                    }
                }
            }

            if (jbTester.ShouldBuildJunction(bestSavings))
            {
                // Select group of junctions that will be added
                int groupSize = jbTester.GetRecommendedGroupSize(bestSavings);

                bestJunctionPoints.Sort((a, b) => Comparer<double>.Default.Compare(a.Savings, b.Savings));
                for (int i = 0; i < groupSize; i++)
                    while (i < bestJunctionPoints.Count && state.cities.Contains(new City() { X = bestJunctionPoints[i].Junction.X, Y = bestJunctionPoints[i].Junction.Y }))
                        bestJunctionPoints.RemoveAt(i);
                bestJunctionPoints = bestJunctionPoints.OrderBy(a => a.Junction.Distance(bestJunctionPoints[0].Junction)).ThenByDescending(a => a.Savings).ToList();
                for (int i = 0; i < groupSize; i++)
                    while (i < bestJunctionPoints.Count && state.cities.Contains(new City() { X = bestJunctionPoints[i].Junction.X, Y = bestJunctionPoints[i].Junction.Y }))
                        bestJunctionPoints.RemoveAt(i);
                if (groupSize > bestJunctionPoints.Count)
                    groupSize = bestJunctionPoints.Count;

                // Try to find best second junction to be added to the group
                if (groupSize >= 2)
                {
                    Junction secondJunction;
                    double evs = CheckSecondJunction(state, distancesIndexes, bestJunctionPoints, solutionCost, bestSavings, jbTester, costWithJunction, out secondJunction);

                    if (evs > 0)
                    {
                        bestJunctionPoints.Insert(1, new JunctionPoint()
                        {
                            Junction = secondJunction,
                            Savings = evs,
                        });
                        expectedValueSavings += evs;
                    }
                    else
                    {
                        groupSize = 1;
                    }
                }

                // Try to find best third junction to be added to the group
                if (groupSize >= 3)
                {
                    Junction thirdJunction;
                    double evs = CheckThirdJunction(state, distancesIndexes, bestJunctionPoints, solutionCost, bestSavings, jbTester, costWithJunction, out thirdJunction);

                    if (evs > 0)
                    {
                        bestJunctionPoints[2] = new JunctionPoint()
                        {
                            Junction = thirdJunction,
                            Savings = evs,
                        };
                        expectedValueSavings += evs;
                    }
                    else
                    {
                        groupSize = 2;
                    }
                }

                // Add junctions from the created group
                for (int i = 0; i < groupSize; i++)
                    state.junctions.Add(bestJunctionPoints[i].Junction);

                // Update expected value
                expectedValueSavings += bestSavings * (1 - failureProbability) - junctionCost;

                // Include new junction into cities list
                state.UpdateExtendedDataForOneCity(new City() { X = bestJunctionPoints[0].Junction.X, Y = bestJunctionPoints[0].Junction.Y }, distancesIndexes);
                for (int i = 1; i < groupSize; i++)
                {
                    distancesIndexes = state.ExtendDataForOneCity();
                    state.UpdateExtendedDataForOneCity(new City() { X = bestJunctionPoints[i].Junction.X, Y = bestJunctionPoints[i].Junction.Y }, distancesIndexes);
                }
            }
            else
                break;
        }

        double cleanSolutionCost;
        CleanState.FindMinConnectionCost(out cleanSolutionCost);
        double extectedValueSolutionCost = cleanSolutionCost - expectedValueSavings;

        // Store solution only if it is better than current
        solutionCost += junctionCost * state.junctions.Count;
        //if (solutionCost < this.State.solutionCost)
        if (extectedValueSolutionCost < this.State.extectedValueSolutionCost)
        {
            this.State.solutionCost = solutionCost;
            this.State.junctions = state.junctions;
            this.State.extectedValueSolutionCost = extectedValueSolutionCost;
        }
    }

    private double CheckThirdJunction(State state, int[] distancesIndexes, List<JunctionPoint> bestJunctionPoints, double originalSolutionCost, double firstJunctionSavings, JunctionBuilderTester jbTester, Dictionary<Junction, double> costWithJunction, out Junction thirdJunction)
    {
        state.UpdateExtendedDataForOneCity(new City() { X = bestJunctionPoints[0].Junction.X, Y = bestJunctionPoints[0].Junction.Y }, distancesIndexes);
        State state2 = state.Clone();
        int[] distancesIndexes2 = state2.ExtendDataForOneCity();
        state2.UpdateExtendedDataForOneCity(new City() { X = bestJunctionPoints[1].Junction.X, Y = bestJunctionPoints[1].Junction.Y }, distancesIndexes2);
        State state3 = state2.Clone();
        int[] distancesIndexes3 = state3.ExtendDataForOneCity();
        state.UpdateExtendedDataForOneCity(new City() { X = bestJunctionPoints[1].Junction.X, Y = bestJunctionPoints[1].Junction.Y }, distancesIndexes);
        State state4 = state.Clone();
        int[] distancesIndexes4 = state4.ExtendDataForOneCity();
        double bestEvs = double.MinValue;
        Junction bestJunction = new Junction();

        // state  - first junction failed, second junction failed
        // state2 - first junction succeeded, second junction failed
        // state3 - first junction succeeded, second junction succeeded
        // state4 - first junction failed, second junction succeeded

        // New stuff
        // a+,b+,c+: (state3 + tj cost) * (1 - fp) ^ 3
        // a+,b-,c+: (state2 + tj cost) * (1-fp)^2*fp
        // a-,b+,c+: (state4 + tj cost) * (1-fp)^2*fp
        // a-,b-,c+: (state + tj cost) * (1-fp)*fp^2
        // Old stuff
        // a+,b+,c-: (state3 cost) * (1-fp)^2*fp
        // a+,b-,c-: (state2 cost) * (1-fp)*fp^2
        // a-,b+,c-: (state4 cost) * (1-fp)*fp^2
        // a-,b-,c-: (state cost) * fp^3


        // Old stuff already calculated:
        // a+,b+: (state3 cost) * (1-fp)^2
        // a-,b+: (state4 cost) * (1-fp)*fp
        // a+,b-: (state2 cost) * (1-fp)*fp
        // a-,b-: (state cost) * (1-fp)^2
        double stateCost, state2Cost, state3Cost, state4Cost;
        state.FindMinConnectionCostWithoutLastCity(out stateCost, distancesIndexes);
        state2.FindMinConnectionCostWithoutLastCity(out state2Cost, distancesIndexes2);
        state3.FindMinConnectionCostWithoutLastCity(out state3Cost, distancesIndexes3);
        state4.FindMinConnectionCostWithoutLastCity(out state4Cost, distancesIndexes4);
        double successProbability = (1 - jbTester.FailureProbability);
        double failureProbability = jbTester.FailureProbability;
        double oldExpectedValue = state3Cost * successProbability * successProbability
            + state4Cost * successProbability * failureProbability
            + state2Cost * successProbability * failureProbability
            + stateCost * failureProbability * failureProbability;
        double oldStuffExpectedValue = oldExpectedValue * failureProbability;
        oldExpectedValue += 2 * jbTester.JunctionCost;

        foreach (Junction junction in bestJunctionPoints.Skip(2).Select(b => b.Junction))
        {
            if (junction.Equals(bestJunctionPoints[1].Junction))
                continue;

            // Find savings for new junction when first one succeeded and second one failed.
            double state2CostExtended;
            state2.UpdateExtendedDataForOneCity(new City() { X = junction.X, Y = junction.Y }, distancesIndexes2);
            state2.FindMinConnectionCost(out state2CostExtended);

            // Find savings for new junction when both first and second one failed.
            double stateCostExtended;
            state.UpdateExtendedDataForOneCity(new City() { X = junction.X, Y = junction.Y }, distancesIndexes);
            state.FindMinConnectionCost(out stateCostExtended);

            // Find savings for new junction when first one failed and second one succeeded.
            double state4CostExtended;
            state4.UpdateExtendedDataForOneCity(new City() { X = junction.X, Y = junction.Y }, distancesIndexes4);
            state4.FindMinConnectionCost(out state4CostExtended);

            // Find savings for new junction when first one failed and second one succeeded.
            double state3CostExtended;
            state3.UpdateExtendedDataForOneCity(new City() { X = junction.X, Y = junction.Y }, distancesIndexes3);
            state3.FindMinConnectionCost(out state3CostExtended);

            // Calculate new stuff expected value
            double newStuffExpectedValue = state3CostExtended * successProbability * successProbability * successProbability
                + state2CostExtended * successProbability * successProbability * failureProbability
                + state4CostExtended * successProbability * successProbability * failureProbability
                + stateCostExtended * successProbability * failureProbability * failureProbability;

            double expectedValue = newStuffExpectedValue + oldStuffExpectedValue + 3 * jbTester.JunctionCost;

            // Calculate best expected value savings
            double evs = oldExpectedValue - expectedValue;

            if (evs > bestEvs)
            {
                bestEvs = evs;
                bestJunction = junction;
            }
        }
        thirdJunction = bestJunction;
        return bestEvs;
    }

    private double CheckSecondJunction(State state, int[] distancesIndexes, List<JunctionPoint> bestJunctionPoints, double originalSolutionCost, double firstJunctionSavings, JunctionBuilderTester jbTester, Dictionary<Junction, double> costWithJunction, out Junction secondJunction)
    {
        state.UpdateExtendedDataForOneCity(new City() { X = bestJunctionPoints[0].Junction.X, Y = bestJunctionPoints[0].Junction.Y }, distancesIndexes);
        State state2 = state.Clone();
        int[] distancesIndexes2 = state2.ExtendDataForOneCity();
        double bestEvs = double.MinValue;
        Junction bestJunction = new Junction();

        if (false && state.cities.Count < 30)
        {
            City[] locals = new City[3];
            List<JunctionPoint> junctionPoints = new List<JunctionPoint>();

            for (int i = 0; i < state2.cities.Count - 1 && HasTime; i++)
            {
                locals[0] = state2.cities[i];
                for (int j = 0; j < SearchClosestCities; j++)
                {
                    locals[1] = state2.cities[state2.cityDistances[i][j].CityId];
                    for (int k = j + 1; k < SearchClosestCities; k++)
                    {
                        locals[2] = state2.cities[state2.cityDistances[i][k].CityId];
                        double connectionCost = FindMinConnectionCost(locals);

                        if (connectionCost / 8 < jbTester.JunctionCost)
                            continue;

                        FindGeometricMedianTopPoints(locals, junctionPoints);

                        // Find best point inside the triangle
                        int minIndex = -1;
                        for (int l = 0; l < junctionPoints.Count; l++)
                            if (minIndex == -1 || junctionPoints[l].Savings < junctionPoints[minIndex].Savings)
                                if (!state2.cities.Contains(new City() { X = junctionPoints[l].Junction.X, Y = junctionPoints[l].Junction.Y }))
                                    minIndex = l;

                        // Calculate savings
                        Junction junction = junctionPoints[minIndex].Junction;

                        // Find savings for new junction when first one succeeded.
                        double savings1;
                        state2.UpdateExtendedDataForOneCity(new City() { X = junction.X, Y = junction.Y }, distancesIndexes2);
                        state2.FindMinConnectionCost(out savings1);
                        savings1 = originalSolutionCost - firstJunctionSavings - savings1;

                        // Find savings for new junction when first one failed.
                        double savings2;
                        if (!costWithJunction.TryGetValue(junction, out savings2))
                        {
                            state.UpdateExtendedDataForOneCity(new City() { X = junction.X, Y = junction.Y }, distancesIndexes);
                            state.FindMinConnectionCost(out savings2);
                            costWithJunction.Add(junction, savings2);
                        }
                        savings2 = originalSolutionCost - savings2;

                        if (Math.Abs(savings1 - savings2) < 0.01)
                            continue;

                        // Calculate best expected value savings
                        double successProbability = (1 - jbTester.FailureProbability);
                        double evs = savings2 * successProbability + (savings1 - savings2) * successProbability * successProbability - jbTester.JunctionCost;

                        if (evs > bestEvs)
                        {
                            bestEvs = evs;
                            bestJunction = junction;
                        }

                        // Clear updated data
                        state2.ClearUpdatedExtendedDataForOneCity(distancesIndexes2);
                    }
                }
            }
        }

        foreach (Junction junction in bestJunctionPoints.Select(b => b.Junction))
        {
            if (junction.Equals(bestJunctionPoints[0].Junction))
                continue;

            // Find savings for new junction when first one succeeded.
            double savings1;
            state2.UpdateExtendedDataForOneCity(new City() { X = junction.X, Y = junction.Y }, distancesIndexes2);
            state2.FindMinConnectionCost(out savings1);
            savings1 = originalSolutionCost - firstJunctionSavings - savings1;

            // Find savings for new junction when first one failed.
            double savings2;
            if (!costWithJunction.TryGetValue(junction, out savings2))
            {
                state.UpdateExtendedDataForOneCity(new City() { X = junction.X, Y = junction.Y }, distancesIndexes);
                state.FindMinConnectionCost(out savings2);
            }
            savings2 = originalSolutionCost - savings2;

            if (Math.Abs(savings1 - savings2) < 0.01)
                continue;

            // Calculate best expected value savings
            double successProbability = (1 - jbTester.FailureProbability);
            double evs = savings2 * successProbability + (savings1 - savings2) * successProbability * successProbability - jbTester.JunctionCost;

            if (evs > bestEvs)
            {
                bestEvs = evs;
                bestJunction = junction;
            }
        }
        secondJunction = bestJunction;
        return bestEvs;
    }

    struct RaySolution
    {
        public State State;
        public double TotalCostBeforeLastJunction;
        public double Savings;
        internal int[] DistancesIndexes; // Used only internaly
        internal Junction Junction; // Last junction to be added after selection, used only internaly

        public double EstimatedCost
        {
            get
            {
                return TotalCostBeforeLastJunction - Savings;
            }
        }
    }

    private void Solve3(double junctionCost, double failureProbability, bool useDeepSearch = true)
    {
        const int RayWidth = 10;
        JunctionBuilderTester jbTester = new JunctionBuilderTester(junctionCost, failureProbability);
        double bestSolution = double.MaxValue;
        List<Junction> bestJunctions = null;
        RaySolution bestEstimatedSolution = new RaySolution()
        {
            Savings = 0,
            TotalCostBeforeLastJunction = double.MaxValue,
        };
        List<RaySolution> ray = new List<RaySolution>();
        List<RaySolution> nextRay = new List<RaySolution>();
        List<JunctionPoint> junctionPoints = new List<JunctionPoint>();
        City[] locals = new City[3];

        ray.Add(new RaySolution()
        {
            Savings = 0,
            TotalCostBeforeLastJunction = 0,
            State = CleanState.Clone(),
        });
        for (int step = 0; ray.Count > 0 && HasTime && step <= this.State.cities.Count * 2; step++)
        {
            nextRay.Clear();
            foreach (RaySolution rs in ray)
            {
                if (!HasTime)
                    break;

                // Find new junctions that can be added to this solution
                double totalCost;
                var roads = rs.State.FindMinConnectionCost(out totalCost);

                totalCost += junctionCost * rs.State.junctions.Count;
                if (totalCost < bestSolution)
                {
                    bestSolution = totalCost;
                    bestJunctions = rs.State.junctions;
                }
                int[] distancesIndexes = rs.State.ExtendDataForOneCity();

                if (!useDeepSearch)
                {
                    Dictionary<int, List<int>> connections = new Dictionary<int, List<int>>();

                    foreach (Road road in roads)
                    {
                        if (!connections.ContainsKey(road.Id1))
                            connections.Add(road.Id1, new List<int>());
                        if (!connections.ContainsKey(road.Id2))
                            connections.Add(road.Id2, new List<int>());
                        connections[road.Id1].Add(road.Id2);
                        connections[road.Id2].Add(road.Id1);
                    }

                    foreach (var kvp in connections)
                    {
                        if (!HasTime)
                            break;

                        int i = kvp.Key;
                        List<int> localConnections = kvp.Value;
                        City[] cities = null;

                        if (localConnections.Count == 2)
                        {
                            cities = new City[5];
                            int found = 0;
                            cities[found++] = rs.State.cities[i];
                            foreach (int j in localConnections)
                            {
                                cities[found++] = rs.State.cities[j];
                                if (connections[j].Count > 1)
                                    cities[found++] = rs.State.cities[connections[j].First(c => c != i)];
                            }
                            if (found < 5)
                                cities = cities.Take(found).ToArray();
                        }
                        else if (localConnections.Count >= 3 && localConnections.Count <= 4)
                        {
                            cities = new City[localConnections.Count + 1];
                            int found = 0;
                            cities[found++] = rs.State.cities[i];
                            foreach (int j in localConnections)
                                cities[found++] = rs.State.cities[j];
                        }

                        if (cities != null)
                        {
                            double savings;
                            Junction junction = FindJunctionPoint(cities, out savings);

                            if (jbTester.ShouldBuildJunction(savings))
                            {
                                RaySolution newRaySolution = new RaySolution()
                                {
                                    State = rs.State,
                                    Savings = savings,
                                    TotalCostBeforeLastJunction = totalCost,
                                    DistancesIndexes = distancesIndexes,
                                    Junction = junction,
                                };
                                nextRay.Add(newRaySolution);
                                if (newRaySolution.EstimatedCost < bestEstimatedSolution.EstimatedCost)
                                    bestEstimatedSolution = newRaySolution;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < rs.State.cities.Count && HasTime; i++)
                    {
                        locals[0] = rs.State.cities[i];
                        for (int j = 0; j < SearchClosestCities; j++)
                        {
                            locals[1] = rs.State.cities[rs.State.cityDistances[i][j].CityId];
                            for (int k = j + 1; k < SearchClosestCities; k++)
                            {
                                locals[2] = rs.State.cities[rs.State.cityDistances[i][k].CityId];
                                double connectionCost = FindMinConnectionCost(locals);

                                if (connectionCost / 8 < junctionCost)
                                    continue;

                                FindGeometricMedianTopPoints(locals, junctionPoints);

                                // Find best point inside the triangle
                                int minIndex = 0;
                                for (int l = 1; l < junctionPoints.Count; l++)
                                    if (junctionPoints[l].Savings < junctionPoints[minIndex].Savings)
                                        minIndex = l;

                                // Check potential savings
                                double potentialSavings = connectionCost - junctionPoints[minIndex].Savings;

                                //if (!jbTester.ShouldBuildJunction(potentialSavings))
                                if (potentialSavings <= 0)
                                    continue;

                                // Prepare processing data for new city
                                rs.State.UpdateExtendedDataForOneCity(new City() { X = junctionPoints[minIndex].Junction.X, Y = junctionPoints[minIndex].Junction.Y }, distancesIndexes);

                                // Find junction point savings
                                double tempSolutionCost;
                                var roads2 = rs.State.FindMinConnectionCost(out tempSolutionCost);
                                tempSolutionCost += junctionCost * rs.State.junctions.Count;
                                double savings = totalCost - tempSolutionCost;

                                if (jbTester.ShouldBuildJunction(savings))
                                {
                                    RaySolution newRaySolution = new RaySolution()
                                    {
                                        State = rs.State,
                                        Savings = savings,
                                        TotalCostBeforeLastJunction = totalCost,
                                        DistancesIndexes = distancesIndexes,
                                        Junction = junctionPoints[minIndex].Junction,
                                    };
                                    nextRay.Add(newRaySolution);
                                    if (newRaySolution.EstimatedCost < bestEstimatedSolution.EstimatedCost)
                                        bestEstimatedSolution = newRaySolution;
                                }

                                // Clear updated data
                                rs.State.ClearUpdatedExtendedDataForOneCity(distancesIndexes);
                            }
                        }
                    }
                }
            }
            nextRay.Sort((rs1, rs2) => Comparer<double>.Default.Compare(rs1.EstimatedCost, rs2.EstimatedCost));
            int newNextRayCount = 1;
            for (int i = 1; newNextRayCount < RayWidth && i < nextRay.Count; i++)
                if (nextRay[i].EstimatedCost != nextRay[i - 1].EstimatedCost)
                {
                    if (i != newNextRayCount)
                        nextRay[newNextRayCount] = nextRay[i];
                    newNextRayCount++;
                }
            if (newNextRayCount < nextRay.Count)
                nextRay.RemoveRange(newNextRayCount, nextRay.Count - newNextRayCount);

            for (int i = 0; i < nextRay.Count; i++)
            {
                State newState = nextRay[i].State.Clone();
                newState.UpdateExtendedDataForOneCity(new City() { X = nextRay[i].Junction.X, Y = nextRay[i].Junction.Y }, nextRay[i].DistancesIndexes);
                newState.junctions.Add(nextRay[i].Junction);
                nextRay[i] = new RaySolution()
                {
                    State = newState,
                    Savings = nextRay[i].Savings,
                    TotalCostBeforeLastJunction = nextRay[i].TotalCostBeforeLastJunction,
                };
            }

            var temp = ray;
            ray = nextRay;
            nextRay = temp;
        }

        // Save best solution
        if (bestSolution < State.solutionCost)
        {
            State.solutionCost = bestSolution;
            State.junctions = bestJunctions;
        }
        if (bestEstimatedSolution.EstimatedCost < State.solutionCost)
        {
            State.solutionCost = bestEstimatedSolution.EstimatedCost;
            State.junctions = bestEstimatedSolution.State.junctions;
        }
    }

    private void Solve2(double junctionCost, double failureProbability)
    {
        for (int startingCity = 0; startingCity < State.cities.Count; startingCity++)
        {
            JunctionBuilderTester jbTester = new JunctionBuilderTester(junctionCost, failureProbability);
            State state = CleanState.Clone();
            double solutionCost = double.MaxValue;
            double expectedValueSavings = 0;
            List<Junction> solutionJunctions = new List<Junction>();
            List<JunctionPoint> junctionPoints = new List<JunctionPoint>();
            Dictionary<Junction, double> costWithJunction = new Dictionary<Junction, double>();

            while (HasTime && solutionJunctions.Count < (state.cities.Count - solutionJunctions.Count) * 2)
            {
                var roads = state.FindMinConnectionCost(out solutionCost, startingCity);
                double bestSavings = 0;
                int[] distanceIndexes = state.ExtendDataForOneCity();
                List<JunctionPoint> bestJunctionPoints = new List<JunctionPoint>();
                Dictionary<int, List<int>> connections = new Dictionary<int, List<int>>();

                foreach (Road road in roads)
                {
                    if (!connections.ContainsKey(road.Id1))
                        connections.Add(road.Id1, new List<int>());
                    if (!connections.ContainsKey(road.Id2))
                        connections.Add(road.Id2, new List<int>());
                    connections[road.Id1].Add(road.Id2);
                    connections[road.Id2].Add(road.Id1);
                }

                foreach (var kvp in connections)
                {
                    int i = kvp.Key;
                    List<int> localConnections = kvp.Value;
                    City[] locals = null;
                    double localsCost = 0;

                    if (localConnections.Count == 2)
                    {
                        locals = new City[5];
                        int found = 0;
                        locals[found++] = state.cities[i];
                        foreach (int j in localConnections)
                        {
                            locals[found++] = state.cities[j];
                            localsCost += state.cities[i].Distance(state.cities[j]);
                            if (connections[j].Count > 1)
                            {
                                int id = connections[j].First(c => c != i);
                                locals[found++] = state.cities[id];
                                localsCost += state.cities[j].Distance(state.cities[id]);
                            }
                        }
                        if (found < 5)
                            locals = locals.Take(found).ToArray();
                    }
                    else if (localConnections.Count >= 3)
                    {
                        locals = new City[localConnections.Count + 1];
                        int found = 0;
                        locals[found++] = state.cities[i];
                        foreach (int j in localConnections)
                        {
                            locals[found++] = state.cities[j];
                            localsCost += state.cities[i].Distance(state.cities[j]);
                        }
                    }

                    if (locals != null)
                    {
                        FindTopJunctionPoints(locals, junctionPoints, localsCost);

                        if (junctionPoints[0].Savings > bestSavings)
                        {
                            bestSavings = junctionPoints[0].Savings;
                            bestJunctionPoints.Clear();
                            bestJunctionPoints.AddRange(junctionPoints);
                        }
                    }
                }

                if (jbTester.ShouldBuildJunction(bestSavings))
                {
                    int groupSize = jbTester.GetRecommendedGroupSize(bestSavings);

                    bestJunctionPoints = bestJunctionPoints.OrderBy(a => a.Junction.Distance(bestJunctionPoints[0].Junction)).ThenByDescending(a => a.Savings).ToList();
                    for (int i = 0; i < groupSize; i++)
                        while (i < bestJunctionPoints.Count && state.cities.Contains(new City() { X = bestJunctionPoints[i].Junction.X, Y = bestJunctionPoints[i].Junction.Y }))
                            bestJunctionPoints.RemoveAt(i);
                    if (groupSize > bestJunctionPoints.Count)
                        groupSize = bestJunctionPoints.Count;
                    for (int i = 0; i < groupSize; i++)
                        solutionJunctions.Add(bestJunctionPoints[i].Junction);

                    // Try to find best second junction to be added to the group
                    if (groupSize >= 2)
                    {
                        Junction secondJunction;
                        double evs = CheckSecondJunction(state, distanceIndexes, bestJunctionPoints, solutionCost, bestSavings, jbTester, costWithJunction, out secondJunction);

                        if (evs > 0)
                        {
                            bestJunctionPoints[1] = new JunctionPoint()
                            {
                                Junction = secondJunction,
                                Savings = evs,
                            };
                            expectedValueSavings += evs;
                        }
                        else
                        {
                            groupSize = 1;
                        }
                    }

                    // Add junctions from the created group
                    for (int i = 0; i < groupSize; i++)
                        state.junctions.Add(bestJunctionPoints[i].Junction);

                    // Update expected value
                    expectedValueSavings += bestSavings * (1 - failureProbability) - junctionCost;

                    // Include new junction into cities list
                    state.UpdateExtendedDataForOneCity(new City() { X = bestJunctionPoints[0].Junction.X, Y = bestJunctionPoints[0].Junction.Y }, distanceIndexes);
                    for (int i = 1; i < groupSize; i++)
                    {
                        distanceIndexes = state.ExtendDataForOneCity();
                        state.UpdateExtendedDataForOneCity(new City() { X = bestJunctionPoints[i].Junction.X, Y = bestJunctionPoints[i].Junction.Y }, distanceIndexes);
                    }
                }
                else
                    break;
            }

            double cleanSolutionCost;
            CleanState.FindMinConnectionCost(out cleanSolutionCost);
            double extectedValueSolutionCost = cleanSolutionCost - expectedValueSavings;

            // Store solution only if it is better than current
            solutionCost += junctionCost * solutionJunctions.Count;
            if (extectedValueSolutionCost < this.State.extectedValueSolutionCost)
            {
                this.State.solutionCost = solutionCost;
                this.State.junctions = solutionJunctions;
                this.State.extectedValueSolutionCost = extectedValueSolutionCost;
            }
        }
    }

    internal struct JunctionPoint
    {
        public Junction Junction;
        public double Savings;
    }

    internal static void FindTopJunctionPoints(City[] cities, List<JunctionPoint> junctionPoints, double citiesConnectionCost)
    {
        FindTopCostSavingsJunctionPoints(cities, junctionPoints);
        for (int i = 0; i < junctionPoints.Count; i++)
            junctionPoints[i] = new JunctionPoint()
            {
                Junction = junctionPoints[i].Junction,
                Savings = citiesConnectionCost - junctionPoints[i].Savings,
            };
    }

    internal static void FindTopCostSavingsJunctionPoints(City[] points, List<JunctionPoint> junctionPoints) // junctionPoints contain cost instead of savings
    {
        // Start with Geometric median point and star distance
        List<JunctionPoint> jp = new List<JunctionPoint>();
        FindGeometricMedianTopPoints(points, jp);
        double bestSavings = double.MaxValue;
        for (int i = 0; i < jp.Count; i++)
            if (jp[i].Savings < bestSavings)
                bestSavings = jp[i].Savings;
        junctionPoints.Clear();
        junctionPoints.AddRange(jp);

        // Try reducing to triangle + rest of the points
        if (points.Length > 3)
        {
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
                        FindGeometricMedianTopPoints(triangle, jp);
                        int minIndex = 0;
                        for (int i = 1; i < jp.Count; i++)
                            if (jp[i].Savings < jp[minIndex].Savings)
                                minIndex = i;

                        extendedPoints[points.Length] = new City() { X = jp[minIndex].Junction.X, Y = jp[minIndex].Junction.Y };
                        jp[minIndex] = new JunctionPoint()
                        {
                            Junction = jp[minIndex].Junction,
                            Savings = FindMinConnectionCost(extendedPoints),
                        };

                        double savings = jp[minIndex].Savings;

                        if (savings < bestSavings)
                        {
                            bestSavings = savings;
                            for (int i = 0; i < jp.Count; i++)
                                if (i != minIndex)
                                {
                                    extendedPoints[points.Length] = new City() { X = jp[i].Junction.X, Y = jp[i].Junction.Y };
                                    jp[i] = new JunctionPoint()
                                    {
                                        Junction = jp[i].Junction,
                                        Savings = FindMinConnectionCost(extendedPoints),
                                    };
                                }
                            junctionPoints.Clear();
                            junctionPoints.AddRange(jp);
                        }
                    }
                }
            }
        }
        junctionPoints.Sort((a, b) => Comparer<double>.Default.Compare(a.Savings, b.Savings));
    }

    internal static void FindGeometricMedianTopPoints(City[] points, List<JunctionPoint> junctionPoints) // junctionPoints contain cost instead of savings
    {
        const double gmError = 0.1;
        double gmx = points[0].X + 0.5;
        double gmy = points[0].Y + 0.5;
        bool workGm = true;

        while (workGm)
        {
            double d = 0;
            double nx = 0;
            double ny = 0;
            for (int i = 0; i < points.Length; i++)
            {
                double distance = Math.Sqrt((gmx - points[i].X) * (gmx - points[i].X) + (gmy - points[i].Y) * (gmy - points[i].Y));
                double d1 = 1 / distance;

                d += 1 * d1;
                nx += points[i].X * d1;
                ny += points[i].Y * d1;
            }

            d = 1 / d;
            nx *= d;
            ny *= d;
            workGm = Math.Abs(gmx - nx) > gmError || Math.Abs(gmy - ny) > gmError;
            gmx = nx;
            gmy = ny;
        }

        junctionPoints.Clear();

        for (int x = -1; x <= 2; x++)
            for (int y = -1; y <= 2; y++)
            {
                City point = new City() { X = (int)gmx + x, Y = (int)gmy + y };
                junctionPoints.Add(new JunctionPoint()
                {
                    Junction = new Junction() { X = point.X, Y = point.Y },
                    Savings = StarDistance(point, points),
                });
            }
    }

    internal static Junction FindJunctionPoint(City[] cities, out double savings)
    {
        double costWithPoint;
        City savingPoint = FindCostSavingsPoint(cities, out costWithPoint);
        double originalCost = FindMinConnectionCost(cities);

        savings = originalCost - costWithPoint;
        return new Junction() { X = savingPoint.X, Y = savingPoint.Y };
    }

    internal static City FindCostSavingsPoint(City[] points, out double costWithPoint)
    {
        // Start with Geometric median point and star distance
        City extendedPoint = FindGeometricMedianPoint(points, out costWithPoint);

        // Try reducing to triangle + rest of the points
        if (points.Length > 3)
        {
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

                        if (distance < costWithPoint)
                        {
                            costWithPoint = distance;
                            extendedPoint = extendedPoints[points.Length];
                        }
                    }
                }
            }
        }
        return extendedPoint;
    }

    internal static double FindMinConnectionCost(City[] points)
    {
        bool[] picked = new bool[points.Length];
        int[,] distances = new int[points.Length, points.Length];
        int[] q = new int[points.Length];
        double cost = 0;

        for (int i = 0; i < points.Length; i++)
            for (int j = i + 1; j < points.Length; j++)
                distances[i, j] = points[i].Distance2(points[j]);

        q[0] = 0;
        picked[0] = true;
        for (int selected = 1; selected < points.Length; selected++)
        {
            int minDistance2 = int.MaxValue;
            int id = -1;

            for (int qi = 0; qi < selected; qi++)
            {
                int i = q[qi];

                for (int j = i + 1; j < points.Length; j++)
                    if (!picked[j])
                    {
                        int distance2 = distances[i, j];

                        if (distance2 < minDistance2)
                        {
                            minDistance2 = distance2;
                            id = j;
                        }
                    }
                for (int j = 0; j < i; j++)
                    if (!picked[j])
                    {
                        int distance2 = distances[j, i];

                        if (distance2 < minDistance2)
                        {
                            minDistance2 = distance2;
                            id = j;
                        }
                    }
            }
            picked[id] = true;
            q[selected] = id;
            cost += Math.Sqrt(minDistance2);
        }
        return cost;
    }

    internal static City FindGeometricMedianPoint(City[] points)
    {
        double newDistance;

        return FindGeometricMedianPoint(points, out newDistance);
    }

    internal static City FindGeometricMedianPoint(City[] points, out double newDistance)
    {
        const double gmError = 0.00001;
        double gmx = points.Average(p => p.X);
        double gmy = points.Average(p => p.Y);
        bool workGm = true;
        int gmIterations = 0;

        while (workGm)
        {
            double d = 0;
            double nx = 0;
            double ny = 0;
            for (int i = 0; i < points.Length; i++)
            {
                double distance = Math.Sqrt((gmx - points[i].X) * (gmx - points[i].X) + (gmy - points[i].Y) * (gmy - points[i].Y));

                d += 1 / distance;
                nx += points[i].X / distance;
                ny += points[i].Y / distance;
            }

            nx /= d;
            ny /= d;
            workGm = Math.Abs(gmx - nx) > gmError || Math.Abs(gmy - ny) > gmError;
            gmx = nx;
            gmy = ny;
            gmIterations++;
        }

        double minDistance = double.MaxValue;
        City minPoint = new City();

        for (int x = -1; x <= 2; x++)
            for (int y = -1; y <= 2; y++)
            {
                City point = new City() { X = (int)gmx + x, Y = (int)gmy + y };
                double distance = StarDistance(point, points);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    minPoint = point;
                }
            }
        newDistance = minDistance;
        return minPoint;
    }

    internal static double StarDistance(City point, City[] points)
    {
        double distance = 0;

        for (int i = 0; i < points.Length; i++)
            distance += point.Distance(points[i]);
        return distance;
    }
}
