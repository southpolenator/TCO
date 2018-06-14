#include <vector>
#include <string>
#include <sstream>
#include <iostream>
#include <algorithm>
#include <queue>
#include <memory>

using namespace std;

#if LOCAL
#if _DEBUG
#define MAX_EXECUTION_TIME 20000.5
#else
#define MAX_EXECUTION_TIME 5.5
#endif
#else
#define MAX_EXECUTION_TIME 9.5
#endif
//#define USE_SLOW_ALGORITHM
#define USE_POTENTIAL_SCORE

#ifndef WIN32

#include <sys/time.h>

double getTime()
{
    timeval tv;
    gettimeofday(&tv, 0);
    return tv.tv_sec + tv.tv_usec * 1e-6;
}

#else

#include <Windows.h>

double getTime()
{
    return GetTickCount() / 1000.0;
}

#endif

typedef char int8;
typedef short int16;

template<class T> inline T operator~ (T a) { return (T)~(int)a; }
template<class T> inline T operator| (T a, T b) { return (T)((int)a | (int)b); }
template<class T> inline T operator& (T a, T b) { return (T)((int)a & (int)b); }
template<class T> inline T operator^ (T a, T b) { return (T)((int)a ^ (int)b); }
template<class T> inline T& operator|= (T& a, T b) { return (T&)((int&)a |= (int)b); }
template<class T> inline T& operator&= (T& a, T b) { return (T&)((int&)a &= (int)b); }
template<class T> inline T& operator^= (T& a, T b) { return (T&)((int&)a ^= (int)b); }

enum class Color : unsigned char
{
    Empty = 0x0,
    Blue = 0x1,
    Yellow = 0x2,
    Red = 0x4,
};

enum class BoardField : unsigned char
{
    Empty = 0x0,
    Blue = 0x1,
    Yellow = 0x2,
    Red = 0x4,
    ColorMask = Blue | Yellow | Red,
    Lantern = 0x8,
    MirrorSlash = 0x10,
    MirrorBackSlash = 0x20,
    MirrorMask = MirrorSlash | MirrorBackSlash,
    Obstacle = 0x40,
    Crystal = 0x80,
    ObjectMask = Lantern | MirrorBackSlash | MirrorSlash | Obstacle | Crystal,
};

enum class Light : unsigned short
{
    Empty = 0x0,
    Blue = 0x1,
    Yellow = 0x2,
    Red = 0x4,
    ColorMask = Blue | Yellow | Red,
    BlueLeft = 0x8,
    BlueRight = 0x10,
    BlueUp = 0x20,
    BlueDown = 0x40,
    BlueDirectionMask = BlueLeft | BlueRight | BlueUp | BlueDown,
    YellowLeft = 0x80,
    YellowRight = 0x100,
    YellowUp = 0x200,
    YellowDown = 0x400,
    YellowDirectionMask = YellowLeft | YellowRight | YellowUp | YellowDown,
    RedLeft = 0x800,
    RedRight = 0x1000,
    RedUp = 0x2000,
    RedDown = 0x4000,
    RedDirectionMask = RedLeft | RedRight | RedUp | RedDown,
    LeftMask = BlueLeft | YellowLeft | RedLeft,
    RightMask = BlueRight | YellowRight | RedRight,
    UpMask = BlueUp | YellowUp | RedUp,
    DownMask = BlueDown | YellowDown | RedDown,
};

struct Position
{
    int8 x;
    int8 y;

    Position()
    {
    }

    Position(int8 x, int8 y)
        : x(x)
        , y(y)
    {
    }

    bool operator==(const Position& p) const
    {
        return x == p.x && y == p.y;
    }
};

struct Obstacle
{
    Position position;

    int GetHash() const
    {
        return (position.x << 17) ^ (position.y << 9);
    }
};

struct Mirror
{
    Position position;
    bool slash;

    int GetHash() const
    {
        BoardField mirrorType = slash ? BoardField::MirrorSlash : BoardField::MirrorBackSlash;

        return (position.x << 11) ^ (position.y << 3) ^ ((int)mirrorType << 20);
    }
};

struct Lantern
{
    Position position;
    Color color;

    int GetHash() const
    {
        return ((int)color << 16) ^ position.x ^ (position.y << 8);
    }
};

enum class MoveType : int8
{
    Lantern,
    Obstacle,
    Mirror,
};

struct State;

struct Move
{
    State* state;
    int score;
    int potentialScore;
    int hash;
    union
    {
        Lantern lantern;
        Obstacle obstacle;
        Mirror mirror;
    };
    MoveType type;

    Move()
        : state(nullptr)
    {
    }

    Move(State* state, Lantern lantern, int cost);
    Move(State* state, Obstacle obstacle, int cost);
    Move(State* state, Mirror mirror, int cost);

    void ApplyToMe(int costLantern, int costObstacle, int costMirror);
    State Apply(int costLantern, int costObstacle, int costMirror) const;
    bool Same(const Move& other) const;
};


struct MoveComparisonType
{
    bool operator()(const Move& m1, const Move& m2) const
    {
#ifdef USE_POTENTIAL_SCORE
        return m2.potentialScore < m1.potentialScore;
#else
        return m2.score < m1.score;
#endif
    }
};

struct PrecalculatedMoves
{
    Move moves[3];
    int8 movesCount;

    PrecalculatedMoves()
        : movesCount(-1)
    {
    }
};

struct State
{
    BoardField* board;
    Light* lightMap;
    Light* crystalsLightMap;
    int16* crystalsFromLeft;
    int16* crystalsFromRight;
    int16* crystalsFromUp;
    int16* crystalsFromDown;
    PrecalculatedMoves* precalculatedMoves;
    int8 width;
    int8 height;
    int score;
    int potentialScore;
    int hash;
    vector<Lantern> lanterns;
    vector<Obstacle> obstacles;
    vector<Mirror> mirrors;
    char* memoryBuffer;

    static int MemoryBufferSize(int8 width, int8 height)
    {
        return MemoryBufferSize(width * height);
    }
    static int MemoryBufferSize(int16 elements)
    {
        int size = sizeof(board[0]) * elements
            + sizeof(lightMap[0]) * elements
            + sizeof(crystalsLightMap[0]) * elements
            + sizeof(crystalsFromLeft[0]) * elements
            + sizeof(crystalsFromRight[0]) * elements
            + sizeof(crystalsFromUp[0]) * elements
            + sizeof(crystalsFromDown[0]) * elements
            + sizeof(precalculatedMoves[0]) * elements;
        return size;
    }

    static vector<char*>& GetMemoryBufferPool(int16 elements)
    {
        static vector<char*> memoryBufferPools[100 * 100];

        return memoryBufferPools[elements - 1];
    }

    static void ReturnMemoryBuffer(char* memoryBuffer, int16 elements)
    {
        GetMemoryBufferPool(elements).push_back(memoryBuffer);
    }

    static char* GetMemoryBuffer(int16 elements)
    {
        vector<char*>& memoryBufferPool = GetMemoryBufferPool(elements);
        char* memoryBuffer;

        if (memoryBufferPool.empty())
            memoryBuffer = new char[MemoryBufferSize(elements)];
        else
        {
            memoryBuffer = memoryBufferPool.back();
            memoryBufferPool.pop_back();
        }
        return memoryBuffer;
    }

    State()
        : memoryBuffer(nullptr)
    {
    }

    State(int8 width, int8 height)
        : width(width)
        , height(height)
        , score(0)
        , potentialScore(0)
        , hash(0)
    {
        CreateBuffers(true);
    }

    State(const State& state)
        : width(state.width)
        , height(state.height)
        , score(state.score)
        , potentialScore(state.potentialScore)
        , hash(state.hash)
        , lanterns(state.lanterns)
        , obstacles(state.obstacles)
        , mirrors(state.mirrors)
    {
        CreateBuffers(false);
        memcpy(memoryBuffer, state.memoryBuffer, MemoryBufferSize(width, height));
    }

    ~State()
    {
        if (memoryBuffer != nullptr)
            ReturnMemoryBuffer(memoryBuffer, width * height);
    }

    State& operator=(const State& state)
    {
        if (width != state.width || height != state.height)
            CreateBuffers(false);
        width = state.width;
        height = state.height;
        score = state.score;
        potentialScore = state.potentialScore;
        hash = state.hash;
        lanterns = state.lanterns;
        obstacles = state.obstacles;
        mirrors = state.mirrors;
        memcpy(memoryBuffer, state.memoryBuffer, MemoryBufferSize(width, height));
        return *this;
    }

