using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

[Flags]
enum BoardField : byte
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
}

[Flags]
enum Light : ushort
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
}

struct Position
{
    public sbyte X;
    public sbyte Y;

    public Position(sbyte x, sbyte y)
        : this()
    {
        X = x;
        Y = y;
    }

    public override string ToString()
    {
        return string.Format("({0}, {1})", X, Y);
    }
}

struct Obstacle
{
    public Position Position;

    public override string ToString()
    {
        return Position.ToString();
    }
}

struct Mirror
{
    public Position Position;
    public bool Slash;

    public override string ToString()
    {
        return string.Format("{0}: {1}", Position, Slash ? '/' : '\\');
    }
}

struct Lantern
{
    public Position Position;
    public BoardField Color;

    public override string ToString()
    {
        return string.Format("{0}: {1}", Position, Color);
    }
}

struct State
{
    public State(int width, int height)
        : this()
    {
        Board = new BoardField[height, width];
        LightMap = new Light[height, width];
        Obstacles = new List<Obstacle>(100);
        Mirrors = new List<Mirror>(100);
        Lanterns = new List<Lantern>(100);
        Score = 0;
    }

    public BoardField[,] Board;
    public Light[,] LightMap;
    public List<Obstacle> Obstacles;
    public List<Mirror> Mirrors;
    public List<Lantern> Lanterns;
    public int Score;

    public int Height { get { return Board.GetLength(0); } }
    public int Width { get { return Board.GetLength(1); } }

    public void Copy(State other)
    {
        Array.Copy(other.Board, Board, other.Board.Length);
        Array.Copy(other.LightMap, LightMap, other.LightMap.Length);
        for (int i = Math.Min(Obstacles.Count, other.Obstacles.Count) - 1; i >= 0; i--)
            Obstacles[i] = other.Obstacles[i];
        if (Obstacles.Count > other.Obstacles.Count)
            Obstacles.RemoveRange(other.Obstacles.Count, Obstacles.Count - other.Obstacles.Count);
        else
            for (int i = Obstacles.Count; i < other.Obstacles.Count; i++)
                Obstacles.Add(other.Obstacles[i]);
        for (int i = Math.Min(Mirrors.Count, other.Mirrors.Count) - 1; i >= 0; i--)
            Mirrors[i] = other.Mirrors[i];
        if (Mirrors.Count > other.Mirrors.Count)
            Mirrors.RemoveRange(other.Mirrors.Count, Mirrors.Count - other.Mirrors.Count);
        else
            for (int i = Mirrors.Count; i < other.Mirrors.Count; i++)
                Mirrors.Add(other.Mirrors[i]);
        for (int i = Math.Min(Lanterns.Count, other.Lanterns.Count) - 1; i >= 0; i--)
            Lanterns[i] = other.Lanterns[i];
        if (Lanterns.Count > other.Lanterns.Count)
            Lanterns.RemoveRange(other.Lanterns.Count, Lanterns.Count - other.Lanterns.Count);
        else
            for (int i = Lanterns.Count; i < other.Lanterns.Count; i++)
                Lanterns.Add(other.Lanterns[i]);
        Score = other.Score;
    }

    public bool Same(State other)
    {
        if (Score != other.Score || Lanterns.Count != other.Lanterns.Count || Obstacles.Count != other.Obstacles.Count || Mirrors.Count != other.Mirrors.Count)
            return false;
        foreach (Obstacle obstacle in Obstacles)
            if (other.Board[obstacle.Position.Y, obstacle.Position.X] != Board[obstacle.Position.Y, obstacle.Position.X])
                return false;
        foreach (Mirror mirror in Mirrors)
            if (other.Board[mirror.Position.Y, mirror.Position.X] != Board[mirror.Position.Y, mirror.Position.X])
                return false;
        foreach (Lantern lantern in Lanterns)
            if (other.Board[lantern.Position.Y, lantern.Position.X] != Board[lantern.Position.Y, lantern.Position.X])
                return false;
        return true;
    }

    public bool PutLantern(Position position, Light color, int cost)
    {
        if (!AddColorLeft(position.X - 1, position.Y, color))
            return false;
        if (!AddColorRight(position.X + 1, position.Y, color))
            return false;
        if (!AddColorUp(position.X, position.Y - 1, color))
            return false;
        if (!AddColorDown(position.X, position.Y + 1, color))
            return false;
        Score -= cost;
        BoardField fieldColor = BoardField.Empty;
        if (color == Light.Red)
            fieldColor = BoardField.Red;
        else if (color == Light.Blue)
            fieldColor = BoardField.Blue;
        else if (color == Light.Yellow)
            fieldColor = BoardField.Yellow;
        Board[position.Y, position.X] |= BoardField.Lantern | fieldColor;
        Lanterns.Add(new Lantern()
        {
            Color = fieldColor,
            Position = position,
        });
        return true;
    }

