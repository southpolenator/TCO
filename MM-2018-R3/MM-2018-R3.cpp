// MM-2018-R3.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "Online.h"
#include <cstdint>
#include <cmath>
#include <fstream>
#include <map>
#include <array>

class JavaRandom
{
protected:
    JavaRandom()
        : haveNextNextGaussian(false)
    {
    }

public:
    JavaRandom(int64_t seed)
    {
        setSeed(seed);
    }

    void setSeed(int64_t seed)
    {
        this->seed = (seed ^ 0x5DEECE66DLL) & ((1LL << 48) - 1);
        haveNextNextGaussian = false;
    }

    int nextInt()
    {
        return next(32);
    }

    int nextIntFastNotPow2(int n)
    {
        int bits, val;
        do
        {
            bits = next(31);
            val = bits % n;
        } while (bits - val + (n - 1) < 0);
        return val;
    }

    int nextInt(int n)
    {
        if (n <= 0)
            throw "n must be positive";

        if ((n & -n) == n)  // i.e., n is a power of 2
            return (int)((n * (int64_t)next(31)) >> 31);

        int bits, val;
        do
        {
            bits = next(31);
            val = bits % n;
        } while (bits - val + (n - 1) < 0);
        return val;
    }

    int64_t nextLong()
    {
        return ((int64_t)next(32) << 32) + next(32);
    }

    bool nextBoolean()
    {
        return next(1) != 0;
    }

    float nextFloat()
    {
        return next(24) / ((float)(1 << 24));
    }

    double nextDouble()
    {
        return (((int64_t)next(26) << 27) + next(27)) / (double)(1LL << 53);
    }

    double nextGaussian()
    {
        if (haveNextNextGaussian)
        {
            haveNextNextGaussian = false;
            return nextNextGaussian;
        }
        else
        {
            double v1, v2, s;
            do
            {
                v1 = 2 * nextDouble() - 1;   // between -1.0 and 1.0
                v2 = 2 * nextDouble() - 1;   // between -1.0 and 1.0
                s = v1 * v1 + v2 * v2;
            } while (s >= 1 || s == 0);
            double multiplier = std::sqrt(-2 * std::log(s) / s);
            nextNextGaussian = v2 * multiplier;
            haveNextNextGaussian = true;
            return v1 * multiplier;
        }
    }

protected:
    int next(int bits)
    {
        seed = (seed * 0x5DEECE66DLL + 0xB) & ((1LL << 48) - 1);
        return (int)((uint64_t)seed >> (unsigned)(48 - bits));
    }

    void simulateNext()
    {
        seed = seed * 0x5DEECE66DLL + 0xB;
    }

protected:
    int64_t seed;
    bool haveNextNextGaussian;
    double nextNextGaussian;
};

template<class T> void getVector(vector<T>& v)
{
    for (size_t i = 0; i < v.size(); ++i)
        cin >> v[i];
}

class Generator
{
public:
    Generator(int seed)
        : r(seed)
    {
        numExperts = 10 + r.nextInt(41);
        for (int i = 0; i < numExperts; i++)
        {
            stDev[i] = r.nextDouble() * 20;
            accuracy[i] = r.nextDouble();
        }
        numPeriods = r.nextInt(91) + 10;
        reported.resize(numExperts);
    }

    int numExperts;
    int numPeriods;
    double accuracy[50];
    double stDev[50];
    double actual[50];
    vector<int> reported;

    void InitializeNextStep()
    {
        for (int i = 0; i < numExperts; i++)
        {
            actual[i] = std::min(1.0, std::max(-1.0, r.nextGaussian() * 0.1));
            if (r.nextDouble() < accuracy[i])
                reported[i] = (int)std::round(std::min(100.0, std::max(-100.0, r.nextGaussian() * stDev[i] + actual[i] * 100)));
            else
                reported[i] = (int)std::round(100 * std::min(1.0, std::max(-1.0, r.nextGaussian() * 0.1)));
        }
    }