    void CreateBuffers(bool initialize)
    {
        int16 elements = width * height;
        int offset = 0;

        boardSize = elements;
        memoryBuffer = GetMemoryBuffer(elements);

        board = (BoardField*)(memoryBuffer + offset);
        offset += sizeof(board[0]) * elements;

        lightMap = (Light*)(memoryBuffer + offset);
        offset += sizeof(lightMap[0]) * elements;

        crystalsLightMap = (Light*)(memoryBuffer + offset);
        offset += sizeof(crystalsLightMap[0]) * elements;

        crystalsFromLeft = (int16*)(memoryBuffer + offset);
        offset += sizeof(crystalsFromLeft[0]) * elements;

        crystalsFromRight = (int16*)(memoryBuffer + offset);
        offset += sizeof(crystalsFromRight[0]) * elements;

        crystalsFromUp = (int16*)(memoryBuffer + offset);
        offset += sizeof(crystalsFromUp[0]) * elements;

        crystalsFromDown = (int16*)(memoryBuffer + offset);
        offset += sizeof(crystalsFromDown[0]) * elements;

        precalculatedMoves = (PrecalculatedMoves*)(memoryBuffer + offset);
        offset += sizeof(precalculatedMoves[0]) * elements;

        if (initialize)
        {
            memset(board, (int)BoardField::Empty, sizeof(board[0]) * elements);
            memset(lightMap, (int)Light::Empty, sizeof(lightMap[0]) * elements);
            memset(crystalsLightMap, (int)Light::Empty, sizeof(crystalsLightMap[0]) * elements);
            memset(crystalsFromLeft, -1, sizeof(crystalsFromLeft[0]) * elements);
            memset(crystalsFromRight, -1, sizeof(crystalsFromRight[0]) * elements);
            memset(crystalsFromUp, -1, sizeof(crystalsFromUp[0]) * elements);
            memset(crystalsFromDown, -1, sizeof(crystalsFromDown[0]) * elements);
            memset(precalculatedMoves, -1, sizeof(precalculatedMoves[0]) * elements);
        }
    }

    State(State&& state)
        : board(std::move(state.board))
        , lightMap(std::move(state.lightMap))
        , crystalsLightMap(std::move(state.crystalsLightMap))
        , crystalsFromLeft(std::move(state.crystalsFromLeft))
        , crystalsFromRight(std::move(state.crystalsFromRight))
        , crystalsFromUp(std::move(state.crystalsFromUp))
        , crystalsFromDown(std::move(state.crystalsFromDown))
        , lanterns(std::move(state.lanterns))
        , obstacles(std::move(state.obstacles))
        , mirrors(std::move(state.mirrors))
        , precalculatedMoves(std::move(state.precalculatedMoves))
        , width(state.width)
        , height(state.height)
        , score(state.score)
        , potentialScore(state.potentialScore)
        , hash(state.hash)
        , memoryBuffer(state.memoryBuffer)
    {
        state.memoryBuffer = nullptr;
    }