    public void PutObstacle(Position position, int cost)
    {
        ClearColor(position);
        Score -= cost;
        Obstacles.Add(new Obstacle()
        {
            Position = position,
        });
        Board[position.Y, position.X] |= BoardField.Obstacle;
    }

    public bool PutMirror(Position position, BoardField mirrorType, int cost)
    {
        Light light = LightMap[position.Y, position.X];

        ClearColor(position);
        if (mirrorType == BoardField.MirrorSlash)
        {
            if ((light & Light.LeftMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueLeft) == Light.BlueLeft ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowLeft) == Light.YellowLeft ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedLeft) == Light.RedLeft ? Light.Red : Light.Empty);

                if (!AddColorDown(position.X, position.Y + 1, color))
                    return false;
            }
            if ((light & Light.RightMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueRight) == Light.BlueRight ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowRight) == Light.YellowRight ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedRight) == Light.RedRight ? Light.Red : Light.Empty);

                if (!AddColorUp(position.X, position.Y - 1, color))
                    return false;
            }
            if ((light & Light.DownMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueDown) == Light.BlueDown ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowDown) == Light.YellowDown ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedDown) == Light.RedDown ? Light.Red : Light.Empty);

                if (!AddColorLeft(position.X - 1, position.Y, color))
                    return false;
            }
            if ((light & Light.UpMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueUp) == Light.BlueUp ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowUp) == Light.YellowUp ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedUp) == Light.RedUp ? Light.Red : Light.Empty);

                if (!AddColorRight(position.X + 1, position.Y, color))
                    return false;
            }
        }
        else
        {
            if ((light & Light.LeftMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueLeft) == Light.BlueLeft ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowLeft) == Light.YellowLeft ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedLeft) == Light.RedLeft ? Light.Red : Light.Empty);

                if (!AddColorUp(position.X, position.Y - 1, color))
                    return false;
            }
            if ((light & Light.RightMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueRight) == Light.BlueRight ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowRight) == Light.YellowRight ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedRight) == Light.RedRight ? Light.Red : Light.Empty);

                if (!AddColorDown(position.X, position.Y + 1, color))
                    return false;
            }
            if ((light & Light.DownMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueDown) == Light.BlueDown ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowDown) == Light.YellowDown ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedDown) == Light.RedDown ? Light.Red : Light.Empty);

                if (!AddColorRight(position.X + 1, position.Y, color))
                    return false;
            }
            if ((light & Light.UpMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueUp) == Light.BlueUp ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowUp) == Light.YellowUp ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedUp) == Light.RedUp ? Light.Red : Light.Empty);

                if (!AddColorLeft(position.X - 1, position.Y, color))
                    return false;
            }
        }
        Score -= cost;
        Mirrors.Add(new Mirror()
        {
            Position = position,
            Slash = mirrorType == BoardField.MirrorSlash,
        });
        Board[position.Y, position.X] |= mirrorType;
        return true;
    }

    public bool HasColor(Position position)
    {
        return (LightMap[position.Y, position.X] & Light.ColorMask) != Light.Empty;
    }

    public bool HasObject(Position position)
    {
        return (Board[position.Y, position.X] & BoardField.ObjectMask) != BoardField.Empty;
    }

    private bool HasObject(int x, int y)
    {
        return (Board[y, x] & BoardField.ObjectMask) != BoardField.Empty;
    }

    private void ClearColor(Position position)
    {
        Light lightDirection = LightMap[position.Y, position.X];

        if ((lightDirection & Light.LeftMask) != Light.Empty)
            ClearColorLeft(position.X - 1, position.Y);
        if ((lightDirection & Light.RightMask) != Light.Empty)
            ClearColorRight(position.X + 1, position.Y);
        if ((lightDirection & Light.DownMask) != Light.Empty)
            ClearColorDown(position.X, position.Y + 1);
        if ((lightDirection & Light.UpMask) != Light.Empty)
            ClearColorUp(position.X, position.Y - 1);
    }

    private void ClearColorDown(int x, int y)
    {
        int height = Height;

        while (y < height)
        {
            Light originalLight = LightMap[y, x];
            Light light = originalLight;

            // Erase all light that comes from down
            light = light & ~Light.DownMask;

            // Clear colors
            if ((light & Light.Blue) == Light.Blue && (light & Light.BlueDirectionMask) == Light.Empty)
                light = light & ~Light.Blue;
            if ((light & Light.Yellow) == Light.Yellow && (light & Light.YellowDirectionMask) == Light.Empty)
                light = light & ~Light.Yellow;
            if ((light & Light.Red) == Light.Red && (light & Light.RedDirectionMask) == Light.Empty)
                light = light & ~Light.Red;

            // Update field information
            LightMap[y, x] = light;

            // If we hit the mirror, we need to continue our search
            BoardField field = Board[y, x];

            if ((field & BoardField.MirrorSlash) == BoardField.MirrorSlash)
            {
                ClearColorLeft(x - 1, y);
                break;
            }
            if ((field & BoardField.MirrorBackSlash) == BoardField.MirrorBackSlash)
            {
                ClearColorRight(x + 1, y);
                break;
            }

            // If we hit crystal, update score
            if ((field & BoardField.Crystal) == BoardField.Crystal)
                UpdateCrystal(x, y, originalLight);

            // Stop if we hit an object
            if (HasObject(x, y))
                break;
            y++;
        }
    }

    private void ClearColorUp(int x, int y)
    {
        while (y >= 0)
        {
            Light originalLight = LightMap[y, x];
            Light light = originalLight;

            // Erase all light that comes from up
            light = light & ~Light.UpMask;

            // Clear colors
            if ((light & Light.Blue) == Light.Blue && (light & Light.BlueDirectionMask) == Light.Empty)
                light = light & ~Light.Blue;
            if ((light & Light.Yellow) == Light.Yellow && (light & Light.YellowDirectionMask) == Light.Empty)
                light = light & ~Light.Yellow;
            if ((light & Light.Red) == Light.Red && (light & Light.RedDirectionMask) == Light.Empty)
                light = light & ~Light.Red;

            // Update field information
            LightMap[y, x] = light;

            // If we hit the mirror, we need to continue our search
            BoardField field = Board[y, x];

            if ((field & BoardField.MirrorSlash) == BoardField.MirrorSlash)
            {
                ClearColorRight(x + 1, y);
                break;
            }
            if ((field & BoardField.MirrorBackSlash) == BoardField.MirrorBackSlash)
            {
                ClearColorLeft(x - 1, y);
                break;
            }

            // If we hit crystal, update score
            if ((field & BoardField.Crystal) == BoardField.Crystal)
                UpdateCrystal(x, y, originalLight);

            // Stop if we hit an object
            if (HasObject(x, y))
                break;
            y--;
        }
    }

    private void ClearColorLeft(int x, int y)
    {
        while (x >= 0)
        {
            Light originalLight = LightMap[y, x];
            Light light = originalLight;

            // Erase all light that comes from left
            light = light & ~Light.LeftMask;

            // Clear colors
            if ((light & Light.Blue) == Light.Blue && (light & Light.BlueDirectionMask) == Light.Empty)
                light = light & ~Light.Blue;
            if ((light & Light.Yellow) == Light.Yellow && (light & Light.YellowDirectionMask) == Light.Empty)
                light = light & ~Light.Yellow;
            if ((light & Light.Red) == Light.Red && (light & Light.RedDirectionMask) == Light.Empty)
                light = light & ~Light.Red;

            // Update field information
            LightMap[y, x] = light;

            // If we hit the mirror, we need to continue our search
            BoardField field = Board[y, x];

            if ((field & BoardField.MirrorSlash) == BoardField.MirrorSlash)
            {
                ClearColorDown(x, y + 1);
                break;
            }
            if ((field & BoardField.MirrorBackSlash) == BoardField.MirrorBackSlash)
            {
                ClearColorUp(x, y - 1);
                break;
            }

            // If we hit crystal, update score
            if ((field & BoardField.Crystal) == BoardField.Crystal)
                UpdateCrystal(x, y, originalLight);

            // Stop if we hit an object
            if (HasObject(x, y))
                break;
            x--;
        }
    }

    private void ClearColorRight(int x, int y)
    {
        int width = Width;

        while (x < width)
        {
            Light originalLight = LightMap[y, x];
            Light light = originalLight;

            // Erase all light that comes from right
            light = light & ~Light.RightMask;

            // Clear colors
            if ((light & Light.Blue) == Light.Blue && (light & Light.BlueDirectionMask) == Light.Empty)
                light = light & ~Light.Blue;
            if ((light & Light.Yellow) == Light.Yellow && (light & Light.YellowDirectionMask) == Light.Empty)
                light = light & ~Light.Yellow;
            if ((light & Light.Red) == Light.Red && (light & Light.RedDirectionMask) == Light.Empty)
                light = light & ~Light.Red;

            // Update field information
            LightMap[y, x] = light;

            // If we hit the mirror, we need to continue our search
            BoardField field = Board[y, x];

            if ((field & BoardField.MirrorSlash) == BoardField.MirrorSlash)
            {
                ClearColorUp(x, y - 1);
                break;
            }
            if ((field & BoardField.MirrorBackSlash) == BoardField.MirrorBackSlash)
            {
                ClearColorDown(x, y + 1);
                break;
            }

            // If we hit crystal, update score
            if ((field & BoardField.Crystal) == BoardField.Crystal)
                UpdateCrystal(x, y, originalLight);

            // Stop if we hit an object
            if (HasObject(x, y))
                break;
            x++;
        }
    }

    private bool AddColorDown(int x, int y, Light color)
    {
        int height = Height;
        Light direction = Light.Empty;

        if (color == Light.Blue)
            direction = Light.BlueDown;
        else if (color == Light.Red)
            direction = Light.RedDown;
        else if (color == Light.Yellow)
            direction = Light.YellowDown;
        while (y < height)
        {
            Light originalLight = LightMap[y, x];

            LightMap[y, x] = originalLight | color | direction;

            // If we hit the mirror, we need to continue our search
            BoardField field = Board[y, x];

            if ((field & BoardField.MirrorSlash) == BoardField.MirrorSlash)
            {
                AddColorLeft(x - 1, y, color);
                break;
            }
            if ((field & BoardField.MirrorBackSlash) == BoardField.MirrorBackSlash)
            {
                AddColorRight(x + 1, y, color);
                break;
            }

            // If we hit crystal, update score
            if ((field & BoardField.Crystal) == BoardField.Crystal)
                UpdateCrystal(x, y, originalLight);

            // If we hit lantern we played wrong move
            if ((field & BoardField.Lantern) == BoardField.Lantern)
                return false;

            // Stop if we hit an object
            if (HasObject(x, y))
                break;
            y++;
        }
        return true;
    }

    private bool AddColorUp(int x, int y, Light color)
    {
        Light direction = Light.Empty;

        if (color == Light.Blue)
            direction = Light.BlueUp;
        else if (color == Light.Red)
            direction = Light.RedUp;
        else if (color == Light.Yellow)
            direction = Light.YellowUp;
        while (y >= 0)
        {
            Light originalLight = LightMap[y, x];

            LightMap[y, x] = originalLight | color | direction;

            // If we hit the mirror, we need to continue our search
            BoardField field = Board[y, x];

            if ((field & BoardField.MirrorSlash) == BoardField.MirrorSlash)
            {
                AddColorRight(x + 1, y, color);
                break;
            }
            if ((field & BoardField.MirrorBackSlash) == BoardField.MirrorBackSlash)
            {
                AddColorLeft(x - 1, y, color);
                break;
            }

            // If we hit crystal, update score
            if ((field & BoardField.Crystal) == BoardField.Crystal)
                UpdateCrystal(x, y, originalLight);

            // If we hit lantern we played wrong move
            if ((field & BoardField.Lantern) == BoardField.Lantern)
                return false;

            // Stop if we hit an object
            if (HasObject(x, y))
                break;
            y--;
        }
        return true;
    }

    private bool AddColorRight(int x, int y, Light color)
    {
        int width = Width;
        Light direction = Light.Empty;

        if (color == Light.Blue)
            direction = Light.BlueRight;
        else if (color == Light.Red)
            direction = Light.RedRight;
        else if (color == Light.Yellow)
            direction = Light.YellowRight;
        while (x < width)
        {
            Light originalLight = LightMap[y, x];

            LightMap[y, x] = originalLight | color | direction;

            // If we hit the mirror, we need to continue our search
            BoardField field = Board[y, x];

            if ((field & BoardField.MirrorSlash) == BoardField.MirrorSlash)
            {
                AddColorUp(x, y - 1, color);
                break;
            }
            if ((field & BoardField.MirrorBackSlash) == BoardField.MirrorBackSlash)
            {
                AddColorDown(x, y + 1, color);
                break;
            }

            // If we hit crystal, update score
            if ((field & BoardField.Crystal) == BoardField.Crystal)
                UpdateCrystal(x, y, originalLight);

            // If we hit lantern we played wrong move
            if ((field & BoardField.Lantern) == BoardField.Lantern)
                return false;

            // Stop if we hit an object
            if (HasObject(x, y))
                break;
            x++;
        }
        return true;
    }

    private bool AddColorLeft(int x, int y, Light color)
    {
        Light direction = Light.Empty;

        if (color == Light.Blue)
            direction = Light.BlueLeft;
        else if (color == Light.Red)
            direction = Light.RedLeft;
        else if (color == Light.Yellow)
            direction = Light.YellowLeft;
        while (x >= 0)
        {
            Light originalLight = LightMap[y, x];

            LightMap[y, x] = originalLight | color | direction;

            // If we hit the mirror, we need to continue our search
            BoardField field = Board[y, x];

            if ((field & BoardField.MirrorSlash) == BoardField.MirrorSlash)
            {
                AddColorDown(x, y + 1, color);
                break;
            }
            if ((field & BoardField.MirrorBackSlash) == BoardField.MirrorBackSlash)
            {
                AddColorUp(x, y - 1, color);
                break;
            }

            // If we hit crystal, update score
            if ((field & BoardField.Crystal) == BoardField.Crystal)
                UpdateCrystal(x, y, originalLight);

            // If we hit lantern we played wrong move
            if ((field & BoardField.Lantern) == BoardField.Lantern)
                return false;

            // Stop if we hit an object
            if (HasObject(x, y))
                break;
            x--;
        }
        return true;
    }

    private void UpdateCrystal(int x, int y, Light originalLight)
    {
        Light previousColor = originalLight & Light.ColorMask;
        Light color = (Light)(Board[y, x] & BoardField.ColorMask);
        Light newColor = LightMap[y, x] & Light.ColorMask;

        int previousScore = GetCrystalScore(color, previousColor);
        int newScore = GetCrystalScore(color, newColor);

        Score += newScore - previousScore;
    }

    private static int GetCrystalScore(Light crystalColor, Light lightColor)
    {
        if (lightColor == Light.Empty)
            return 0;

        if (crystalColor == lightColor)
        {
            if (crystalColor == Light.Blue || crystalColor == Light.Red || crystalColor == Light.Yellow)
                return 20;
            return 30;
        }
        return -10;
    }
}

