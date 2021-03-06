// MM-2018-R2-Cpp.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "Online.h"
#include <fstream>

template<class T> void getVector(istream& in, vector<T>& v)
{
    for (int i = 0; i < (int)v.size(); ++i)
        in >> v[i];
}

void ReadTest(istream& in, vector<string>& targetBoard, int& costLantern, int& costMirror, int& costObstacle, int& maxMirrors, int& maxObstacles)
{
    int H;
    in >> H;
    targetBoard.resize((size_t)H);
    getVector(in, targetBoard);
    in >> costLantern >> costMirror >> costObstacle >> maxMirrors >> maxObstacles;
}

void RunTest(int test)
{
    stringstream ss;
    ss << "inputs\\" << test << ".txt";
    ifstream fin(ss.str());
    vector<string> targetBoard;
    int costLantern, costMirror, costObstacle, maxMirrors, maxObstacles;

    ReadTest(fin, targetBoard, costLantern, costMirror, costObstacle, maxMirrors, maxObstacles);
    CrystalLighting().placeItems(targetBoard, costLantern, costMirror, costObstacle, maxMirrors, maxObstacles);
}

int main(int argc, char** argv)
{
    if (argc == 1)
    {
        for (int i = 1; i <= 10; i++)
            RunTest(i);
    }
    else
    {
        CrystalLighting cl;
        vector<string> targetBoard;
        int costLantern, costMirror, costObstacle, maxMirrors, maxObstacles;
        ReadTest(cin, targetBoard, costLantern, costMirror, costObstacle, maxMirrors, maxObstacles);
        vector<string> ret = cl.placeItems(targetBoard, costLantern, costMirror, costObstacle, maxMirrors, maxObstacles);
        cout << ret.size() << endl;
        for (int i = 0; i < (int)ret.size(); ++i)
            cout << ret[i] << endl;
        cout.flush();
    }
}