    void UpdateFromBoard()
    {
        // Initialize crystals light map
        int16 mp = 0;
        for (int8 y = 0; y < height; y++)
            for (int8 x = 0; x < width; x++, mp++)
                if ((board[mp] & BoardField::Crystal) != BoardField::Empty)
                {
                    Color color = (Color)(board[mp] & BoardField::ColorMask);

                    AddColorDown(x, y + 1, mp + width, width, color, crystalsLightMap, board);
                    AddColorUp(x, y - 1, mp - width, width, color, crystalsLightMap, board);
                    AddColorLeft(x - 1, y, mp - 1, width, color, crystalsLightMap, board);
                    AddColorRight(x + 1, y, mp + 1, width, color, crystalsLightMap, board);
                    UpdateMapDown(x, y + 1, mp + width, width, mp, crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
                    UpdateMapUp(x, y - 1, mp - width, width, mp, crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
                    UpdateMapLeft(x - 1, y, mp - 1, width, mp, crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
                    UpdateMapRight(x + 1, y, mp + 1, width, mp, crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
                }
    }

    BoardField& Board(int y, int x)
    {
        return board[y * width + x];
    }

    static bool MoveComparison(const Move& m1, const Move& m2)
    {
#ifdef USE_POTENTIAL_SCORE
        return m2.potentialScore < m1.potentialScore;
#else
        return m2.score < m1.score;
#endif
    }

    void GetTopMoves(vector<Move>& moves, size_t maxMoves, int costLantern, int costMirror, int costObstacle, int maxMirrors, int maxObstacles)
    {
        UpdateMoves(costLantern, costMirror, costObstacle, maxMirrors, maxObstacles);

        // Take first maxMoves
        PrecalculatedMoves* preMoves = precalculatedMoves;
        int16 mp = 0, mpMax = width * height;
        bool obstaclesOk = (int)obstacles.size() < maxObstacles;
        bool mirrorsOk = (int)mirrors.size() < maxMirrors;

        moves.clear();
        moves.reserve(maxMoves * 2);
        for (; mp < mpMax && moves.size() < maxMoves; mp++, preMoves++)
            for (int8 i = 0; i < preMoves->movesCount; i++)
            {
                Move& move = preMoves->moves[i];

                if (move.type == MoveType::Obstacle && !obstaclesOk)
                    continue;
                if (move.type == MoveType::Mirror && !mirrorsOk)
                    continue;

                if (!moves.empty())
                {
                    auto it = lower_bound(moves.begin(), moves.end(), move, MoveComparison);

                    moves.insert(it, move);
                }
                else
                    moves.push_back(move);
            }
        if (moves.size() >= maxMoves)
        {
            moves.resize(maxMoves);

            // Keep only maxMoves in moves array
            Move& lastMove = moves[maxMoves - 1];
            for (; mp < mpMax; mp++, preMoves++)
                for (int8 i = 0; i < preMoves->movesCount; i++)
                {
                    Move& move = preMoves->moves[i];

                    if (move.type == MoveType::Obstacle && !obstaclesOk)
                        continue;
                    if (move.type == MoveType::Mirror && !mirrorsOk)
                        continue;

#ifdef USE_POTENTIAL_SCORE
                    if (lastMove.potentialScore >= move.potentialScore)
#else
                    if (lastMove.score >= move.score)
#endif
                        continue;

                    auto it = lower_bound(moves.begin(), moves.end(), move, MoveComparison);

                    moves.insert(it, move);
                    moves.resize(maxMoves);
                }
        }

        // Update outdated fields
        for (Move& move : moves)
        {
            move.state = this;
            switch (move.type)
            {
            case MoveType::Lantern:
                move.hash = hash ^ move.lantern.GetHash();
                break;
            case MoveType::Obstacle:
                move.hash = hash ^ move.obstacle.GetHash();
                break;
            case MoveType::Mirror:
                move.hash = hash ^ move.mirror.GetHash();
                break;
            }
        }
    }

    void UpdateMoves(int costLantern, int costMirror, int costObstacle, int maxMirrors, int maxObstacles)
    {
        int16 mp = 0;
        for (int8 y = 0; y < height; y++)
            for (int8 x = 0; x < width; x++, mp++)
            {
                PrecalculatedMoves& preMoves = precalculatedMoves[mp];

                if (preMoves.movesCount >= 0)
                    continue;

                preMoves.movesCount = 0;
                if ((board[mp] & BoardField::ObjectMask) == BoardField::Empty)
                {
                    if ((lightMap[mp] & Light::ColorMask) == Light::Empty)
                    {
                        // See if any crystal can be hit from this position in the map
                        Color color = (Color)(crystalsLightMap[mp] & Light::ColorMask);

                        if (color == Color::Empty)
                            continue;

                        Lantern lantern;
                        lantern.position.x = x;
                        lantern.position.y = y;

                        // Try putting lantern
                        if ((color & Color::Blue) != Color::Empty)
                        {
                            lantern.color = Color::Blue;
                            preMoves.moves[preMoves.movesCount++] = Move(this, lantern, costLantern);
                        }
                        if ((color & Color::Yellow) != Color::Empty)
                        {
                            lantern.color = Color::Yellow;
                            preMoves.moves[preMoves.movesCount++] = Move(this, lantern, costLantern);
                        }
                        if ((color & Color::Red) != Color::Empty)
                        {
                            lantern.color = Color::Red;
                            preMoves.moves[preMoves.movesCount++] = Move(this, lantern, costLantern);
                        }
                    }
                    else
                    {
                        // Try to put Obstacle
                        if ((int)obstacles.size() < maxObstacles)
                        {
                            Obstacle obstacle;
                            obstacle.position.x = x;
                            obstacle.position.y = y;
                            preMoves.moves[preMoves.movesCount++] = Move(this, obstacle, costObstacle);
                        }

                        // Try to put slash Mirror '/'
                        if ((int)mirrors.size() >= maxMirrors)
                            continue;
                        Mirror mirror;
                        mirror.position.x = x;
                        mirror.position.y = y;
                        mirror.slash = true;

                        if (IsPuttingMirrorSafe(mirror))
                            preMoves.moves[preMoves.movesCount++] = Move(this, mirror, costMirror);

                        // Try to put backslash Mirror '\'
                        mirror.slash = false;
                        if (IsPuttingMirrorSafe(mirror))
                            preMoves.moves[preMoves.movesCount++] = Move(this, mirror, costMirror);
                    }
                }
            }
    }

    void PutLantern(Lantern lantern, int cost)
    {
        int16 mp = lantern.position.y * width + lantern.position.x;
        UpdateScore(AddColorLeft(lantern.position.x - 1, lantern.position.y, mp - 1, width, lantern.color, lightMap, board));
        UpdateScore(AddColorRight(lantern.position.x + 1, lantern.position.y, mp + 1, width, lantern.color, lightMap, board));
        UpdateScore(AddColorUp(lantern.position.x, lantern.position.y - 1, mp - width, width, lantern.color, lightMap, board));
        UpdateScore(AddColorDown(lantern.position.x, lantern.position.y + 1, mp + width, width, lantern.color, lightMap, board));

        // Update crystalsLightMap
        ClearColorLeft(lantern.position.x - 1, lantern.position.y, mp - 1, width, crystalsLightMap, board);
        ClearColorRight(lantern.position.x + 1, lantern.position.y, mp + 1, width, crystalsLightMap, board);
        ClearColorUp(lantern.position.x, lantern.position.y - 1, mp - width, width, crystalsLightMap, board);
        ClearColorDown(lantern.position.x, lantern.position.y + 1, mp + width, width, crystalsLightMap, board);

        // Update crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown
        UpdateMapLeft(lantern.position.x - 1, lantern.position.y, mp - 1, width, -1, crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
        UpdateMapRight(lantern.position.x + 1, lantern.position.y, mp + 1, width, -1, crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
        UpdateMapUp(lantern.position.x, lantern.position.y - 1, mp - width, width, -1, crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
        UpdateMapDown(lantern.position.x, lantern.position.y + 1, mp + width, width, -1, crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);

        // Mark as invalid precalculated moves
        precalculatedMoves[mp].movesCount = -1;
        InvalidatePreMovesLeft(lantern.position.x - 1, lantern.position.y, mp - 1, width, precalculatedMoves, board);
        InvalidatePreMovesRight(lantern.position.x + 1, lantern.position.y, mp + 1, width, precalculatedMoves, board);
        InvalidatePreMovesUp(lantern.position.x, lantern.position.y - 1, mp - width, width, precalculatedMoves, board);
        InvalidatePreMovesDown(lantern.position.x, lantern.position.y + 1, mp + width, width, precalculatedMoves, board);

        // Update rest of the fields
        score -= cost;
        potentialScore -= cost;
        board[mp] |= BoardField::Lantern | (BoardField)(lantern.color);
        lanterns.push_back(lantern);
        hash = hash ^ lantern.GetHash();
    }

    void PutObstacle(Obstacle obstacle, int cost)
    {
        int16 mp = obstacle.position.y * width + obstacle.position.x;

        ClearColor(obstacle.position);
        score -= cost;
        potentialScore -= cost;
        obstacles.push_back(obstacle);
        board[mp] |= BoardField::Obstacle;
        hash = hash ^ obstacle.GetHash();
    }

    void PutMirror(Mirror mirror, int cost)
    {
        int16 mp = mirror.position.y * width + mirror.position.x;
        Light light = lightMap[mp];
        Light crystalsLight = crystalsLightMap[mp];

        ClearColor(mirror.position);
        if (mirror.slash)
        {
            if ((light & Light::LeftMask) != Light::Empty)
                UpdateScore(AddColorDown(mirror.position.x, mirror.position.y + 1, mp + width, width, GetLeftColor(light), lightMap, board));
            if ((light & Light::RightMask) != Light::Empty)
                UpdateScore(AddColorUp(mirror.position.x, mirror.position.y - 1, mp - width, width, GetLeftColor(light), lightMap, board));
            if ((light & Light::DownMask) != Light::Empty)
                UpdateScore(AddColorLeft(mirror.position.x - 1, mirror.position.y, mp - 1, width, GetLeftColor(light), lightMap, board));
            if ((light & Light::UpMask) != Light::Empty)
                UpdateScore(AddColorRight(mirror.position.x + 1, mirror.position.y, mp + 1, width, GetLeftColor(light), lightMap, board));
            if ((crystalsLight & Light::LeftMask) != Light::Empty)
                AddColorDown(mirror.position.x, mirror.position.y + 1, mp + width, width, GetLeftColor(crystalsLight), crystalsLightMap, board);
            if ((crystalsLight & Light::RightMask) != Light::Empty)
                AddColorUp(mirror.position.x, mirror.position.y - 1, mp - width, width, GetLeftColor(crystalsLight), crystalsLightMap, board);
            if ((crystalsLight & Light::DownMask) != Light::Empty)
                AddColorLeft(mirror.position.x - 1, mirror.position.y, mp - 1, width, GetLeftColor(crystalsLight), crystalsLightMap, board);
            if ((crystalsLight & Light::UpMask) != Light::Empty)
                AddColorRight(mirror.position.x + 1, mirror.position.y, mp + 1, width, GetLeftColor(crystalsLight), crystalsLightMap, board);
            if (crystalsFromLeft[mp] >= 0)
                UpdateMapUp(mirror.position.x, mirror.position.y - 1, mp - width, width, crystalsFromLeft[mp], crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
            if (crystalsFromRight[mp] >= 0)
                UpdateMapDown(mirror.position.x, mirror.position.y + 1, mp + width, width, crystalsFromRight[mp], crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
            if (crystalsFromUp[mp] >= 0)
                UpdateMapLeft(mirror.position.x - 1, mirror.position.y, mp - 1, width, crystalsFromUp[mp], crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
            if (crystalsFromDown[mp] >= 0)
                UpdateMapRight(mirror.position.x + 1, mirror.position.y, mp + 1, width, crystalsFromDown[mp], crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
        }
        else
        {
            if ((light & Light::LeftMask) != Light::Empty)
                UpdateScore(AddColorUp(mirror.position.x, mirror.position.y - 1, mp - width, width, GetLeftColor(light), lightMap, board));
            if ((light & Light::RightMask) != Light::Empty)
                UpdateScore(AddColorDown(mirror.position.x, mirror.position.y + 1, mp + width, width, GetLeftColor(light), lightMap, board));
            if ((light & Light::DownMask) != Light::Empty)
                UpdateScore(AddColorRight(mirror.position.x + 1, mirror.position.y, mp + 1, width, GetLeftColor(light), lightMap, board));
            if ((light & Light::UpMask) != Light::Empty)
                UpdateScore(AddColorLeft(mirror.position.x - 1, mirror.position.y, mp - 1, width, GetLeftColor(light), lightMap, board));
            if ((crystalsLight & Light::LeftMask) != Light::Empty)
                AddColorUp(mirror.position.x, mirror.position.y - 1, mp - width, width, GetLeftColor(crystalsLight), crystalsLightMap, board);
            if ((crystalsLight & Light::RightMask) != Light::Empty)
                AddColorDown(mirror.position.x, mirror.position.y + 1, mp + width, width, GetLeftColor(crystalsLight), crystalsLightMap, board);
            if ((crystalsLight & Light::DownMask) != Light::Empty)
                AddColorRight(mirror.position.x + 1, mirror.position.y, mp + 1, width, GetLeftColor(crystalsLight), crystalsLightMap, board);
            if ((crystalsLight & Light::UpMask) != Light::Empty)
                AddColorLeft(mirror.position.x - 1, mirror.position.y, mp - 1, width, GetLeftColor(crystalsLight), crystalsLightMap, board);
            if (crystalsFromLeft[mp] >= 0)
                UpdateMapDown(mirror.position.x, mirror.position.y + 1, mp + width, width, crystalsFromLeft[mp], crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
            if (crystalsFromRight[mp] >= 0)
                UpdateMapUp(mirror.position.x, mirror.position.y - 1, mp - width, width, crystalsFromRight[mp], crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
            if (crystalsFromUp[mp] >= 0)
                UpdateMapRight(mirror.position.x + 1, mirror.position.y, mp + 1, width, crystalsFromUp[mp], crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
            if (crystalsFromDown[mp] >= 0)
                UpdateMapLeft(mirror.position.x - 1, mirror.position.y, mp - 1, width, crystalsFromDown[mp], crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
        }

        score -= cost;
        potentialScore -= cost;
        mirrors.push_back(mirror);
        board[mp] |= mirror.slash ? BoardField::MirrorSlash : BoardField::MirrorBackSlash;
        hash = hash ^ mirror.GetHash();
    }

    void GetLanternScore(Lantern lantern, int cost, int& score, int& potentialScore)
    {
        score = -cost;
        potentialScore = -cost;

        int16 lmp = lantern.position.y * width + lantern.position.x;
        Light crystalLights = crystalsLightMap[lmp];

        if ((crystalLights & Light::LeftMask) != Light::Empty)
        {
            int16 mp = crystalsFromRight[lmp];
            if (mp >= 0)
            {
                Color previousColor = (Color)(lightMap[mp] & Light::ColorMask);
                Color crystalColor = (Color)(board[mp] & BoardField::ColorMask);
                Color newColor = previousColor | lantern.color;

                if (previousColor != newColor)
                {
                    score += GetCrystalScoreDiff(previousColor, crystalColor, newColor);
                    potentialScore += GetCrystalPotentialScoreDiff(previousColor, crystalColor, newColor);
                }
            }
        }
        if ((crystalLights & Light::RightMask) != Light::Empty)
        {
            int16 mp = crystalsFromLeft[lmp];
            if (mp >= 0)
            {
                Color previousColor = (Color)(lightMap[mp] & Light::ColorMask);
                Color crystalColor = (Color)(board[mp] & BoardField::ColorMask);
                Color newColor = previousColor | lantern.color;

                if (previousColor != newColor)
                {
                    score += GetCrystalScoreDiff(previousColor, crystalColor, newColor);
                    potentialScore += GetCrystalPotentialScoreDiff(previousColor, crystalColor, newColor);
                }
            }
        }
        if ((crystalLights & Light::DownMask) != Light::Empty)
        {
            int16 mp = crystalsFromUp[lmp];
            if (mp >= 0)
            {
                Color previousColor = (Color)(lightMap[mp] & Light::ColorMask);
                Color crystalColor = (Color)(board[mp] & BoardField::ColorMask);
                Color newColor = previousColor | lantern.color;

                if (previousColor != newColor)
                {
                    score += GetCrystalScoreDiff(previousColor, crystalColor, newColor);
                    potentialScore += GetCrystalPotentialScoreDiff(previousColor, crystalColor, newColor);
                }
            }
        }
        if ((crystalLights & Light::UpMask) != Light::Empty)
        {
            int16 mp = crystalsFromDown[lmp];
            if (mp >= 0)
            {
                Color previousColor = (Color)(lightMap[mp] & Light::ColorMask);
                Color crystalColor = (Color)(board[mp] & BoardField::ColorMask);
                Color newColor = previousColor | lantern.color;

                if (previousColor != newColor)
                {
                    score += GetCrystalScoreDiff(previousColor, crystalColor, newColor);
                    potentialScore += GetCrystalPotentialScoreDiff(previousColor, crystalColor, newColor);
                }
            }
        }
    }

    void GetObstacleScore(Obstacle obstacle, int cost, int& score, int& potentialScore)
    {
        score = -cost;
        potentialScore = -cost;

        int16 lmp = obstacle.position.y * width + obstacle.position.x;
        Light lights = lightMap[lmp];

        if ((lights & Light::LeftMask) != Light::Empty)
        {
            int16 mp = crystalsFromLeft[lmp];
            if (mp >= 0)
            {
                Light previousLight = lightMap[mp];
                Light newLight = previousLight & ~(lights & Light::LeftMask);
                Color previousColor = (Color)(previousLight & Light::ColorMask);
                Color crystalColor = (Color)(board[mp] & BoardField::ColorMask);
                Color newColor = ((newLight & Light::BlueDirectionMask) != Light::Empty ? Color::Blue : Color::Empty)
                    | ((newLight & Light::YellowDirectionMask) != Light::Empty ? Color::Yellow : Color::Empty)
                    | ((newLight & Light::RedDirectionMask) != Light::Empty ? Color::Red : Color::Empty);

                if (previousColor != newColor)
                {
                    score += GetCrystalScoreDiff(previousColor, crystalColor, newColor);
                    potentialScore += GetCrystalPotentialScoreDiff(previousColor, crystalColor, newColor);
                }
            }
        }
        if ((lights & Light::RightMask) != Light::Empty)
        {
            int16 mp = crystalsFromRight[lmp];
            if (mp >= 0)
            {
                Light previousLight = lightMap[mp];
                Light newLight = previousLight & ~(lights & Light::RightMask);
                Color previousColor = (Color)(previousLight & Light::ColorMask);
                Color crystalColor = (Color)(board[mp] & BoardField::ColorMask);
                Color newColor = ((newLight & Light::BlueDirectionMask) != Light::Empty ? Color::Blue : Color::Empty)
                    | ((newLight & Light::YellowDirectionMask) != Light::Empty ? Color::Yellow : Color::Empty)
                    | ((newLight & Light::RedDirectionMask) != Light::Empty ? Color::Red : Color::Empty);

                if (previousColor != newColor)
                {
                    score += GetCrystalScoreDiff(previousColor, crystalColor, newColor);
                    potentialScore += GetCrystalPotentialScoreDiff(previousColor, crystalColor, newColor);
                }
            }
        }
        if ((lights & Light::UpMask) != Light::Empty)
        {
            int16 mp = crystalsFromUp[lmp];
            if (mp >= 0)
            {
                Light previousLight = lightMap[mp];
                Light newLight = previousLight & ~(lights & Light::UpMask);
                Color previousColor = (Color)(previousLight & Light::ColorMask);
                Color crystalColor = (Color)(board[mp] & BoardField::ColorMask);
                Color newColor = ((newLight & Light::BlueDirectionMask) != Light::Empty ? Color::Blue : Color::Empty)
                    | ((newLight & Light::YellowDirectionMask) != Light::Empty ? Color::Yellow : Color::Empty)
                    | ((newLight & Light::RedDirectionMask) != Light::Empty ? Color::Red : Color::Empty);

                if (previousColor != newColor)
                {
                    score += GetCrystalScoreDiff(previousColor, crystalColor, newColor);
                    potentialScore += GetCrystalPotentialScoreDiff(previousColor, crystalColor, newColor);
                }
            }
        }
        if ((lights & Light::DownMask) != Light::Empty)
        {
            int16 mp = crystalsFromDown[lmp];
            if (mp >= 0)
            {
                Light previousLight = lightMap[mp];
                Light newLight = previousLight & ~(lights & Light::DownMask);
                Color previousColor = (Color)(previousLight & Light::ColorMask);
                Color crystalColor = (Color)(board[mp] & BoardField::ColorMask);
                Color newColor = ((newLight & Light::BlueDirectionMask) != Light::Empty ? Color::Blue : Color::Empty)
                    | ((newLight & Light::YellowDirectionMask) != Light::Empty ? Color::Yellow : Color::Empty)
                    | ((newLight & Light::RedDirectionMask) != Light::Empty ? Color::Red : Color::Empty);

                if (previousColor != newColor)
                {
                    score += GetCrystalScoreDiff(previousColor, crystalColor, newColor);
                    potentialScore += GetCrystalPotentialScoreDiff(previousColor, crystalColor, newColor);
                }
            }
        }
    }

    void GetMirrorScore(Mirror mirror, int cost, int& score, int& potentialScore)
    {
        score = -cost;
        potentialScore = -cost;

        // Slash: Left -> Down, Right -> Up, Down -> Left, Up -> Right
        // BackSlash: Left -> Up, Right -> Down, Up -> Left, Down -> Right
        int16 lmp = mirror.position.y * width + mirror.position.x;
        Light lights = lightMap[lmp];
        Light crystalLights = crystalsLightMap[lmp];

        if ((crystalLights & Light::LeftMask) != Light::Empty)
        {
            int16 mp = crystalsFromRight[lmp];
            if (mp >= 0)
            {
                Light previousLight = lightMap[mp];
                Color previousColor = (Color)(previousLight & Light::ColorMask);
                Color crystalColor = (Color)(board[mp] & BoardField::ColorMask);
                Color newColor = previousColor;

                // Block light
                if ((lights & Light::RightMask) != Light::Empty)
                {
                    Light newLight = previousLight & ~(lights & Light::RightMask);

                    newColor = ((newLight & Light::BlueDirectionMask) != Light::Empty ? Color::Blue : Color::Empty)
                        | ((newLight & Light::YellowDirectionMask) != Light::Empty ? Color::Yellow : Color::Empty)
                        | ((newLight & Light::RedDirectionMask) != Light::Empty ? Color::Red : Color::Empty);
                }

                // Add light
                newColor |= mirror.slash ? GetUpColor(lights) : GetDownColor(lights);

                if (previousColor != newColor)
                {
                    score += GetCrystalScoreDiff(previousColor, crystalColor, newColor);
                    potentialScore += GetCrystalPotentialScoreDiff(previousColor, crystalColor, newColor);
                }
            }
        }
        if ((crystalLights & Light::RightMask) != Light::Empty)
        {
            int16 mp = crystalsFromLeft[lmp];
            if (mp >= 0)
            {
                Light previousLight = lightMap[mp];
                Color previousColor = (Color)(previousLight & Light::ColorMask);
                Color crystalColor = (Color)(board[mp] & BoardField::ColorMask);
                Color newColor = previousColor;

                // Block light
                if ((lights & Light::LeftMask) != Light::Empty)
                {
                    Light newLight = previousLight & ~(lights & Light::LeftMask);

                    newColor = ((newLight & Light::BlueDirectionMask) != Light::Empty ? Color::Blue : Color::Empty)
                        | ((newLight & Light::YellowDirectionMask) != Light::Empty ? Color::Yellow : Color::Empty)
                        | ((newLight & Light::RedDirectionMask) != Light::Empty ? Color::Red : Color::Empty);
                }

                // Add light
                newColor |= mirror.slash ? GetDownColor(lights) : GetUpColor(lights);

                if (previousColor != newColor)
                {
                    score += GetCrystalScoreDiff(previousColor, crystalColor, newColor);
                    potentialScore += GetCrystalPotentialScoreDiff(previousColor, crystalColor, newColor);
                }
            }
        }
        if ((crystalLights & Light::UpMask) != Light::Empty)
        {
            int16 mp = crystalsFromDown[lmp];
            if (mp >= 0)
            {
                Light previousLight = lightMap[mp];
                Color previousColor = (Color)(previousLight & Light::ColorMask);
                Color crystalColor = (Color)(board[mp] & BoardField::ColorMask);
                Color newColor = previousColor;

                // Block light
                if ((lights & Light::DownMask) != Light::Empty)
                {
                    Light newLight = previousLight & ~(lights & Light::DownMask);

                    newColor = ((newLight & Light::BlueDirectionMask) != Light::Empty ? Color::Blue : Color::Empty)
                        | ((newLight & Light::YellowDirectionMask) != Light::Empty ? Color::Yellow : Color::Empty)
                        | ((newLight & Light::RedDirectionMask) != Light::Empty ? Color::Red : Color::Empty);
                }

                // Add light
                newColor |= mirror.slash ? GetLeftColor(lights) : GetRightColor(lights);

                if (previousColor != newColor)
                {
                    score += GetCrystalScoreDiff(previousColor, crystalColor, newColor);
                    potentialScore += GetCrystalPotentialScoreDiff(previousColor, crystalColor, newColor);
                }
            }
        }
        if ((crystalLights & Light::DownMask) != Light::Empty)
        {
            int16 mp = crystalsFromUp[lmp];
            if (mp >= 0)
            {
                Light previousLight = lightMap[mp];
                Color previousColor = (Color)(previousLight & Light::ColorMask);
                Color crystalColor = (Color)(board[mp] & BoardField::ColorMask);
                Color newColor = previousColor;

                // Block light
                if ((lights & Light::UpMask) != Light::Empty)
                {
                    Light newLight = previousLight & ~(lights & Light::UpMask);

                    newColor = ((newLight & Light::BlueDirectionMask) != Light::Empty ? Color::Blue : Color::Empty)
                        | ((newLight & Light::YellowDirectionMask) != Light::Empty ? Color::Yellow : Color::Empty)
                        | ((newLight & Light::RedDirectionMask) != Light::Empty ? Color::Red : Color::Empty);
                }

                // Add light
                newColor |= mirror.slash ? GetRightColor(lights) : GetLeftColor(lights);

                if (previousColor != newColor)
                {
                    score += GetCrystalScoreDiff(previousColor, crystalColor, newColor);
                    potentialScore += GetCrystalPotentialScoreDiff(previousColor, crystalColor, newColor);
                }
            }
        }
    }

    bool IsPuttingMirrorSafe(Mirror mirror)
    {
        int16 mp = mirror.position.y * width + mirror.position.x;
        Light light = lightMap[mp];

        // Check if it will succeed
        Light up = light & Light::UpMask;
        Light down = light & Light::DownMask;
        Light left = light & Light::LeftMask;
        Light right = light & Light::RightMask;

        if (mirror.slash)
        {
            if (left != Light::Empty && up != Light::Empty)
                return false;
            if (right != Light::Empty && down != Light::Empty)
                return false;
        }
        else
        {
            if (left != Light::Empty && down != Light::Empty)
                return false;
            if (right != Light::Empty && up != Light::Empty)
                return false;
        }
        return true;
    }

    static int GetCrystalScoreDiff(Color previousColor, Color crystalColor, Color newColor)
    {
        int previousScore = GetCrystalScore(crystalColor, previousColor);
        int newScore = GetCrystalScore(crystalColor, newColor);

        return newScore - previousScore;
    }

    static int GetCrystalPotentialScoreDiff(Color previousColor, Color crystalColor, Color newColor)
    {
        int previousScore = GetCrystalPotentialScore(crystalColor, previousColor);
        int newScore = GetCrystalPotentialScore(crystalColor, newColor);

        return newScore - previousScore;
    }

    static int GetCrystalScore(Color crystalColor, Color lightColor)
    {
        if (lightColor == Color::Empty)
            return 0;
        if (crystalColor == lightColor)
        {
            if (crystalColor == Color::Blue || crystalColor == Color::Red || crystalColor == Color::Yellow)
                return 20;
            return 30;
        }
        return -10;
    }

    static int GetCrystalPotentialScore(Color crystalColor, Color lightColor)
    {
        if (lightColor == Color::Empty)
            return 0;

        if ((crystalColor & lightColor) != lightColor)
            return -10;

        if (crystalColor == lightColor)
        {
            if (crystalColor == Color::Blue || crystalColor == Color::Red || crystalColor == Color::Yellow)
                return 20;
            return 30;
        }
        return 5;
    }

private:
    void ClearColor(Position position)
    {
        int16 mp = position.y * width + position.x;

        UpdateScore(ClearColorLeft(position.x - 1, position.y, mp - 1, width, lightMap, board));
        UpdateScore(ClearColorRight(position.x + 1, position.y, mp + 1, width, lightMap, board));
        UpdateScore(ClearColorUp(position.x, position.y - 1, mp - width, width, lightMap, board));
        UpdateScore(ClearColorDown(position.x, position.y + 1, mp + width, width, lightMap, board));

        // Update crystalsLightMap
        ClearColorLeft(position.x - 1, position.y, mp - 1, width, crystalsLightMap, board);
        ClearColorRight(position.x + 1, position.y, mp + 1, width, crystalsLightMap, board);
        ClearColorUp(position.x, position.y - 1, mp - width, width, crystalsLightMap, board);
        ClearColorDown(position.x, position.y + 1, mp + width, width, crystalsLightMap, board);

        // Update crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown
        UpdateMapLeft(position.x - 1, position.y, mp - 1, width, -1, crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
        UpdateMapRight(position.x + 1, position.y, mp + 1, width, -1, crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
        UpdateMapUp(position.x, position.y - 1, mp - width, width, -1, crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);
        UpdateMapDown(position.x, position.y + 1, mp + width, width, -1, crystalsFromLeft, crystalsFromRight, crystalsFromUp, crystalsFromDown, board);

        // Mark as invalid precalculated moves
        precalculatedMoves[mp].movesCount = -1;
        InvalidatePreMovesLeft(position.x - 1, position.y, mp - 1, width, precalculatedMoves, board);
        InvalidatePreMovesRight(position.x + 1, position.y, mp + 1, width, precalculatedMoves, board);
        InvalidatePreMovesUp(position.x, position.y - 1, mp - width, width, precalculatedMoves, board);
        InvalidatePreMovesDown(position.x, position.y + 1, mp + width, width, precalculatedMoves, board);
    }

    struct Hit
    {
        int8 x;              // x position
        int8 y;              // y position
        int16 mp;            // Map position (for direct access to the map)
        Color previousColor; // Previous light color comming from lanterns
        Color crystalColor;  // Color of the crystal
        Color newColor;      // New light color comming from lanterns

        Hit(int8 x, int8 y, int16 mp)
            : x(x)
            , y(y)
            , mp(mp)
            , crystalColor(Color::Empty)
        {
        }

        Hit(int8 x, int8 y, int16 mp, Color crystalColor, Color previousColor, Color newColor)
            : x(x)
            , y(y)
            , mp(mp)
            , crystalColor(crystalColor)
            , previousColor(previousColor)
            , newColor(newColor)
        {
        }

        int GetScore() const
        {
            if (crystalColor == Color::Empty)
                return 0;

            int previousScore = GetCrystalScore(crystalColor, previousColor);
            int newScore = GetCrystalScore(crystalColor, newColor);

            return newScore - previousScore;
        }

        int GetPotentialScore() const
        {
            if (crystalColor == Color::Empty)
                return 0;

            int previousScore = GetCrystalPotentialScore(crystalColor, previousColor);
            int newScore = GetCrystalPotentialScore(crystalColor, newColor);

            return newScore - previousScore;
        }
    };

    void UpdateScore(Hit hit)
    {
        score += hit.GetScore();
        potentialScore += hit.GetPotentialScore();
    }

    static int16 boardSize;

    static void UpdateMapLeft(int8 x, int8 y, int16 mp, int8 stride, int16 value, int16* leftMps, int16* rightMps, int16* upMps, int16* downMps, BoardField* board)
    {
        while (x >= 0)
        {
            rightMps[mp] = value;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return UpdateMapDown(x, y + 1, mp + stride, stride, value, leftMps, rightMps, upMps, downMps, board);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return UpdateMapUp(x, y - 1, mp - stride, stride, value, leftMps, rightMps, upMps, downMps, board);

            // Stop if we hit an object
            if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            x--;
            mp--;
        }
    }

    static void UpdateMapRight(int8 x, int8 y, int16 mp, int8 stride, int16 value, int16* leftMps, int16* rightMps, int16* upMps, int16* downMps, BoardField* board)
    {
        while (x < stride)
        {
            leftMps[mp] = value;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return UpdateMapUp(x, y - 1, mp - stride, stride, value, leftMps, rightMps, upMps, downMps, board);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return UpdateMapDown(x, y + 1, mp + stride, stride, value, leftMps, rightMps, upMps, downMps, board);

            // Stop if we hit an object
            if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            x++;
            mp++;
        }
    }

    static void UpdateMapUp(int8 x, int8 y, int16 mp, int8 stride, int16 value, int16* leftMps, int16* rightMps, int16* upMps, int16* downMps, BoardField* board)
    {
        while (y >= 0)
        {
            downMps[mp] = value;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return UpdateMapRight(x + 1, y, mp + 1, stride, value, leftMps, rightMps, upMps, downMps, board);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return UpdateMapLeft(x - 1, y, mp - 1, stride, value, leftMps, rightMps, upMps, downMps, board);

            // Stop if we hit an object
            if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            y--;
            mp -= stride;
        }
    }

    static void UpdateMapDown(int8 x, int8 y, int16 mp, int8 stride, int16 value, int16* leftMps, int16* rightMps, int16* upMps, int16* downMps, BoardField* board)
    {
        int16 mpMax = boardSize;

        while (mp < mpMax)
        {
            upMps[mp] = value;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return UpdateMapLeft(x - 1, y, mp, stride, value, leftMps, rightMps, upMps, downMps, board);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return UpdateMapRight(x + 1, y, mp, stride, value, leftMps, rightMps, upMps, downMps, board);

            // Stop if we hit an object
            if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            y++;
            mp += stride;
        }
    }

    static Hit AddColorLeft(int8 x, int8 y, int16 mp, int8 stride, Color color, Light* lightMap, BoardField* board)
    {
        Light direction = Light::Empty;

        if ((color & Color::Blue) == Color::Blue)
            direction |= Light::BlueLeft;
        if ((color & Color::Red) == Color::Red)
            direction |= Light::RedLeft;
        if ((color & Color::Yellow) == Color::Yellow)
            direction |= Light::YellowLeft;
        while (x >= 0)
        {
            Light originalLight = lightMap[mp];
            Light newLight = originalLight | (Light)color | direction;

            lightMap[mp] = newLight;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return AddColorDown(x, y + 1, mp + stride, stride, color, lightMap, board);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return AddColorUp(x, y - 1, mp - stride, stride, color, lightMap, board);

            // If we hit crystal, update score
            if ((field & BoardField::Crystal) != BoardField::Empty)
                return Hit(x, y, mp, (Color)(field & BoardField::ColorMask), (Color)(originalLight & Light::ColorMask), (Color)(newLight & Light::ColorMask));

            // Stop if we hit an object
            if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            x--;
            mp--;
        }
        return Hit(x, y, mp);
    }

    static Hit AddColorRight(int8 x, int8 y, int16 mp, int8 stride, Color color, Light* lightMap, BoardField* board)
    {
        Light direction = Light::Empty;

        if ((color & Color::Blue) == Color::Blue)
            direction |= Light::BlueRight;
        if ((color & Color::Red) == Color::Red)
            direction |= Light::RedRight;
        if ((color & Color::Yellow) == Color::Yellow)
            direction |= Light::YellowRight;
        while (x < stride)
        {
            Light originalLight = lightMap[mp];
            Light newLight = originalLight | (Light)color | direction;

            lightMap[mp] = newLight;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return AddColorUp(x, y - 1, mp - stride, stride, color, lightMap, board);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return AddColorDown(x, y + 1, mp + stride, stride, color, lightMap, board);

            // If we hit crystal, update score
            if ((field & BoardField::Crystal) != BoardField::Empty)
                return Hit(x, y, mp, (Color)(field & BoardField::ColorMask), (Color)(originalLight & Light::ColorMask), (Color)(newLight & Light::ColorMask));

            // Stop if we hit an object
            if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            x++;
            mp++;
        }
        return Hit(x, y, mp);
    }

    static Hit AddColorUp(int8 x, int8 y, int16 mp, int8 stride, Color color, Light* lightMap, BoardField* board)
    {
        Light direction = Light::Empty;

        if ((color & Color::Blue) == Color::Blue)
            direction |= Light::BlueUp;
        if ((color & Color::Red) == Color::Red)
            direction |= Light::RedUp;
        if ((color & Color::Yellow) == Color::Yellow)
            direction |= Light::YellowUp;
        while (y >= 0)
        {
            Light originalLight = lightMap[mp];
            Light newLight = originalLight | (Light)color | direction;

            lightMap[mp] = newLight;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return AddColorRight(x + 1, y, mp + 1, stride, color, lightMap, board);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return AddColorLeft(x - 1, y, mp - 1, stride, color, lightMap, board);

            // If we hit crystal, update score
            if ((field & BoardField::Crystal) != BoardField::Empty)
                return Hit(x, y, mp, (Color)(field & BoardField::ColorMask), (Color)(originalLight & Light::ColorMask), (Color)(newLight & Light::ColorMask));

            // Stop if we hit an object
            if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            y--;
            mp -= stride;
        }
        return Hit(x, y, mp);
    }

    static Hit AddColorDown(int8 x, int8 y, int16 mp, int8 stride, Color color, Light* lightMap, BoardField* board)
    {
        int16 mpMax = boardSize;
        Light direction = Light::Empty;

        if ((color & Color::Blue) == Color::Blue)
            direction |= Light::BlueDown;
        if ((color & Color::Red) == Color::Red)
            direction |= Light::RedDown;
        if ((color & Color::Yellow) == Color::Yellow)
            direction |= Light::YellowDown;
        while (mp < mpMax)
        {
            Light originalLight = lightMap[mp];
            Light newLight = originalLight | (Light)color | direction;

            lightMap[mp] = newLight;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return AddColorLeft(x - 1, y, mp - 1, stride, color, lightMap, board);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return AddColorRight(x + 1, y, mp + 1, stride, color, lightMap, board);

            // If we hit crystal, update score
            if ((field & BoardField::Crystal) != BoardField::Empty)
                return Hit(x, y, mp, (Color)(field & BoardField::ColorMask), (Color)(originalLight & Light::ColorMask), (Color)(newLight & Light::ColorMask));

            // Stop if we hit an object
            if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            y++;
            mp += stride;
        }
        return Hit(x, y, mp);
    }

    static Hit ClearColorLeft(int8 x, int8 y, int16 mp, int8 stride, Light* lightMap, BoardField* board)
    {
        while (x >= 0)
        {
            Light originalLight = lightMap[mp];
            Light light = originalLight;

            // Erase all light that comes from left
            light = light & ~Light::LeftMask;

            // Clear colors
            if ((light & Light::Blue) != Light::Empty && (light & Light::BlueDirectionMask) == Light::Empty)
                light = light & ~Light::Blue;
            if ((light & Light::Yellow) != Light::Empty && (light & Light::YellowDirectionMask) == Light::Empty)
                light = light & ~Light::Yellow;
            if ((light & Light::Red) != Light::Empty && (light & Light::RedDirectionMask) == Light::Empty)
                light = light & ~Light::Red;

            // Update field information
            lightMap[mp] = light;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return ClearColorDown(x, y + 1, mp + stride, stride, lightMap, board);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return ClearColorUp(x, y - 1, mp - stride, stride, lightMap, board);

            // If we hit crystal, update score
            if ((field & BoardField::Crystal) != BoardField::Empty)
                return Hit(x, y, mp, (Color)(field & BoardField::ColorMask), (Color)(originalLight & Light::ColorMask), (Color)(light & Light::ColorMask));

            // Stop if we hit an object
            if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            x--;
            mp--;
        }
        return Hit(x, y, mp);
    }

    static Hit ClearColorRight(int8 x, int8 y, int16 mp, int8 stride, Light* lightMap, BoardField* board)
    {
        while (x < stride)
        {
            Light originalLight = lightMap[mp];
            Light light = originalLight;

            // Erase all light that comes from left
            light = light & ~Light::RightMask;

            // Clear colors
            if ((light & Light::Blue) != Light::Empty && (light & Light::BlueDirectionMask) == Light::Empty)
                light = light & ~Light::Blue;
            if ((light & Light::Yellow) != Light::Empty && (light & Light::YellowDirectionMask) == Light::Empty)
                light = light & ~Light::Yellow;
            if ((light & Light::Red) != Light::Empty && (light & Light::RedDirectionMask) == Light::Empty)
                light = light & ~Light::Red;

            // Update field information
            lightMap[mp] = light;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return ClearColorUp(x, y - 1, mp - stride, stride, lightMap, board);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return ClearColorDown(x, y + 1, mp + stride, stride, lightMap, board);

            // If we hit crystal, update score
            if ((field & BoardField::Crystal) != BoardField::Empty)
                return Hit(x, y, mp, (Color)(field & BoardField::ColorMask), (Color)(originalLight & Light::ColorMask), (Color)(light & Light::ColorMask));

            // Stop if we hit an object
            if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            x++;
            mp++;
        }
        return Hit(x, y, mp);
    }

    static Hit ClearColorUp(int8 x, int8 y, int16 mp, int8 stride, Light* lightMap, BoardField* board)
    {
        while (y >= 0)
        {
            Light originalLight = lightMap[mp];
            Light light = originalLight;

            // Erase all light that comes from up
            light = light & ~Light::UpMask;

            // Clear colors
            if ((light & Light::Blue) != Light::Empty && (light & Light::BlueDirectionMask) == Light::Empty)
                light = light & ~Light::Blue;
            if ((light & Light::Yellow) != Light::Empty && (light & Light::YellowDirectionMask) == Light::Empty)
                light = light & ~Light::Yellow;
            if ((light & Light::Red) != Light::Empty && (light & Light::RedDirectionMask) == Light::Empty)
                light = light & ~Light::Red;

            // Update field information
            lightMap[mp] = light;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return ClearColorRight(x + 1, y, mp + 1, stride, lightMap, board);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return ClearColorLeft(x - 1, y, mp - 1, stride, lightMap, board);

            // If we hit crystal, update score
            if ((field & BoardField::Crystal) != BoardField::Empty)
                return Hit(x, y, mp, (Color)(field & BoardField::ColorMask), (Color)(originalLight & Light::ColorMask), (Color)(light & Light::ColorMask));

            // Stop if we hit an object
            if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            y--;
            mp -= stride;
        }
        return Hit(x, y, mp);
    }

    static Hit ClearColorDown(int8 x, int8 y, int16 mp, int8 stride, Light* lightMap, BoardField* board)
    {
        int16 mpMax = boardSize;

        while (mp < mpMax)
        {
            Light originalLight = lightMap[mp];
            Light light = originalLight;

            // Erase all light that comes from up
            light = light & ~Light::DownMask;

            // Clear colors
            if ((light & Light::Blue) != Light::Empty && (light & Light::BlueDirectionMask) == Light::Empty)
                light = light & ~Light::Blue;
            if ((light & Light::Yellow) != Light::Empty && (light & Light::YellowDirectionMask) == Light::Empty)
                light = light & ~Light::Yellow;
            if ((light & Light::Red) != Light::Empty && (light & Light::RedDirectionMask) == Light::Empty)
                light = light & ~Light::Red;

            // Update field information
            lightMap[mp] = light;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return ClearColorLeft(x - 1, y, mp - 1, stride, lightMap, board);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return ClearColorRight(x + 1, y, mp + 1, stride, lightMap, board);

            // If we hit crystal, update score
            if ((field & BoardField::Crystal) != BoardField::Empty)
                return Hit(x, y, mp, (Color)(field & BoardField::ColorMask), (Color)(originalLight & Light::ColorMask), (Color)(light & Light::ColorMask));

            // Stop if we hit an object
            if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            y++;
            mp += stride;
        }
        return Hit(x, y, mp);
    }

    static void InvalidatePreMovesLeft(int8 x, int8 y, int16 mp, int8 stride, PrecalculatedMoves* moves, BoardField* board, bool invalidateCrystals = true)
    {
        while (x >= 0)
        {
            moves[mp].movesCount = -1;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return InvalidatePreMovesDown(x, y + 1, mp + stride, stride, moves, board, invalidateCrystals);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return InvalidatePreMovesUp(x, y - 1, mp - stride, stride, moves, board, invalidateCrystals);

            // If we hit crystal, we need to invalidate all of its directions
            if (invalidateCrystals && (field & BoardField::Crystal) != BoardField::Empty)
            {
                InvalidatePreMovesDown(x, y + 1, mp + stride, stride, moves, board, false);
                InvalidatePreMovesUp(x, y - 1, mp - stride, stride, moves, board, false);
                invalidateCrystals = false;
            }
            // Stop if we hit an object
            else if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            x--;
            mp--;
        }
    }

    static void InvalidatePreMovesRight(int8 x, int8 y, int16 mp, int8 stride, PrecalculatedMoves* moves, BoardField* board, bool invalidateCrystals = true)
    {
        while (x < stride)
        {
            moves[mp].movesCount = -1;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return InvalidatePreMovesUp(x, y - 1, mp - stride, stride, moves, board, invalidateCrystals);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return InvalidatePreMovesDown(x, y + 1, mp + stride, stride, moves, board, invalidateCrystals);

            // If we hit crystal, we need to invalidate all of its directions
            if (invalidateCrystals && (field & BoardField::Crystal) != BoardField::Empty)
            {
                InvalidatePreMovesDown(x, y + 1, mp + stride, stride, moves, board, false);
                InvalidatePreMovesUp(x, y - 1, mp - stride, stride, moves, board, false);
                invalidateCrystals = false;
            }
            // Stop if we hit an object
            else if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            x++;
            mp++;
        }
    }

    static void InvalidatePreMovesUp(int8 x, int8 y, int16 mp, int8 stride, PrecalculatedMoves* moves, BoardField* board, bool invalidateCrystals = true)
    {
        while (y >= 0)
        {
            moves[mp].movesCount = -1;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return InvalidatePreMovesRight(x + 1, y, mp + 1, stride, moves, board, invalidateCrystals);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return InvalidatePreMovesLeft(x - 1, y, mp - 1, stride, moves, board, invalidateCrystals);

            // If we hit crystal, we need to invalidate all of its directions
            if (invalidateCrystals && (field & BoardField::Crystal) != BoardField::Empty)
            {
                InvalidatePreMovesLeft(x - 1, y, mp - 1, stride, moves, board, false);
                InvalidatePreMovesRight(x + 1, y, mp + 1, stride, moves, board, false);
                invalidateCrystals = false;
            }
            // Stop if we hit an object
            else if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            y--;
            mp -= stride;
        }
    }

    static void InvalidatePreMovesDown(int8 x, int8 y, int16 mp, int8 stride, PrecalculatedMoves* moves, BoardField* board, bool invalidateCrystals = true)
    {
        int16 mpMax = boardSize;

        while (mp < mpMax)
        {
            moves[mp].movesCount = -1;

            // If we hit the mirror, we need to continue our search
            BoardField field = board[mp];

            if ((field & BoardField::MirrorSlash) != BoardField::Empty)
                return InvalidatePreMovesLeft(x - 1, y, mp - 1, stride, moves, board, invalidateCrystals);
            if ((field & BoardField::MirrorBackSlash) != BoardField::Empty)
                return InvalidatePreMovesRight(x + 1, y, mp + 1, stride, moves, board, invalidateCrystals);

            // If we hit crystal, we need to invalidate all of its directions
            if (invalidateCrystals && (field & BoardField::Crystal) != BoardField::Empty)
            {
                InvalidatePreMovesLeft(x - 1, y, mp - 1, stride, moves, board, false);
                InvalidatePreMovesRight(x + 1, y, mp + 1, stride, moves, board, false);
                invalidateCrystals = false;
            }
            // Stop if we hit an object
            else if ((field & BoardField::ObjectMask) != BoardField::Empty)
                break;
            y++;
            mp += stride;
        }
    }

    static Color GetLeftColor(Light light)
    {
        Color color = Color::Empty;

        if ((light & Light::BlueLeft) != Light::Empty)
            color |= Color::Blue;
        if ((light & Light::YellowLeft) != Light::Empty)
            color |= Color::Yellow;
        if ((light & Light::RedLeft) != Light::Empty)
            color |= Color::Red;
        return color;
    }

    static Color GetRightColor(Light light)
    {
        Color color = Color::Empty;

        if ((light & Light::BlueRight) != Light::Empty)
            color |= Color::Blue;
        if ((light & Light::YellowRight) != Light::Empty)
            color |= Color::Yellow;
        if ((light & Light::RedRight) != Light::Empty)
            color |= Color::Red;
        return color;
    }

    static Color GetUpColor(Light light)
    {
        Color color = Color::Empty;

        if ((light & Light::BlueUp) != Light::Empty)
            color |= Color::Blue;
        if ((light & Light::YellowUp) != Light::Empty)
            color |= Color::Yellow;
        if ((light & Light::RedUp) != Light::Empty)
            color |= Color::Red;
        return color;
    }

    static Color GetDownColor(Light light)
    {
        Color color = Color::Empty;

        if ((light & Light::BlueDown) != Light::Empty)
            color |= Color::Blue;
        if ((light & Light::YellowDown) != Light::Empty)
            color |= Color::Yellow;
        if ((light & Light::RedDown) != Light::Empty)
            color |= Color::Red;
        return color;
    }
};

int16 State::boardSize = -1;

Move::Move(State* state, Lantern lantern, int cost)
    : state(state)
    , lantern(lantern)
    , type(MoveType::Lantern)
    , hash(state->hash ^ lantern.GetHash())
{
    state->GetLanternScore(lantern, cost, score, potentialScore);
}

Move::Move(State* state, Obstacle obstacle, int cost)
    : state(state)
    , obstacle(obstacle)
    , type(MoveType::Obstacle)
    , hash(state->hash ^ obstacle.GetHash())
{
    state->GetObstacleScore(obstacle, cost, score, potentialScore);
}

Move::Move(State* state, Mirror mirror, int cost)
    : state(state)
    , mirror(mirror)
    , type(MoveType::Mirror)
    , hash(state->hash ^ mirror.GetHash())
{
    state->GetMirrorScore(mirror, cost, score, potentialScore);
}

void Move::ApplyToMe(int costLantern, int costObstacle, int costMirror)
{
    switch (type)
    {
    case MoveType::Lantern:
        state->PutLantern(lantern, costLantern);
        break;
    case MoveType::Obstacle:
        state->PutObstacle(obstacle, costObstacle);
        break;
    case MoveType::Mirror:
        state->PutMirror(mirror, costMirror);
        break;
    }
}

State Move::Apply(int costLantern, int costObstacle, int costMirror) const
{
    State result = *state;

    switch (type)
    {
        case MoveType::Lantern:
            result.PutLantern(lantern, costLantern);
            break;
        case MoveType::Obstacle:
            result.PutObstacle(obstacle, costObstacle);
            break;
        case MoveType::Mirror:
            result.PutMirror(mirror, costMirror);
            break;
    }

    return result;
}

bool Move::Same(const Move& other) const
{
    if (hash != other.hash)
        return false;
    if (score + state->score != other.score + other.state->score)
        return false;
    if (potentialScore + state->potentialScore != other.potentialScore + other.state->potentialScore)
        return false;
    size_t mirrorsCount = state->mirrors.size() + (type == MoveType::Mirror ? 1 : 0);
    size_t otherMirrorsCount = other.state->mirrors.size() + (other.type == MoveType::Mirror ? 1 : 0);
    if (mirrorsCount != otherMirrorsCount)
        return false;
    size_t obstaclesCount = state->obstacles.size() + (type == MoveType::Obstacle ? 1 : 0);
    size_t otherObstaclesCount = other.state->obstacles.size() + (other.type == MoveType::Obstacle ? 1 : 0);
    if (obstaclesCount != otherObstaclesCount)
        return false;
    size_t lanternsCount = state->lanterns.size() + (type == MoveType::Lantern ? 1 : 0);
    size_t otherLanternsCount = other.state->lanterns.size() + (other.type == MoveType::Lantern ? 1 : 0);
    if (lanternsCount != otherLanternsCount)
        return false;
    for (auto& o : state->obstacles)
    {
        int16 mp = o.position.y * state->width + o.position.x;

        if (other.state->board[mp] != state->board[mp])
        {
            if (other.type == MoveType::Obstacle)
                if (o.position == other.obstacle.position)
                    continue;
            return false;
        }
    }
    if (type == MoveType::Obstacle && other.state->board[obstacle.position.y * state->width + obstacle.position.x] != BoardField::Obstacle)
        return false;
    for (auto& m : state->mirrors)
    {
        int16 mp = m.position.y * state->width + m.position.x;

        if (other.state->board[mp] != state->board[mp])
        {
            if (other.type == MoveType::Mirror)
                if (m.position == other.mirror.position && m.slash == other.mirror.slash)
                    continue;
            return false;
        }
    }
    if (type == MoveType::Mirror && other.state->board[mirror.position.y * state->width + mirror.position.x] != (mirror.slash ? BoardField::MirrorSlash : BoardField::MirrorBackSlash))
        return false;
    for (auto& l : state->lanterns)
    {
        int16 mp = l.position.y * state->width + l.position.x;

        if (other.state->board[mp] != state->board[mp])
        {
            if (other.type == MoveType::Lantern)
                if (l.position == other.lantern.position && l.color == other.lantern.color)
                    continue;
            return false;
        }
    }
    if (type == MoveType::Lantern && other.state->board[lantern.position.y * state->width + lantern.position.x] != (BoardField::Lantern | (BoardField)lantern.color))
        return false;
    return true;
}

class CrystalLighting
{
private:
    double stopwatchStart;

public:
    vector<string> placeItems(vector<string> targetBoard, int costLantern, int costMirror, int costObstacle, int maxMirrors, int maxObstacles)
    {
        // Start stopwatch
        stopwatchStart = getTime();

        // Parse input data
        int height = (int)targetBoard.size();
        int width = (int)targetBoard[0].size();
        State inputState(width, height);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                BoardField field;

                switch (targetBoard[y][x])
                {
                case '.':
                default:
                    field = BoardField::Empty;
                    break;
                case 'X':
                    field = BoardField::Obstacle;
                    break;
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                    field = BoardField::Crystal | (BoardField)(targetBoard[y][x] - '0');
                    break;
                }
                inputState.Board(y, x) = field;
            }
        inputState.UpdateFromBoard();

        maxMirrors = 0; // TODO:
        // Do place items on the board
        State solution = inputState;

        for (maxRayWidth = 1; !TimeExceeded(); maxRayWidth *= 5)
        {
            State s = Solve(inputState, costLantern, costMirror, costObstacle, maxMirrors, maxObstacles);

            if (s.score > solution.score)
                solution = s;
        }

        // Return result
        vector<string> result;

        for (auto& obstacle : solution.obstacles)
        {
            stringstream ss;
            ss << (int)obstacle.position.y << " " << (int)obstacle.position.x << " X";
            result.push_back(ss.str());
        }
        for (auto& mirror : solution.mirrors)
        {
            stringstream ss;
            ss << (int)mirror.position.y << " " << (int)mirror.position.x << " " << (mirror.slash ? '/' : '\\');
            result.push_back(ss.str());
        }
        for (auto& lantern : solution.lanterns)
        {
            stringstream ss;
            ss << (int)lantern.position.y << " " << (int)lantern.position.x << " " << (int)lantern.color;
            result.push_back(ss.str());
        }
        return result;
    }

private:
    bool TimeExceeded()
    {
        return ElapsedSeconds() >= MAX_EXECUTION_TIME;
    }

    double ElapsedSeconds()
    {
        return getTime() - stopwatchStart;
    }

    State Solve(State inputState, int costLantern, int costMirror, int costObstacle, int maxMirrors, int maxObstacles)
    {
        vector<int> refCounts;
        vector<Move> moves, currentMoves;
        vector<State> previousStates;
        vector<State> newStates;
        State bestSolution = inputState;
        int steps = 0;

        previousStates.push_back(inputState);
        while (!TimeExceeded() && !previousStates.empty())
        {
#if LOCAL
            //cerr << steps << ". " << bestSolution.score << " " << ElapsedSeconds() << "s " << bestSolution.lanterns.size() << " " << bestSolution.mirrors.size() << " " << bestSolution.obstacles.size() << endl;
#endif
            steps++;
            for (auto& previousState : previousStates)
            {
                if (TimeExceeded())
                    break;

#ifdef USE_SLOW_ALGORITHM
                int16 mp = 0;
                for (int8 y = 0; y < inputState.height; y++)
                {
                    if (TimeExceeded())
                        break;
                    for (int8 x = 0; x < inputState.width; x++, mp++)
                        if ((previousState.board[mp] & BoardField::ObjectMask) == BoardField::Empty)
                        {
                            if ((previousState.lightMap[mp] & Light::ColorMask) == Light::Empty)
                            {
                                // See if any crystal can be hit from this position in the map
                                Color color = (Color)(previousState.crystalsLightMap[mp] & Light::ColorMask);

                                if (color == Color::Empty)
                                    continue;

                                Lantern lantern;
                                lantern.position.x = x;
                                lantern.position.y = y;

                                // Try putting lantern
                                if ((color & Color::Blue) != Color::Empty)
                                {
                                    lantern.color = Color::Blue;
                                    AddMove(Move(&previousState, lantern, costLantern), moves);
                                }
                                if ((color & Color::Yellow) != Color::Empty)
                                {
                                    lantern.color = Color::Yellow;
                                    AddMove(Move(&previousState, lantern, costLantern), moves);
                                }
                                if ((color & Color::Red) != Color::Empty)
                                {
                                    lantern.color = Color::Red;
                                    AddMove(Move(&previousState, lantern, costLantern), moves);
                                }
                            }
                            else
                            {
                                // Try to put Obstacle
                                if ((int)previousState.obstacles.size() < maxObstacles)
                                {
                                    Obstacle obstacle;
                                    obstacle.position.x = x;
                                    obstacle.position.y = y;
                                    AddMove(Move(&previousState, obstacle, costObstacle), moves);
                                }

                                // Try to put slash Mirror '/'
                                if ((int)previousState.mirrors.size() >= maxMirrors)
                                    continue;
                                Mirror mirror;
                                mirror.position.x = x;
                                mirror.position.y = y;
                                mirror.slash = true;

                                if (previousState.IsPuttingMirrorSafe(mirror))
                                    AddMove(Move(&previousState, mirror, costMirror), moves);

                                // Try to put backslash Mirror '\'
                                mirror.slash = false;
                                if (previousState.IsPuttingMirrorSafe(mirror))
                                    AddMove(Move(&previousState, mirror, costMirror), moves);
                            }
                        }
                }
#else
                previousState.GetTopMoves(currentMoves, maxRayWidth, costLantern, costMirror, costObstacle, maxMirrors, maxObstacles);
                for (auto& move : currentMoves)
                    AddMove(move, moves);
#endif
            }

            // Check if we can reuse current state object instead of creating a copy
            refCounts.clear();
            refCounts.resize(previousStates.size(), 0);
            for (auto& move : moves)
                refCounts[move.state - previousStates.data()]++;

            // Convert moves to states
            for (auto& move : moves)
            {
                int index = move.state - previousStates.data();
                int& refCount = refCounts[index];
                bool reuse = refCount == 1;

                if (reuse)
                {
                    move.ApplyToMe(costLantern, costObstacle, costMirror);
                    newStates.emplace_back(std::move(*move.state));
                }
                else
                    newStates.emplace_back(move.Apply(costLantern, costObstacle, costMirror));
                refCount--;
            }

            // Check if we found better solution
            for (auto& state : newStates)
                if (state.score > bestSolution.score)
                    bestSolution = state;

            // Store new states to previous states
            previousStates.swap(newStates);
            newStates.clear();
            moves.clear();
        }

        cerr << steps << ". " << bestSolution.score << " " << ElapsedSeconds() << "s " << bestSolution.lanterns.size() << " " << bestSolution.mirrors.size() << " " << bestSolution.obstacles.size() << endl;
        return bestSolution;
    }

    static size_t maxRayWidth;

    static bool MoveComparison(const Move& m1, const Move& m2)
    {
#ifdef USE_POTENTIAL_SCORE
        return m2.potentialScore + m2.state->potentialScore < m1.potentialScore + m1.state->potentialScore;
#else
        return m2.score + m2.state->score < m1.score + m1.state->score;
#endif
    }

    void AddMove(const Move& move, vector<Move>& moves)
    {
        if (moves.size() < maxRayWidth)
        {
            if (!moves.empty())
            {
                auto it = lower_bound(moves.begin(), moves.end(), move, MoveComparison);

#ifdef USE_POTENTIAL_SCORE
                while (it != moves.end() && it->potentialScore + it->state->potentialScore == move.potentialScore + move.state->potentialScore)
#else
                while (it != moves.end() && it->score + it->state->score == move.score + move.state->score)
#endif
                {
                    if (it->hash == move.hash && it->Same(move))
                        return;
                    it++;
                }
                moves.insert(it, move);
            }
            else
                moves.push_back(move);
        }
        else
        {
#ifdef USE_POTENTIAL_SCORE
            if (moves[moves.size() - 1].potentialScore + moves[moves.size() - 1].state->potentialScore >= move.potentialScore + move.state->potentialScore)
#else
            if (moves[moves.size() - 1].score + moves[moves.size() - 1].state->score >= move.score + move.state->score)
#endif
                return;

            auto it = lower_bound(moves.begin(), moves.end(), move, MoveComparison);

#ifdef USE_POTENTIAL_SCORE
            while (it != moves.end() && it->potentialScore + it->state->potentialScore == move.potentialScore + move.state->potentialScore)
#else
            while (it != moves.end() && it->score + it->state->score == move.score + move.state->score)
#endif
            {
                if (it->hash == move.hash && it->Same(move))
                    return;
                it++;
            }
            moves.insert(it, move);
            moves.resize(maxRayWidth);
        }
    }
};

size_t CrystalLighting::maxRayWidth = 0;
