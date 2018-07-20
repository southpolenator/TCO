#include <vector>
#include <algorithm>
#include <iostream>

using namespace std;

#define TEST_MONEY 100
#define GUARANTEED_BET 0.0
#define TRUST_BORDER 0.1
#define MIN_ADVICE 0.0
#define MIN_BETTINGS_FOR_LEARNING 0
#define MAX_BET 400000
#define MAX_BET2 350000
#define NEGATIVE_TRUST 1


class InvestmentAdvice
{
public:
    vector<int> getInvestments(vector<int> advice, vector<int> recent, int money, int timeLeft, int roundsLeft)
    {
        auto result = Solve2(advice, recent, money, timeLeft, roundsLeft);

#ifdef _DEBUG
        fprintf(stderr, "%d. Money: %d\n", (int)previousBettings.size(), money);
        if (previousBettings.size() > 1)
        {
            fprintf(stderr, "Expert|invest|income|advice-1|recent|trust|error| bonus  |  fake  | total  |\n");
            for (int i = 0; i < advice.size(); i++)
            {
                int total = 0, bonus = 0, fake = 0;
                int t = 0;
                double trust, error = 0;
                for (int j = 1; j < previousRecent.size(); j++)
                {
                    total += previousRecent[j][i];
                    int expected = (int)(previousAdvices[j - 1][i] / 100.0 * previousBettings[j - 1][i]);
                    if (previousRecent[j][i] >= expected)
                        bonus += previousRecent[j][i] - expected;
                    else
                        fake += expected - previousRecent[j][i];

                    {
                        double expected = previousAdvices[j - 1][i] / 100.0;
                        double actual = previousRecent[j][i] / (double)previousBettings[j - 1][i];

                        if (std::abs(expected - actual) < TRUST_BORDER)
                        {
                            t++;
                            error += expected - actual;
                        }
                    }
                }
                error = t > 0 ? error * 100 / t : 0;
                trust = t / ((double)previousAdvices.size() - 1);
                size_t n = previousAdvices.size() - 2;
                fprintf(stderr, "%5d |%6d|%6d| %5d  | %5.1lf|%5.1lf|%5.1lf|%8d|%8d|%8d|\n", i, previousBettings[n][i], previousRecent[n + 1][i], previousAdvices[n][i], recent[i] * 100.0 / previousBettings[n][i], trust, error, bonus, fake, total);
            }
        }
        fflush(stderr);
#endif
        return result;
    }

private:
    vector<int> Solve3(vector<int> advice, vector<int> recent, int money, int timeLeft, int roundsLeft)
    {
        vector<bool> done(advice.size(), false);
        vector<int> ret(advice.size(), 0);

        // Find number of possible bets
        int positive = 0;

        for (size_t i = 0; i < advice.size(); i++)
            if (advice[i] > 0)
                positive++;

        // Assign money
        int maxBet = positive * MAX_BET2 < money ? MAX_BET2 : MAX_BET;

        while (money > 0)
        {
            int max = -1;

            for (size_t i = 0; i < advice.size(); i++)
                if (!done[i] && advice[i] / 100.0 > MIN_ADVICE)
                {
                    if (max == -1 || advice[i] > advice[max])
                        max = i;
                }
            if (max == -1)
                break;

            done[max] = true;
            ret[max] = std::min(maxBet, money);
            money -= ret[max];
        }
        previousBettings.push_back(ret);
        return ret;
    }