    void ProcessStep(const vector<int>& result, int& money, vector<int>& recent)
    {
        int invested = 0, returned = 0;

        if (result.size() != reported.size())
            throw "Incorect number of experts in result";
        recent.resize(numExperts);
        for (int i = 0; i < numExperts; i++)
        {
            if (result[i] < 0)
                throw "result negative";
            if (result[i] > 400000)
                throw "result too big";
            invested += result[i];
            recent[i] = (int)std::floor(result[i] * actual[i]);
            returned += recent[i] + result[i];
        }
        if (invested > money)
            throw "invested more that you have";
        money += returned - invested;
    }

private:
    JavaRandom r;
};

int RunTest(int seed, int* numExpertsOut = nullptr, int* numPeriodsOut = nullptr)
{
    Generator g(seed);

    if (numExpertsOut != nullptr)
        *numExpertsOut = g.numExperts;
    if (numPeriodsOut != nullptr)
        *numPeriodsOut = g.numPeriods;

    InvestmentAdvice advice;
    int money = 1000000;
    vector<int> recent(g.numExperts, 0);

    for (int p = 0; p < g.numPeriods; p++)
    {
        g.InitializeNextStep();
        auto result = advice.getInvestments(g.reported, recent, money, 0, g.numPeriods - p);
        g.ProcessStep(result, money, recent);
    }
    return money;
}

void TestNegativeChain(int seed, array<double, 50>& result)
{
    Generator g(seed);
    int negativeChain[50] = { 0 };

    for (int p = 0; p < g.numPeriods; p++)
    {
        g.InitializeNextStep();
        for (int i = 0; i < g.numExperts; i++)
        {
            if (g.reported[i] > 0)
                result[negativeChain[i]] += g.actual[i];
            if (g.reported[i] < 0)
                negativeChain[i]++;
            else
                negativeChain[i] = 0;
        }
    }
}

void TestNegativeChain()
{
    array<double, 50> expectedValue = { 0 };

    for (int i = 1; i <= 1000; i++)
        TestNegativeChain(i, expectedValue);

    for (int i = 0; i < 50; i++)
        if (expectedValue[i] != 0)
            printf("%2d: %6.2lf%%\n", i, expectedValue[i] * 100);
}

void RunAllTests()
{
    ofstream output("output.txt");
    ifstream input("previous.txt");
    double totalScore = 0;

    for (int i = 1; i <= 1000; i++)
    {
        int numExperts, numPeriods;
        int score = RunTest(i, &numExperts, &numPeriods);
        int previousScore;
        double percent;

        input >> previousScore;
        output << score << endl;
        percent = score * 100.0 / previousScore;
        totalScore += percent;
        printf("%4d. (%2d, %3d) %8d / %8d [%5.1lf%%]\n", i, numExperts, numPeriods, score, previousScore, percent);
    }
    totalScore /= 1000;
    printf("Total score: %.2lf%%\n", totalScore);
}

int main(int argc, char** agrv)
{
    if (argc == 1)
    {
        //TestNegativeChain();
        //return 0;
#ifdef _DEBUG
        printf("Score: %d\n", RunTest(975));
#else
        RunAllTests();
#endif
    }
    else
    {
        InvestmentAdvice sol;
        int roundsLeft = 99;
        while (roundsLeft > 1) {
            int A;
            cin >> A;
            vector<int> advice(A);
            getVector(advice);
            int R;
            cin >> R;
            vector<int> recent(R);
            getVector(recent);
            int money;
            int timeLeft;
            cin >> money;
            cin >> timeLeft;
            cin >> roundsLeft;
            vector<int> ret = sol.getInvestments(advice, recent, money, timeLeft, roundsLeft);
            cout << ret.size() << endl;
            for (int i = 0; i < (int)ret.size(); ++i)
                cout << ret[i] << endl;
            cout.flush();
        }
    }
    return 0;
}