public class CrystalLighting
{
    public string[] placeItems(string[] targetBoard, int costLantern, int costMirror, int costObstacle, int maxMirrors, int maxObstacles)
    {
        int height = targetBoard.Length;
        int width = targetBoard[0].Length;
        State inputState = new State(width, height);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                BoardField field;

                switch (targetBoard[y][x])
                {
                    case '.':
                    default:
                        field = BoardField.Empty;
                        break;
                    case 'X':
                        field = BoardField.Obstacle;
                        break;
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                        field = BoardField.Crystal | (BoardField)(targetBoard[y][x] - '0');
                        break;
                }
                inputState.Board[y, x] = field;
            }

        State solution = Solve(inputState, costLantern, costMirror, costObstacle, maxMirrors, maxObstacles);
        List<string> result = new List<string>();

        foreach (var obstacle in solution.Obstacles)
            result.Add(string.Format("{0} {1} X", obstacle.Position.Y, obstacle.Position.X));
        foreach (var mirror in solution.Mirrors)
            result.Add(string.Format("{0} {1} {2}", mirror.Position.Y, mirror.Position.X, mirror.Slash ? '/' : '\\'));
        foreach (var lantern in solution.Lanterns)
            result.Add(string.Format("{0} {1} {2}", lantern.Position.Y, lantern.Position.X, (int)lantern.Color));
        return result.ToArray();
    }

    private State Solve(State inputState, int costLantern, int costMirror, int costObstacle, int maxMirrors, int maxObstacles)
    {
        Stopwatch sw = Stopwatch.StartNew();
        State[] stateCache = new State[1000000];
        int stateCacheCount = 0;
        List<State> solutions = new List<State>();
        List<State> previousSolutions = new List<State>();
        State solution = CloneState(stateCache, ref stateCacheCount, inputState);
        State bestSolution = inputState;
        sbyte height = (sbyte)inputState.Board.GetLength(0);
        sbyte width = (sbyte)inputState.Board.GetLength(1);
        Position position = new Position();
        TimeSpan maxTime = Debugger.IsAttached ? TimeSpan.FromSeconds(99999.5) : TimeSpan.FromSeconds(90.5);
        int steps = 0;

        previousSolutions.Add(CloneState(stateCache, ref stateCacheCount, inputState));
        while (sw.Elapsed < maxTime && previousSolutions.Count > 0)
        {
            steps++;
            foreach (State previousState in previousSolutions)
            {
                if (sw.Elapsed > maxTime)
                    break;
                for (position.Y = 0; position.Y < height; position.Y++)
                {
                    if (sw.Elapsed > maxTime)
                        break;
                    for (position.X = 0; position.X < width; position.X++)
                        if (!previousState.HasObject(position))
                        {
                            if (!previousState.HasColor(position))
                            {
                                // Try to put Blue lantern
                                solution.Copy(previousState);
                                if (solution.PutLantern(position, Light.Blue, costLantern))
                                    solutions.Add(CloneState(stateCache, ref stateCacheCount, solution));

                                // Try to put Yellow lantern
                                solution.Copy(previousState);
                                if (solution.PutLantern(position, Light.Yellow, costLantern))
                                    solutions.Add(CloneState(stateCache, ref stateCacheCount, solution));

                                // Try to put Red lantern
                                solution.Copy(previousState);
                                if (solution.PutLantern(position, Light.Red, costLantern))
                                    solutions.Add(CloneState(stateCache, ref stateCacheCount, solution));
                            }
                            else
                            {
                                // Try to put Obstacle
                                if (previousState.Obstacles.Count < maxObstacles)
                                {
                                    solution.Copy(previousState);
                                    solution.PutObstacle(position, costObstacle);
                                    solutions.Add(CloneState(stateCache, ref stateCacheCount, solution));
                                }

                                // Try to put slash Mirror /
                                if (previousState.Mirrors.Count < maxMirrors)
                                {
                                    solution.Copy(previousState);
                                    if (solution.PutMirror(position, BoardField.MirrorSlash, costMirror))
                                        solutions.Add(CloneState(stateCache, ref stateCacheCount, solution));
                                }

                                // Try to put backslash Mirror \
                                if (previousState.Mirrors.Count < maxMirrors)
                                {
                                    solution.Copy(previousState);
                                    if (solution.PutMirror(position, BoardField.MirrorBackSlash, costMirror))
                                        solutions.Add(CloneState(stateCache, ref stateCacheCount, solution));
                                }
                            }
                        }
                }
            }

            // Return previous solutions to the cache
            for (int i = 0; i < previousSolutions.Count; i++)
                stateCache[stateCacheCount++] = previousSolutions[i];
            previousSolutions.Clear();

            // Select only top N states, but unique
            int maxRayWidth = 2000;

            solutions.Sort((s1, s2) => s2.Score - s1.Score);

            int j = 1;
            for (int i = 1; j < maxRayWidth && i < solutions.Count; i++)
            {
                bool duplicate = false;

                for (int k = j - 1; k >= 0 && solutions[k].Score == solutions[i].Score && !duplicate; k--)
                    if (solutions[i].Same(solutions[k]))
                        duplicate = true;
                if (duplicate)
                    continue;
                if (j != i)
                {
                    var ttt = solutions[i];
                    solutions[i] = solutions[j];
                    solutions[j] = ttt;
                }
                j++;
            }

            // Remove duplicates and ones with low score
            for (int i = j; i < solutions.Count; i++)
                stateCache[stateCacheCount++] = solutions[i];
            if (solutions.Count > j)
                solutions.RemoveRange(j, solutions.Count - j);

            // Store best solution
            for (int i = 0; i < solutions.Count; i++)
                if (solutions[i].Score > bestSolution.Score)
                    bestSolution.Copy(solutions[i]);

            // Swap solutions and previous solutions
            var temp = solutions;
            solutions = previousSolutions;
            previousSolutions = temp;
        }
        Console.Error.WriteLine("{0}: {1} ({2}s)", steps, bestSolution.Score, sw.Elapsed.TotalSeconds);
        return bestSolution;
    }

    private static State CloneState(State[] stateCache, ref int stateCacheCount, State inputState)
    {
        State state;

        if (stateCacheCount > 0)
            state = stateCache[--stateCacheCount];
        else
        {
            int height = inputState.Board.GetLength(0);
            int width = inputState.Board.GetLength(1);

            state = new State(width, height);
        }
        state.Copy(inputState);
        return state;
    }
}