    vector<int> Solve2(vector<int> advice, vector<int> recent, int money, int timeLeft, int roundsLeft)
    {
        bool executeTrust = roundsLeft + previousBettings.size() >= 14;
        vector<bool> done(advice.size(), false);
        int tm = roundsLeft == 1 || !executeTrust ? 0 : TEST_MONEY;
        int testMoney = advice.size() * tm;
        vector<int> ret(advice.size(), tm);

        // calculate trust
        vector<double> trust(advice.size(), 1.0);
        vector<double> error(advice.size(), 0.0);

        previousAdvices.push_back(advice);
        previousRecent.push_back(recent);
        if (!previousBettings.empty() && executeTrust)
        {
            for (size_t i = 0; i < advice.size(); i++)
            {
                int t = 0;
                double e = 0;

                for (size_t j = 0; j < previousBettings.size(); j++)
                {
                    double expected = previousAdvices[j][i] / 100.0;
                    double actual = previousRecent[j + 1][i] / (double)previousBettings[j][i];

                    if (std::abs(expected - actual) < TRUST_BORDER)
                    {
                        t++;
                        e += expected - actual;
                    }
                }

                trust[i] = t / (double)previousBettings.size();
                error[i] = t > 0 ? e * 100 / t : 0;
            }
        }

        // Assign money
        bool first = false;

        money -= testMoney;
        while (money > 0)
        {
            int max = -1;

            for (size_t i = 0; i < advice.size(); i++)
                if (!done[i] && advice[i] / 100.0 > MIN_ADVICE)
                {
                    if (max == -1 || advice[i] * trust[i] > advice[max] * trust[max])
                        max = i;
                }
            if (max == -1)
                break;
            if (first)
            {
                first = false;
                advice[max] = 1;
                continue;
            }

            done[max] = true;
            ret[max] = std::min(MAX_BET, money);
            money -= ret[max] - tm;
        }
        previousBettings.push_back(ret);
        return ret;
    }

    vector<int> Solve1(const vector<int>& advice, const vector<int>& recent, int money, int timeLeft, int roundsLeft)
    {
        vector<int> ret(advice.size());
        int tm = roundsLeft == 1 ? 0 : TEST_MONEY;
        int testMoney = advice.size() * tm;

        // calculate trust
        vector<double> trust(advice.size(), 1.0);

        previousAdvices.push_back(advice);
        previousRecent.push_back(recent);
        if (!previousBettings.empty())
        {
            for (size_t i = 0; i < advice.size(); i++)
            {
                int t = 0;

                for (size_t j = 0; j < previousBettings.size(); j++)
                {
                    double expected = previousAdvices[j][i] / 100.0;
                    double actual = previousRecent[j + 1][i] / (double)previousBettings[j][i];

                    if (std::abs(expected - actual) < TRUST_BORDER)
                        t++;
                }

                trust[i] = t / (double)previousBettings.size();
            }
        }

        // Calculate amounts
        vector<double> amounts(advice.size(), 0.0);
        double sum = 0;

        if (previousBettings.size() >= MIN_BETTINGS_FOR_LEARNING)
            for (size_t i = 0; i < advice.size(); i++)
            {
                double a = advice[i] / 100.0;

                if (a > MIN_ADVICE)
                {
                    amounts[i] = GUARANTEED_BET + (1 - GUARANTEED_BET) * std::pow(trust[i], 1) * std::pow(a - MIN_ADVICE, 1);
                    if (amounts[i] > 0)
                        sum += amounts[i];
                }
            }

        // Find max value
        double ma = (MAX_BET - tm) / (double)(money - testMoney) * sum;
        bool changed = true;
        double newSum = sum;
        int lastCount = 0;
        double sumUsed = 0;

        while (changed)
        {
            int maxValues = 0;
            sumUsed = 0;
            changed = false;
            for (size_t i = 0; i < advice.size(); i++)
                if (amounts[i] >= ma)
                {
                    sumUsed += amounts[i];
                    maxValues++;
                }
            if (sumUsed > 0)
            {
                for (int i = 0; i < 100; i++)
                {
                    newSum = sum - sumUsed + maxValues * ma;
                    ma = (MAX_BET - tm) / (double)(money - testMoney) * newSum;
                }
                changed = maxValues > lastCount;
                lastCount = maxValues;
            }
        }
        if (sumUsed < sum * 0.9)
            sum = newSum;

        // Assign betting
        for (size_t i = 0; i < advice.size(); i++)
            if (amounts[i] > 0)
            {
                int bet = (int)(amounts[i] / sum * (money - testMoney)) + tm;
                ret[i] = std::max(tm, std::min(MAX_BET, bet));
            }
            else
                ret[i] = tm;
        previousBettings.push_back(ret);
        return ret;
    }

private:
    vector<vector<int>> previousAdvices;
    vector<vector<int>> previousRecent;
    vector<vector<int>> previousBettings;
};
