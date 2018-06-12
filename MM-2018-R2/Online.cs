using System;
using System.Collections.Generic;
using System.Diagnostics;

#if LOCAL
[Flags]
#endif
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

#if LOCAL
[Flags]
#endif
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

static class LightExtensions
{
    public static Light GetLeftColor(this Light light)
    {
        return ((light & Light.BlueLeft) == Light.BlueLeft ? Light.Blue : Light.Empty)
            | ((light & Light.YellowLeft) == Light.YellowLeft ? Light.Yellow : Light.Empty)
            | ((light & Light.RedLeft) == Light.RedLeft ? Light.Red : Light.Empty);
    }

    public static Light GetRightColor(this Light light)
    {
        return ((light & Light.BlueRight) == Light.BlueRight ? Light.Blue : Light.Empty)
            | ((light & Light.YellowRight) == Light.YellowRight ? Light.Yellow : Light.Empty)
            | ((light & Light.RedRight) == Light.RedRight ? Light.Red : Light.Empty);
    }

    public static Light GetDownColor(this Light light)
    {
        return ((light & Light.BlueDown) == Light.BlueDown ? Light.Blue : Light.Empty)
            | ((light & Light.YellowDown) == Light.YellowDown ? Light.Yellow : Light.Empty)
            | ((light & Light.RedDown) == Light.RedDown ? Light.Red : Light.Empty);
    }

    public static Light GetUpColor(this Light light)
    {
        return ((light & Light.BlueUp) == Light.BlueUp ? Light.Blue : Light.Empty)
            | ((light & Light.YellowUp) == Light.YellowUp ? Light.Yellow : Light.Empty)
            | ((light & Light.RedUp) == Light.RedUp ? Light.Red : Light.Empty);
    }
}

struct Position
{
    public int X;
    public int Y;

    public Position(int x, int y)
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

    public int Hash
    {
        get
        {
            return (Position.X << 17) ^ (Position.Y << 9);
        }
    }

    public override string ToString()
    {
        return Position.ToString();
    }
}

struct Mirror
{
    public Position Position;
    public bool Slash;

    public int Hash
    {
        get
        {
            BoardField mirrorType = Slash ? BoardField.MirrorSlash : BoardField.MirrorBackSlash;

            return (Position.X << 11) ^ (Position.Y << 3) ^ ((int)mirrorType << 20);
        }
    }

    public override string ToString()
    {
        return string.Format("{0}: {1}", Position, Slash ? '/' : '\\');
    }
}

struct Lantern
{
    public Position Position;
    public BoardField Color;

    public int Hash
    {
        get
        {
            return ((int)Color << 16) ^ Position.X ^ (Position.Y << 8);
        }
    }

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
        Obstacles = new List<Obstacle>();
        Mirrors = new List<Mirror>();
        Lanterns = new List<Lantern>();
        Score = 0;
        Hash = 0;
    }

    public BoardField[,] Board;
    public Light[,] LightMap;
    public List<Obstacle> Obstacles;
    public List<Mirror> Mirrors;
    public List<Lantern> Lanterns;
    public int Score;
    public int PotentialScore;
    public int Hash;

    private static Position[] dummyHits = new Position[12];

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
        PotentialScore = other.PotentialScore;
        Hash = other.Hash;
    }

    public void ShallowCopy(State other)
    {
        Score = other.Score;
        PotentialScore = other.PotentialScore;
        Hash = other.Hash;
        Array.Copy(other.LightMap, LightMap, other.LightMap.Length);
    }

    public bool Same(State other)
    {
        if (Hash != other.Hash)
            return false;
        if (Mirrors.Count != other.Mirrors.Count)
            return false;
        if (Obstacles.Count != other.Obstacles.Count)
            return false;
        if (Lanterns.Count != other.Lanterns.Count)
            return false;
        if (Score != other.Score)
            return false;
        if (PotentialScore != other.PotentialScore)
            return false;
        for (int i = 0; i < Obstacles.Count; i++)
            if (other.Board[Obstacles[i].Position.Y, Obstacles[i].Position.X] != Board[Obstacles[i].Position.Y, Obstacles[i].Position.X])
                return false;
        for (int i = 0; i < Mirrors.Count; i++)
            if (other.Board[Mirrors[i].Position.Y, Mirrors[i].Position.X] != Board[Mirrors[i].Position.Y, Mirrors[i].Position.X])
                return false;
        for (int i = 0; i < Lanterns.Count; i++)
            if (other.Board[Lanterns[i].Position.Y, Lanterns[i].Position.X] != Board[Lanterns[i].Position.Y, Lanterns[i].Position.X])
                return false;
        return true;
    }

    public bool Equals(State other)
    {
        if (!Same(other))
            return false;
        int width = Width, height = Height;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (Board[y, x] != other.Board[y, x] || LightMap[y, x] != other.LightMap[y, x])
                    return false;
        return true;
    }

    public bool PutLantern(Position position, Light color, int cost, Position[] hits = null)
    {
        if ((LightMap[position.Y, position.X] & Light.ColorMask) != Light.Empty)
            return false;
        hits = hits ?? dummyHits;
        hits[0] = AddColorLeft(position.X - 1, position.Y, color);
        hits[1] = AddColorRight(position.X + 1, position.Y, color);
        hits[2] = AddColorUp(position.X, position.Y - 1, color);
        hits[3] = AddColorDown(position.X, position.Y + 1, color);
        Score -= cost;
        PotentialScore -= cost;
        Board[position.Y, position.X] |= BoardField.Lantern | (BoardField)color;
        Lantern lantern = new Lantern()
        {
            Color = (BoardField)color,
            Position = position,
        };
        Lanterns.Add(lantern);
        Hash = Hash ^ lantern.Hash;
        return true;
    }

    public void RemoveLantern(Position position, int cost, Position[] hits = null)
    {
        hits = hits ?? dummyHits;
        hits[0] = ClearColorLeft(position.X - 1, position.Y);
        hits[1] = ClearColorRight(position.X + 1, position.Y);
        hits[2] = ClearColorUp(position.X, position.Y - 1);
        hits[3] = ClearColorDown(position.X, position.Y + 1);
        Score += cost;
        PotentialScore += cost;
        Board[position.Y, position.X] = BoardField.Empty;
        int hash = 0;
        for (int i = Lanterns.Count - 1; i >= 0; i--)
        {
            Position lp = Lanterns[i].Position;

            if (lp.X == position.X && lp.Y == position.Y)
            {
                hash = Lanterns[i].Hash;
                Lanterns.RemoveAt(i);
                break;
            }
        }
        Hash = Hash ^ hash;
    }

    public void PutObstacle(Position position, int cost)
    {
        int hitsCount;

        PutObstacle(position, cost, dummyHits, out hitsCount);
    }

    public void PutObstacle(Position position, int cost, Position[] hits, out int hitsCount)
    {
        hitsCount = 0;
        ClearColor(position, hits, ref hitsCount);
        Score -= cost;
        PotentialScore -= cost;
        Obstacles.Add(new Obstacle()
        {
            Position = position,
        });
        Board[position.Y, position.X] |= BoardField.Obstacle;
        Hash = Hash ^ (position.X << 17) ^ (position.Y << 9);
    }

    public bool RemoveObstacle(Position position, int cost)
    {
        Light lightDirection = LightMap[position.Y, position.X];
        bool hasLeft = (lightDirection & Light.LeftMask) != Light.Empty;
        bool hasRight = (lightDirection & Light.RightMask) != Light.Empty;
        bool hasDown = (lightDirection & Light.DownMask) != Light.Empty;
        bool hasUp = (lightDirection & Light.UpMask) != Light.Empty;

        if (hasLeft && hasRight)
            return false;
        if (hasUp && hasDown)
            return false;

        if (hasLeft)
        {
            Light color = Light.Empty;

            if ((lightDirection & Light.BlueLeft) != Light.Empty)
                color |= Light.Blue;
            if ((lightDirection & Light.YellowLeft) != Light.Empty)
                color |= Light.Yellow;
            if ((lightDirection & Light.RedLeft) != Light.Empty)
                color |= Light.Red;
            AddColorLeft(position.X - 1, position.Y, color);
        }
        if (hasRight)
        {
            Light color = Light.Empty;

            if ((lightDirection & Light.BlueRight) != Light.Empty)
                color |= Light.Blue;
            if ((lightDirection & Light.YellowRight) != Light.Empty)
                color |= Light.Yellow;
            if ((lightDirection & Light.RedRight) != Light.Empty)
                color |= Light.Red;
            AddColorRight(position.X + 1, position.Y, color);
        }
        if (hasDown)
        {
            Light color = Light.Empty;

            if ((lightDirection & Light.BlueDown) != Light.Empty)
                color |= Light.Blue;
            if ((lightDirection & Light.YellowDown) != Light.Empty)
                color |= Light.Yellow;
            if ((lightDirection & Light.RedDown) != Light.Empty)
                color |= Light.Red;
            AddColorDown(position.X, position.Y + 1, color);
        }
        if (hasUp)
        {
            Light color = Light.Empty;

            if ((lightDirection & Light.BlueUp) != Light.Empty)
                color |= Light.Blue;
            if ((lightDirection & Light.YellowUp) != Light.Empty)
                color |= Light.Yellow;
            if ((lightDirection & Light.RedUp) != Light.Empty)
                color |= Light.Red;
            AddColorUp(position.X, position.Y - 1, color);
        }

        Score += cost;
        PotentialScore += cost;
        int hash = 0;
        for (int i = Obstacles.Count - 1; i >= 0; i--)
        {
            Position lp = Obstacles[i].Position;

            if (lp.X == position.X && lp.Y == position.Y)
            {
                hash = Obstacles[i].Hash;
                Obstacles.RemoveAt(i);
                break;
            }
        }
        Board[position.Y, position.X] = BoardField.Empty;
        Hash = Hash ^ hash;
        return true;
    }

    public bool IsPuttingMirrorSafe(Position position, BoardField mirrorType)
    {
        Light light = LightMap[position.Y, position.X];

        // Check if it will succeed
        Light up = light & Light.UpMask;
        Light down = light & Light.DownMask;
        Light left = light & Light.LeftMask;
        Light right = light & Light.RightMask;

        if (mirrorType == BoardField.MirrorSlash)
        {
            if (left != Light.Empty && up != Light.Empty)
                return false;
            if (right != Light.Empty && down != Light.Empty)
                return false;
        }
        else
        {
            if (left != Light.Empty && down != Light.Empty)
                return false;
            if (right != Light.Empty && up != Light.Empty)
                return false;
        }
        return true;
    }

    public bool PutMirror(Position position, BoardField mirrorType, int cost)
    {
        int hitsCount;

        return PutMirror(position, mirrorType, cost, dummyHits, out hitsCount);
    }

    public bool PutMirror(Position position, BoardField mirrorType, int cost, Position[] hits, out int hitsCount)
    {
        hitsCount = 0;
        if (!IsPuttingMirrorSafe(position, mirrorType))
            return false;
        PutMirrorUnsafe(position, mirrorType, cost, hits, out hitsCount);
        return true;
    }

    public void PutMirrorUnsafe(Position position, BoardField mirrorType, int cost)
    {
        int hitsCount;

        PutMirrorUnsafe(position, mirrorType, cost, dummyHits, out hitsCount);
    }

    public void PutMirrorUnsafe(Position position, BoardField mirrorType, int cost, Position[] hits, out int hitsCount)
    {
        hitsCount = 0;

        // Correct light path
        Light light = LightMap[position.Y, position.X];
        ClearColor(position, hits, ref hitsCount);
        if (mirrorType == BoardField.MirrorSlash)
        {
            if ((light & Light.LeftMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueLeft) == Light.BlueLeft ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowLeft) == Light.YellowLeft ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedLeft) == Light.RedLeft ? Light.Red : Light.Empty);

                hits[hitsCount++] = AddColorDown(position.X, position.Y + 1, color);
            }
            if ((light & Light.RightMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueRight) == Light.BlueRight ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowRight) == Light.YellowRight ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedRight) == Light.RedRight ? Light.Red : Light.Empty);

                hits[hitsCount++] = AddColorUp(position.X, position.Y - 1, color);
            }
            if ((light & Light.DownMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueDown) == Light.BlueDown ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowDown) == Light.YellowDown ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedDown) == Light.RedDown ? Light.Red : Light.Empty);

                hits[hitsCount++] = AddColorLeft(position.X - 1, position.Y, color);
            }
            if ((light & Light.UpMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueUp) == Light.BlueUp ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowUp) == Light.YellowUp ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedUp) == Light.RedUp ? Light.Red : Light.Empty);

                hits[hitsCount++] = AddColorRight(position.X + 1, position.Y, color);
            }
        }
        else
        {
            if ((light & Light.LeftMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueLeft) == Light.BlueLeft ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowLeft) == Light.YellowLeft ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedLeft) == Light.RedLeft ? Light.Red : Light.Empty);

                hits[hitsCount++] = AddColorUp(position.X, position.Y - 1, color);
            }
            if ((light & Light.RightMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueRight) == Light.BlueRight ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowRight) == Light.YellowRight ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedRight) == Light.RedRight ? Light.Red : Light.Empty);

                hits[hitsCount++] = AddColorDown(position.X, position.Y + 1, color);
            }
            if ((light & Light.DownMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueDown) == Light.BlueDown ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowDown) == Light.YellowDown ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedDown) == Light.RedDown ? Light.Red : Light.Empty);

                hits[hitsCount++] = AddColorRight(position.X + 1, position.Y, color);
            }
            if ((light & Light.UpMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueUp) == Light.BlueUp ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowUp) == Light.YellowUp ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedUp) == Light.RedUp ? Light.Red : Light.Empty);

                hits[hitsCount++] = AddColorLeft(position.X - 1, position.Y, color);
            }
        }
        Score -= cost;
        PotentialScore -= cost;
        Mirror mirror = new Mirror()
        {
            Position = position,
            Slash = mirrorType == BoardField.MirrorSlash,
        };
        Mirrors.Add(mirror);
        Board[position.Y, position.X] |= mirrorType;
        Hash = Hash ^ mirror.Hash;
    }

    public bool RemoveMirror(Position position, int cost)
    {
        int hitsCount;

        return RemoveMirror(position, cost, dummyHits, out hitsCount);
    }

    public bool RemoveMirror(Position position, int cost, Position[] hits, out int hitsCount)
    {
        Light light = LightMap[position.Y, position.X];
        bool hasLeft = (light & Light.LeftMask) != Light.Empty;
        bool hasRight = (light & Light.RightMask) != Light.Empty;
        bool hasDown = (light & Light.DownMask) != Light.Empty;
        bool hasUp = (light & Light.UpMask) != Light.Empty;

        hitsCount = 0;
        if (hasLeft && hasRight)
            return false;
        if (hasUp && hasDown)
            return false;

        if (Board[position.Y, position.X] == BoardField.MirrorSlash)
        {
            if (hasLeft)
                hits[hitsCount++] = ClearColorDown(position.X, position.Y + 1);
            if (hasRight)
                hits[hitsCount++] = ClearColorUp(position.X, position.Y - 1);
            if (hasDown)
                hits[hitsCount++] = ClearColorLeft(position.X - 1, position.Y);
            if (hasUp)
                hits[hitsCount++] = ClearColorRight(position.X + 1, position.Y);
        }
        else
        {
            if (hasLeft)
                hits[hitsCount++] = ClearColorUp(position.X, position.Y - 1);
            if (hasRight)
                hits[hitsCount++] = ClearColorDown(position.X, position.Y + 1);
            if (hasDown)
                hits[hitsCount++] = ClearColorRight(position.X + 1, position.Y);
            if (hasUp)
                hits[hitsCount++] = ClearColorLeft(position.X - 1, position.Y);
        }
        if (hasLeft)
        {
            Light color = ((light & Light.BlueLeft) == Light.BlueLeft ? Light.Blue : Light.Empty)
                | ((light & Light.YellowLeft) == Light.YellowLeft ? Light.Yellow : Light.Empty)
                | ((light & Light.RedLeft) == Light.RedLeft ? Light.Red : Light.Empty);

            hits[hitsCount++] = AddColorLeft(position.X - 1, position.Y, color);
        }
        if (hasRight)
        {
            Light color = ((light & Light.BlueRight) == Light.BlueRight ? Light.Blue : Light.Empty)
                | ((light & Light.YellowRight) == Light.YellowRight ? Light.Yellow : Light.Empty)
                | ((light & Light.RedRight) == Light.RedRight ? Light.Red : Light.Empty);

            hits[hitsCount++] = AddColorRight(position.X + 1, position.Y, color);
        }
        if (hasDown)
        {
            Light color = ((light & Light.BlueDown) == Light.BlueDown ? Light.Blue : Light.Empty)
                | ((light & Light.YellowDown) == Light.YellowDown ? Light.Yellow : Light.Empty)
                | ((light & Light.RedDown) == Light.RedDown ? Light.Red : Light.Empty);

            hits[hitsCount++] = AddColorDown(position.X, position.Y + 1, color);
        }
        if (hasUp)
        {
            Light color = ((light & Light.BlueUp) == Light.BlueUp ? Light.Blue : Light.Empty)
                | ((light & Light.YellowUp) == Light.YellowUp ? Light.Yellow : Light.Empty)
                | ((light & Light.RedUp) == Light.RedUp ? Light.Red : Light.Empty);

            hits[hitsCount++] = AddColorUp(position.X, position.Y - 1, color);
        }
        Score += cost;
        PotentialScore += cost;
        int hash = 0;
        for (int i = Mirrors.Count - 1; i >= 0; i--)
        {
            Position lp = Mirrors[i].Position;

            if (lp.X == position.X && lp.Y == position.Y)
            {
                hash = Mirrors[i].Hash;
                Mirrors.RemoveAt(i);
                break;
            }
        }
        Board[position.Y, position.X] = BoardField.Empty;
        Hash = Hash ^ hash;
        return true;
    }

    public void BacktraceCrystal(int x, int y)
    {
        BoardField field = Board[y, x];
        Light color = (Light)(field & BoardField.ColorMask);

        BacktraceCrystal(x, y, color);
    }

    public void BacktraceCrystal(int x, int y, Light color)
    {
        AddColorDown(x, y + 1, color);
        AddColorUp(x, y - 1, color);
        AddColorLeft(x - 1, y, color);
        AddColorRight(x + 1, y, color);
    }

    public void UndoBacktraceCrystal(int x, int y)
    {
        ClearColorDown(x, y + 1);
        ClearColorUp(x, y - 1);
        ClearColorLeft(x - 1, y);
        ClearColorRight(x + 1, y);
    }

    public bool HasColor(Position position)
    {
        return (LightMap[position.Y, position.X] & Light.ColorMask) != Light.Empty;
    }

    public bool HasObject(Position position)
    {
        return (Board[position.Y, position.X] & BoardField.ObjectMask) != BoardField.Empty;
    }

    private void ClearColor(Position position, Position[] hits, ref int hitsCount)
    {
        Light lightDirection = LightMap[position.Y, position.X];

        if ((lightDirection & Light.LeftMask) != Light.Empty)
            hits[hitsCount++] = ClearColorLeft(position.X - 1, position.Y);
        if ((lightDirection & Light.RightMask) != Light.Empty)
            hits[hitsCount++] = ClearColorRight(position.X + 1, position.Y);
        if ((lightDirection & Light.DownMask) != Light.Empty)
            hits[hitsCount++] = ClearColorDown(position.X, position.Y + 1);
        if ((lightDirection & Light.UpMask) != Light.Empty)
            hits[hitsCount++] = ClearColorUp(position.X, position.Y - 1);
    }

    private Position ClearColorDown(int x, int y)
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
                return ClearColorLeft(x - 1, y);
            if ((field & BoardField.MirrorBackSlash) == BoardField.MirrorBackSlash)
                return ClearColorRight(x + 1, y);

            // If we hit crystal, update score
            if ((field & BoardField.Crystal) == BoardField.Crystal)
                UpdateCrystal(x, y, originalLight);

            // Stop if we hit an object
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
                break;
            y++;
        }
        return new Position(-x, -y);
    }

    private Position ClearColorUp(int x, int y)
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
                return ClearColorRight(x + 1, y);
            if ((field & BoardField.MirrorBackSlash) == BoardField.MirrorBackSlash)
                return ClearColorLeft(x - 1, y);

            // If we hit crystal, update score
            if ((field & BoardField.Crystal) == BoardField.Crystal)
                UpdateCrystal(x, y, originalLight);

            // Stop if we hit an object
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
                break;
            y--;
        }
        return new Position(-x, -y);
    }

    private Position ClearColorLeft(int x, int y)
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
                return ClearColorDown(x, y + 1);
            if ((field & BoardField.MirrorBackSlash) == BoardField.MirrorBackSlash)
                return ClearColorUp(x, y - 1);

            // If we hit crystal, update score
            if ((field & BoardField.Crystal) == BoardField.Crystal)
                UpdateCrystal(x, y, originalLight);

            // Stop if we hit an object
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
                break;
            x--;
        }
        return new Position(-x, -y);
    }

    private Position ClearColorRight(int x, int y)
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
                return ClearColorUp(x, y - 1);
            if ((field & BoardField.MirrorBackSlash) == BoardField.MirrorBackSlash)
                return ClearColorDown(x, y + 1);

            // If we hit crystal, update score
            if ((field & BoardField.Crystal) == BoardField.Crystal)
                UpdateCrystal(x, y, originalLight);

            // Stop if we hit an object
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
                break;
            x++;
        }
        return new Position(-x, -y);
    }

    private Position AddColorDown(int x, int y, Light color)
    {
        int height = Height;
        Light direction = Light.Empty;

        if ((color & Light.Blue) == Light.Blue)
            direction |= Light.BlueDown;
        if ((color & Light.Red) == Light.Red)
            direction |= Light.RedDown;
        if ((color & Light.Yellow) == Light.Yellow)
            direction |= Light.YellowDown;
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
                return new Position(x, y);

            // Stop if we hit an object
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
                break;
            y++;
        }
        return new Position(x, y);
    }

    private Position AddColorUp(int x, int y, Light color)
    {
        Light direction = Light.Empty;

        if ((color & Light.Blue) == Light.Blue)
            direction |= Light.BlueUp;
        if ((color & Light.Red) == Light.Red)
            direction |= Light.RedUp;
        if ((color & Light.Yellow) == Light.Yellow)
            direction |= Light.YellowUp;
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
                return new Position(x, y);

            // Stop if we hit an object
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
                break;
            y--;
        }
        return new Position(x, y);
    }

    private Position AddColorRight(int x, int y, Light color)
    {
        int width = Width;
        Light direction = Light.Empty;

        if ((color & Light.Blue) == Light.Blue)
            direction |= Light.BlueRight;
        if ((color & Light.Red) == Light.Red)
            direction |= Light.RedRight;
        if ((color & Light.Yellow) == Light.Yellow)
            direction |= Light.YellowRight;
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
                return new Position(x, y);

            // Stop if we hit an object
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
                break;
            x++;
        }
        return new Position(x, y);
    }

    private Position AddColorLeft(int x, int y, Light color)
    {
        Light direction = Light.Empty;

        if ((color & Light.Blue) == Light.Blue)
            direction |= Light.BlueLeft;
        if ((color & Light.Red) == Light.Red)
            direction |= Light.RedLeft;
        if ((color & Light.Yellow) == Light.Yellow)
            direction |= Light.YellowLeft;
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
                return new Position(x, y);

            // Stop if we hit an object
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
                break;
            x--;
        }
        return new Position(x, y);
    }

    private void UpdateCrystal(int x, int y, Light originalLight)
    {
        Light previousColor = originalLight & Light.ColorMask;
        Light color = (Light)(Board[y, x] & BoardField.ColorMask);
        Light newColor = LightMap[y, x] & Light.ColorMask;

        if (previousColor != newColor)
        {
            int previousScore = GetCrystalScore(color, previousColor);
            int newScore = GetCrystalScore(color, newColor);

            Score += newScore - previousScore;

            int previousPotentialScore = GetCrystalPotentialScore(color, previousColor);
            int newPotentialScore = GetCrystalPotentialScore(color, newColor);

            PotentialScore += newPotentialScore - previousPotentialScore;
        }
    }

    internal static int GetCrystalScoreDiff(Light previousColor, Light color, Light newColor)
    {
        int previousScore = GetCrystalScore(color, previousColor);
        int newScore = GetCrystalScore(color, newColor);

        return newScore - previousScore;
    }

    internal static int GetCrystalPotentialScoreDiff(Light previousColor, Light color, Light newColor)
    {
        int previousPotentialScore = GetCrystalPotentialScore(color, previousColor);
        int newPotentialScore = GetCrystalPotentialScore(color, newColor);

        return newPotentialScore - previousPotentialScore;
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

    private static int GetCrystalPotentialScore(Light crystalColor, Light lightColor)
    {
        if (lightColor == Light.Empty)
            return 0;

        if ((crystalColor & lightColor) != lightColor)
            return -10;

        if (crystalColor == lightColor)
        {
            if (crystalColor == Light.Blue || crystalColor == Light.Red || crystalColor == Light.Yellow)
                return 20;
            return 30;
        }
        return 5;
    }
}

struct ScoreTuple
{
    public int Score;
    public int PotentialScore;
}

struct ExtendedState
{
    public State State;
    public State CrystalLights;
    public State UnlitedCrystalLights;
    internal Position[] HitsCache;
    internal State InputState;

    public ExtendedState(State inputState)
        : this()
    {
        int width = inputState.Width;
        int height = inputState.Height;

        InputState = CloneState(inputState);
        State = CloneState(inputState);
        CrystalLights = CloneState(inputState);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if ((CrystalLights.Board[y, x] & BoardField.Crystal) == BoardField.Crystal)
                    CrystalLights.BacktraceCrystal(x, y);
        UnlitedCrystalLights = CloneState(CrystalLights);
        HitsCache = new Position[12];
    }

    public ScoreTuple GetLanternScore(Position position, Light light, int cost)
    {
        State.PutLantern(position, light, cost);
        ScoreTuple originalScore = new ScoreTuple()
        {
            Score = State.Score,
            PotentialScore = State.PotentialScore,
        };
        State.RemoveLantern(position, cost);

        Light crystalLights = CrystalLights.LightMap[position.Y, position.X];
        Light unlitedCrystalLights = UnlitedCrystalLights.LightMap[position.Y, position.X];
        Light leftCrystal = crystalLights.GetLeftColor();
        Light rightCrystal = crystalLights.GetRightColor();
        Light downCrystal = crystalLights.GetDownColor();
        Light upCrystal = crystalLights.GetUpColor();
        ScoreTuple score = new ScoreTuple()
        {
            Score = State.Score,
            PotentialScore = State.PotentialScore,
        };
        score.Score -= cost;
        score.PotentialScore -= cost;

        if (leftCrystal != Light.Empty)
        {
            Light crystal = leftCrystal;
            Light unlitedCrystal = unlitedCrystalLights.GetLeftColor();
            Light previousColor = crystal ^ unlitedCrystal;
            Light color = crystal;
            Light newColor = previousColor | light;

            score.Score += State.GetCrystalScoreDiff(previousColor, color, newColor);
            score.PotentialScore += State.GetCrystalPotentialScoreDiff(previousColor, color, newColor);
        }
        if (rightCrystal != Light.Empty)
        {
            Light crystal = rightCrystal;
            Light unlitedCrystal = unlitedCrystalLights.GetRightColor();
            Light previousColor = crystal ^ unlitedCrystal;
            Light color = crystal;
            Light newColor = previousColor | light;

            score.Score += State.GetCrystalScoreDiff(previousColor, color, newColor);
            score.PotentialScore += State.GetCrystalPotentialScoreDiff(previousColor, color, newColor);
        }
        if (downCrystal != Light.Empty)
        {
            Light crystal = downCrystal;
            Light unlitedCrystal = unlitedCrystalLights.GetDownColor();
            Light previousColor = crystal ^ unlitedCrystal;
            Light color = crystal;
            Light newColor = previousColor | light;

            score.Score += State.GetCrystalScoreDiff(previousColor, color, newColor);
            score.PotentialScore += State.GetCrystalPotentialScoreDiff(previousColor, color, newColor);
        }
        if (upCrystal != Light.Empty)
        {
            Light crystal = upCrystal;
            Light unlitedCrystal = unlitedCrystalLights.GetUpColor();
            Light previousColor = crystal ^ unlitedCrystal;
            Light color = crystal;
            Light newColor = previousColor | light;

            score.Score += State.GetCrystalScoreDiff(previousColor, color, newColor);
            score.PotentialScore += State.GetCrystalPotentialScoreDiff(previousColor, color, newColor);
        }

        if (score.Score != originalScore.Score || score.PotentialScore != originalScore.PotentialScore)
            throw new Exception("ASDFASFDA");

        return score;
    }

    public bool PutLantern(Position position, Light light, int cost)
    {
        int hitsCount = 4;
        Position[] hits = HitsCache;

        if (!State.PutLantern(position, light, cost, hits))
            return false;
        for (int i = 0; i < hitsCount; i++)
            if (hits[i].X >= 0 && hits[i].Y >= 0 && hits[i].Y < State.Height && hits[i].X < State.Width)
            {
                BoardField field = State.Board[hits[i].Y, hits[i].X];

                if ((field & BoardField.Crystal) == BoardField.Crystal)
                {
                    Light crystalColor = (Light)(State.Board[hits[i].Y, hits[i].X] & BoardField.ColorMask);
                    Light crystalLight = State.LightMap[hits[i].Y, hits[i].X] & Light.ColorMask;

                    UnlitedCrystalLights.UndoBacktraceCrystal(hits[i].X, hits[i].Y);
                    UnlitedCrystalLights.BacktraceCrystal(hits[i].X, hits[i].Y, crystalColor ^ crystalLight);
                }
            }
        return true;
    }

    public ScoreTuple GetObstacleScore(Position position, int cost)
    {
        // TODO: Speed this up
        ScoreTuple score = new ScoreTuple()
        {
            Score = State.Score,
            PotentialScore = State.Score,
        };
        State.PutObstacle(position, cost);
        score.Score = State.Score;
        score.PotentialScore = State.PotentialScore;
        State.RemoveObstacle(position, cost);
        return score;
    }

    public void PutObstacle(Position position, int cost)
    {
        int hitsCount;
        Position[] hits = HitsCache;

        State.PutObstacle(position, cost, hits, out hitsCount);
        CrystalLights.PutObstacle(position, cost);
        UnlitedCrystalLights.PutObstacle(position, cost);
        for (int i = 0; i < hitsCount; i++)
        {
            Position hit = hits[i];

            if (hit.X < 0 && hit.Y < 0)
            {
                hit.X = -hit.X;
                hit.Y = -hit.Y;
            }
            if (hit.X >= 0 && hit.Y >= 0 && hit.Y < State.Height && hit.X < State.Width)
            {
                BoardField field = State.Board[hit.Y, hit.X];

                if ((field & BoardField.Crystal) == BoardField.Crystal)
                {
                    Light crystalColor = (Light)(State.Board[hit.Y, hit.X] & BoardField.ColorMask);
                    Light crystalLight = State.LightMap[hit.Y, hit.X] & Light.ColorMask;

                    UnlitedCrystalLights.UndoBacktraceCrystal(hit.X, hit.Y);
                    UnlitedCrystalLights.BacktraceCrystal(hit.X, hit.Y, crystalColor ^ crystalLight);
                }
            }
        }
    }

    public ScoreTuple GetMirrorScore(Position position, bool slash, int cost)
    {
        // TODO: Speed this up
        ScoreTuple score = new ScoreTuple()
        {
            Score = State.Score,
            PotentialScore = State.Score,
        };
        State.PutMirror(position, slash ? BoardField.MirrorSlash : BoardField.MirrorBackSlash, cost);
        score.Score = State.Score;
        score.PotentialScore = State.PotentialScore;
        State.RemoveMirror(position, cost);
        return score;
    }

    public bool PutMirror(Position position, BoardField mirrorType, int cost)
    {
        int hitsCount;
        Position[] hits = HitsCache;

        if (!State.PutMirror(position, mirrorType, cost, hits, out hitsCount))
            return false;
        CrystalLights.PutMirrorUnsafe(position, mirrorType, cost);
        UnlitedCrystalLights.PutMirrorUnsafe(position, mirrorType, cost);
        for (int i = 0; i < hitsCount; i++)
        {
            Position hit = hits[i];

            if (hit.X < 0 && hit.Y < 0)
            {
                hit.X = -hit.X;
                hit.Y = -hit.Y;
            }
            if (hit.X >= 0 && hit.Y >= 0 && hit.Y < State.Height && hit.X < State.Width)
            {
                BoardField field = State.Board[hit.Y, hit.X];

                if ((field & BoardField.Crystal) == BoardField.Crystal)
                {
                    Light crystalColor = (Light)(State.Board[hit.Y, hit.X] & BoardField.ColorMask);
                    Light crystalLight = State.LightMap[hit.Y, hit.X] & Light.ColorMask;
                    Light newColor = crystalColor ^ crystalLight;

                    UnlitedCrystalLights.UndoBacktraceCrystal(hit.X, hit.Y);
                    if (newColor != Light.Empty)
                        UnlitedCrystalLights.BacktraceCrystal(hit.X, hit.Y, newColor);
                }
            }
        }
        return true;
    }

    private static State CloneState(State inputState)
    {
        int height = inputState.Board.GetLength(0);
        int width = inputState.Board.GetLength(1);
        State state = new State(width, height);

        state.Copy(inputState);
        return state;
    }
}

public class CrystalLighting
{
    private Stopwatch sw;
#if LOCAL
    private TimeSpan maxTime = Debugger.IsAttached ? TimeSpan.FromSeconds(99999.5) : TimeSpan.FromSeconds(200.5);
#else
    private TimeSpan maxTime = TimeSpan.FromSeconds(9.5);
#endif

    public string[] placeItems(string[] targetBoard, int costLantern, int costMirror, int costObstacle, int maxMirrors, int maxObstacles)
    {
        sw = Stopwatch.StartNew();
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

        State solution = SolveGreedy(inputState, costLantern, costMirror, costObstacle, maxMirrors, maxObstacles);

        for (maxRayWidth = 5; sw.Elapsed < maxTime; maxRayWidth *= 5)
        {
            State s = Solve(inputState, costLantern, costMirror, costObstacle, maxMirrors, maxObstacles);
            if (s.Score > solution.Score)
                solution = s;
        }
        List<string> result = new List<string>();

        foreach (var obstacle in solution.Obstacles)
            result.Add(string.Format("{0} {1} X", obstacle.Position.Y, obstacle.Position.X));
        foreach (var mirror in solution.Mirrors)
            result.Add(string.Format("{0} {1} {2}", mirror.Position.Y, mirror.Position.X, mirror.Slash ? '/' : '\\'));
        foreach (var lantern in solution.Lanterns)
            result.Add(string.Format("{0} {1} {2}", lantern.Position.Y, lantern.Position.X, (int)lantern.Color));
        return result.ToArray();
    }

    int maxRayWidth = 1;

    private State Solve(State inputState, int costLantern, int costMirror, int costObstacle, int maxMirrors, int maxObstacles)
    {
        State[] stateCache = new State[1000000];
        int stateCacheCount = 0;
        State[] solutions = new State[maxRayWidth];
        int solutionsCount = 0;
        State[] previousSolutions = new State[maxRayWidth];
        int previousSolutionsCount = 0;
        State solution = CloneState(stateCache, ref stateCacheCount, inputState);
        State bestSolution = CloneState(stateCache, ref stateCacheCount, inputState);
        int height = inputState.Height;
        int width = inputState.Width;
        Position position = new Position();
        int steps = 0;

        previousSolutions[previousSolutionsCount++] = CloneState(stateCache, ref stateCacheCount, inputState);
        while (sw.Elapsed < maxTime && previousSolutionsCount > 0)
        {
#if LOCAL
            Console.Error.WriteLine("{0}. {1}   {2}s   ({3} {4} {5})", steps, bestSolution.Score, sw.Elapsed.TotalSeconds, bestSolution.Lanterns.Count, bestSolution.Mirrors.Count, bestSolution.Obstacles.Count);
#endif
            steps++;
            for (int pi = 0; pi < previousSolutionsCount; pi++)
            {
                State previousState = previousSolutions[pi];
                solution.Copy(previousState);
                if (sw.Elapsed > maxTime)
                    break;
                for (position.Y = 0; position.Y < height; position.Y++)
                {
                    if (sw.Elapsed > maxTime)
                        break;
                    for (position.X = 0; position.X < width; position.X++)
                        if (!solution.HasObject(position))
                        {
                            if (!solution.HasColor(position))
                            {
                                // Try to put Blue lantern
                                solution.PutLantern(position, Light.Blue, costLantern);
                                AddSolution(solutions, ref solutionsCount, solution, stateCache, ref stateCacheCount);
                                solution.RemoveLantern(position, costLantern);

                                // Try to put Yellow lantern
                                solution.PutLantern(position, Light.Yellow, costLantern);
                                AddSolution(solutions, ref solutionsCount, solution, stateCache, ref stateCacheCount);
                                solution.RemoveLantern(position, costLantern);

                                // Try to put Red lantern
                                solution.PutLantern(position, Light.Red, costLantern);
                                AddSolution(solutions, ref solutionsCount, solution, stateCache, ref stateCacheCount);
                                solution.RemoveLantern(position, costLantern);
                            }
                            else
                            {
                                // Try to put Obstacle
                                if (solution.Obstacles.Count < maxObstacles)
                                {
                                    solution.PutObstacle(position, costObstacle);
                                    AddSolution(solutions, ref solutionsCount, solution, stateCache, ref stateCacheCount);
                                    solution.RemoveObstacle(position, costObstacle);
                                }

                                // Try to put slash Mirror /
                                if (solution.Mirrors.Count < maxMirrors)
                                {
                                    if (solution.PutMirror(position, BoardField.MirrorSlash, costMirror))
                                    {
                                        AddSolution(solutions, ref solutionsCount, solution, stateCache, ref stateCacheCount);
                                        solution.RemoveMirror(position, costMirror);
                                    }
                                }

                                // Try to put backslash Mirror \
                                if (solution.Mirrors.Count < maxMirrors)
                                {
                                    if (solution.PutMirror(position, BoardField.MirrorBackSlash, costMirror))
                                    {
                                        AddSolution(solutions, ref solutionsCount, solution, stateCache, ref stateCacheCount);
                                        solution.RemoveMirror(position, costMirror);
                                    }
                                }
                            }
                        }
                }
            }

            // Return previous solutions to the cache
            for (int i = 0; i < previousSolutionsCount; i++)
                stateCache[stateCacheCount++] = previousSolutions[i];
            previousSolutionsCount = 0;

            // Store best solution
            for (int i = 0; i < solutionsCount; i++)
                if (solutions[i].Score > bestSolution.Score)
                    bestSolution.Copy(solutions[i]);

            // Swap solutions and previous solutions
            var temp = solutions;
            solutions = previousSolutions;
            previousSolutions = temp;
            previousSolutionsCount = solutionsCount;
            solutionsCount = 0;
        }
#if LOCAL
        Console.Error.WriteLine("{0}: {1} ({2}s)", steps, bestSolution.Score, sw.Elapsed.TotalSeconds);
#endif
        return bestSolution;
    }

    enum MoveType
    {
        Lantern,
        Obstacle,
        Mirror,
    }

    struct Move
    {
        public ExtendedState State; // TODO: Consider adding ref-counting
        public MoveType Type;
        public Lantern Lantern;
        public Obstacle Obstacle;
        public Mirror Mirror;
        public int Score;
        public int PotentialScore;
        public int Hash;

        public Move(ExtendedState state, Lantern lantern, int cost)
            : this()
        {
            State = state;
            Type = MoveType.Lantern;
            Lantern = lantern;
            Hash = state.State.Hash ^ lantern.Hash;
            var score = state.GetLanternScore(lantern.Position, (Light)lantern.Color, cost);
            Score = score.Score;
            PotentialScore = score.PotentialScore;
        }

        public Move(ExtendedState state, Obstacle obstacle, int cost)
            : this()
        {
            State = state;
            Type = MoveType.Obstacle;
            Obstacle = obstacle;
            Hash = state.State.Hash ^ obstacle.Hash;
            var score = state.GetObstacleScore(obstacle.Position, cost);
            Score = score.Score;
            PotentialScore = score.PotentialScore;
        }

        public Move(ExtendedState state, Mirror mirror, int cost)
            : this()
        {
            State = state;
            Type = MoveType.Mirror;
            Mirror = mirror;
            Hash = state.State.Hash ^ mirror.Hash;
            var score = state.GetMirrorScore(mirror.Position, mirror.Slash, cost);
            Score = score.Score;
            PotentialScore = score.PotentialScore;
        }

        public ExtendedState Apply(int costLantern, int costObstacle, int costMirror)
        {
            ExtendedState result = new ExtendedState()
            {
                CrystalLights = CloneState(State.CrystalLights),
                State = CloneState(State.State),
                UnlitedCrystalLights = CloneState(State.UnlitedCrystalLights),
                HitsCache = new Position[State.HitsCache.Length],
                InputState = State.InputState,
            };

            switch (Type)
            {
                case MoveType.Lantern:
                    result.PutLantern(Lantern.Position, (Light)Lantern.Color, costLantern);
                    break;
                case MoveType.Mirror:
                    result.PutMirror(Mirror.Position, Mirror.Slash ? BoardField.MirrorSlash : BoardField.MirrorBackSlash, costMirror);
                    break;
                case MoveType.Obstacle:
                    result.PutObstacle(Obstacle.Position, costObstacle);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return result;
        }

        public bool Same(Move other)
        {
            // TODO: Implement Same for State, but it needs to include this action.
            if (Hash != other.Hash)
                return false;
            if (Score != other.Score)
                return false;
            if (PotentialScore != other.PotentialScore)
                return false;
            int mirrorsCount = State.State.Mirrors.Count + (Type == MoveType.Mirror ? 1 : 0);
            int otherMirrorsCount = other.State.State.Mirrors.Count + (other.Type == MoveType.Mirror ? 1 : 0);
            if (mirrorsCount != otherMirrorsCount)
                return false;
            int obstaclesCount = State.State.Obstacles.Count + (Type == MoveType.Obstacle ? 1 : 0);
            int otherObstaclesCount = other.State.State.Obstacles.Count + (other.Type == MoveType.Obstacle ? 1 : 0);
            if (obstaclesCount != otherObstaclesCount)
                return false;
            int lanternsCount = State.State.Lanterns.Count + (Type == MoveType.Lantern ? 1 : 0);
            int otherLanternsCount = other.State.State.Lanterns.Count + (other.Type == MoveType.Lantern ? 1 : 0);
            if (lanternsCount != otherLanternsCount)
                return false;
            for (int i = 0; i < State.State.Obstacles.Count; i++)
                if (other.State.State.Board[State.State.Obstacles[i].Position.Y, State.State.Obstacles[i].Position.X] != State.State.Board[State.State.Obstacles[i].Position.Y, State.State.Obstacles[i].Position.X])
                {
                    if (other.Type == MoveType.Obstacle)
                    {
                        var obstacle = State.State.Obstacles[i];
                        var otherObstacle = other.Obstacle;

                        if (obstacle.Position.X == otherObstacle.Position.X && obstacle.Position.Y == otherObstacle.Position.Y)
                            continue;
                    }
                    return false;
                }
            if (Type == MoveType.Obstacle && other.State.State.Board[Obstacle.Position.Y, Obstacle.Position.X] != BoardField.Obstacle)
                return false;
            for (int i = 0; i < State.State.Mirrors.Count; i++)
                if (other.State.State.Board[State.State.Mirrors[i].Position.Y, State.State.Mirrors[i].Position.X] != State.State.Board[State.State.Mirrors[i].Position.Y, State.State.Mirrors[i].Position.X])
                {
                    if (other.Type == MoveType.Mirror)
                    {
                        var mirror = State.State.Mirrors[i];
                        var otherMirror = other.Mirror;

                        if (mirror.Position.X == otherMirror.Position.X && mirror.Position.Y == otherMirror.Position.Y && mirror.Slash == otherMirror.Slash)
                            continue;
                    }
                    return false;
                }
            if (Type == MoveType.Mirror && other.State.State.Board[Mirror.Position.Y, Mirror.Position.X] != (Mirror.Slash ? BoardField.MirrorSlash : BoardField.MirrorBackSlash))
                return false;
            for (int i = 0; i < State.State.Lanterns.Count; i++)
                if (other.State.State.Board[State.State.Lanterns[i].Position.Y, State.State.Lanterns[i].Position.X] != State.State.Board[State.State.Lanterns[i].Position.Y, State.State.Lanterns[i].Position.X])
                {
                    if (other.Type == MoveType.Lantern)
                    {
                        var lantern = State.State.Lanterns[i];
                        var otherLantern = other.Lantern;

                        if (lantern.Position.X == otherLantern.Position.X && lantern.Position.Y == otherLantern.Position.Y && lantern.Color == otherLantern.Color)
                            continue;
                    }
                    return false;
                }
            if (Type == MoveType.Lantern && other.State.State.Board[Lantern.Position.Y, Lantern.Position.X] != (BoardField.Lantern | Lantern.Color))
                return false;
            return true;
        }
    }

    private State Solve2(State inputState, int costLantern, int costMirror, int costObstacle, int maxMirrors, int maxObstacles)
    {
        Move[] moves = new Move[maxRayWidth];
        int movesCount = 0;
        ExtendedState[] previousStates = new ExtendedState[maxRayWidth];
        int previousStatesCount = 0;
        State bestSolution = CloneState(inputState);
        int height = inputState.Height;
        int width = inputState.Width;
        Position position = new Position();
        int steps = 0;

        previousStates[previousStatesCount++] = new ExtendedState(inputState);
        while (sw.Elapsed < maxTime && previousStatesCount > 0)
        {
#if LOCAL
            Console.Error.WriteLine("{0}. {1}   {2}s   ({3} {4} {5})", steps, bestSolution.Score, sw.Elapsed.TotalSeconds, bestSolution.Lanterns.Count, bestSolution.Mirrors.Count, bestSolution.Obstacles.Count);
#endif
            steps++;
            for (int pi = 0; pi < previousStatesCount; pi++)
            {
                ExtendedState previousState = previousStates[pi];

                if (sw.Elapsed > maxTime)
                    break;
                for (position.Y = 0; position.Y < height; position.Y++)
                {
                    if (sw.Elapsed > maxTime)
                        break;
                    for (position.X = 0; position.X < width; position.X++)
                        if (!previousState.State.HasObject(position))
                        {
                            if (!previousState.State.HasColor(position))
                            {
                                Light light = previousState.CrystalLights.LightMap[position.Y, position.X];
                                Light color = light & Light.ColorMask;

                                if (color == Light.Empty)
                                    continue;

                                // Try to put Blue lantern
                                if ((color & Light.Blue) == Light.Blue)
                                {
                                    Move move = new Move(previousState, new Lantern() { Position = position, Color = BoardField.Blue }, costLantern);

                                    AddSolution(moves, ref movesCount, move);
                                }

                                // Try to put Yellow lantern
                                if ((color & Light.Yellow) == Light.Yellow)
                                {
                                    Move move = new Move(previousState, new Lantern() { Position = position, Color = BoardField.Yellow }, costLantern);

                                    AddSolution(moves, ref movesCount, move);
                                }

                                // Try to put Red lantern
                                if ((color & Light.Red) == Light.Red)
                                {
                                    Move move = new Move(previousState, new Lantern() { Position = position, Color = BoardField.Red }, costLantern);

                                    AddSolution(moves, ref movesCount, move);
                                }
                            }
                            else
                            {
                                // Try to put Obstacle
                                if (previousState.State.Obstacles.Count < maxObstacles)
                                {
                                    Move move = new Move(previousState, new Obstacle() { Position = position }, costObstacle);

                                    AddSolution(moves, ref movesCount, move);
                                }

                                // Try to put slash Mirror /
                                if (previousState.State.Mirrors.Count < maxMirrors)
                                {
                                    if (previousState.State.IsPuttingMirrorSafe(position, BoardField.MirrorSlash))
                                    {
                                        Move move = new Move(previousState, new Mirror() { Position = position, Slash = true }, costObstacle);

                                        AddSolution(moves, ref movesCount, move);
                                    }
                                }

                                // Try to put backslash Mirror \
                                if (previousState.State.Mirrors.Count < maxMirrors)
                                {
                                    if (previousState.State.IsPuttingMirrorSafe(position, BoardField.MirrorBackSlash))
                                    {
                                        Move move = new Move(previousState, new Mirror() { Position = position, Slash = false }, costObstacle);

                                        AddSolution(moves, ref movesCount, move);
                                    }
                                }
                            }
                        }
                }
            }

            // Convert moves to previous states
            for (int i = 0; i < movesCount; i++)
                previousStates[i] = moves[i].Apply(costLantern, costObstacle, costMirror);
            previousStatesCount = movesCount;
            movesCount = 0;

            // Store best solution
            for (int i = 0; i < previousStatesCount; i++)
                if (previousStates[i].State.Score > bestSolution.Score)
                    bestSolution.Copy(previousStates[i].State);
        }
#if LOCAL
        Console.Error.WriteLine("{0}: {1} ({2}s)", steps, bestSolution.Score, sw.Elapsed.TotalSeconds);
#endif
        return bestSolution;
    }

    private class MoveCostComparerClass : IComparer<Move>
    {
        public int Compare(Move x, Move y)
        {
            return y.Score - x.Score;
        }
    }

    static MoveCostComparerClass MoveCostComparer = new MoveCostComparerClass();

    private class MovePotentialCostComparerClass : IComparer<Move>
    {
        public int Compare(Move x, Move y)
        {
            return y.PotentialScore - x.PotentialScore;
        }
    }

    static MovePotentialCostComparerClass MovePotentialCostComparer = new MovePotentialCostComparerClass();

    private void AddSolution(Move[] moves, ref int movesCount, Move move)
    {
        if (movesCount < moves.Length)
        {
            if (movesCount > 0)
            {
                int index = Array.BinarySearch(moves, 0, movesCount, move, MovePotentialCostComparer);

                if (index < 0)
                {
                    index = ~index;
                }
                else
                {
                    while (index + 1 < movesCount && moves[index].PotentialScore == moves[index + 1].PotentialScore)
                        index++;
                    for (int i = index; i >= 0 && moves[i].PotentialScore == move.PotentialScore; i--)
                        if (moves[i].Hash == move.Hash && moves[i].Same(move))
                            return;
                    index++;
                }
                if (index != movesCount)
                    Array.Copy(moves, index, moves, index + 1, movesCount - index);
                moves[index] = move;
                movesCount++;
            }
            else
                moves[movesCount++] = move;
        }
        else
        {
            if (moves[moves.Length - 1].PotentialScore >= move.PotentialScore)
                return;

            int index = Array.BinarySearch(moves, 0, movesCount, move, MovePotentialCostComparer);

            if (index < 0)
                index = ~index;
            else
            {
                while (index + 1 < movesCount && moves[index].PotentialScore == moves[index + 1].PotentialScore)
                    index++;
                for (int i = index; i >= 0 && moves[i].PotentialScore == move.PotentialScore; i--)
                    if (moves[i].Hash == move.Hash && moves[i].Same(move))
                        return;
                if (index < movesCount + 1)
                    index++;
            }
            if (index != moves.Length - 1)
                Array.Copy(moves, index, moves, index + 1, moves.Length - 1 - index);
            moves[index] = move;
        }
    }

    private State SolveGreedy(State inputState, int costLantern, int costMirror, int costObstacle, int maxMirrors, int maxObstacles)
    {
        State bestSolution = CloneState(inputState);
        State crystalLights = CloneState(inputState); // TODO: Remove once ExtendedState implements everything needed for this function.
        int width = inputState.Width;
        int height = inputState.Height;
        ExtendedState extendedState = new ExtendedState(inputState);

        // Backtrace crystals to get potential places for lanterns
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if ((crystalLights.Board[y, x] & BoardField.Crystal) == BoardField.Crystal)
                    crystalLights.BacktraceCrystal(x, y);

        // Pick all unique intersections
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if ((bestSolution.Board[y, x] != BoardField.Empty) || (bestSolution.LightMap[y, x] != Light.Empty))
                    continue;

                Light light = crystalLights.LightMap[y, x];
                Light color = light & Light.ColorMask;

                if (color == Light.Blue)
                {
                    Light direction = light & Light.BlueDirectionMask;

                    if (direction == Light.BlueDown || direction == Light.BlueLeft || direction == Light.BlueRight || direction == Light.BlueUp)
                        continue;
                    bestSolution.PutLantern(new Position(x, y), color, costLantern);
                    extendedState.PutLantern(new Position(x, y), color, costLantern);
                }
                else if (color == Light.Yellow)
                {
                    Light direction = light & Light.YellowDirectionMask;

                    if (direction == Light.YellowDown || direction == Light.YellowLeft || direction == Light.YellowRight || direction == Light.YellowUp)
                        continue;
                    bestSolution.PutLantern(new Position(x, y), color, costLantern);
                    extendedState.PutLantern(new Position(x, y), color, costLantern);
                }
                else if (color == Light.Red)
                {
                    Light direction = light & Light.RedDirectionMask;

                    if (direction == Light.RedDown || direction == Light.RedLeft || direction == Light.RedRight || direction == Light.RedUp)
                        continue;
                    bestSolution.PutLantern(new Position(x, y), color, costLantern);
                    extendedState.PutLantern(new Position(x, y), color, costLantern);
                }
            }

        Console.Error.WriteLine("Greedy clear: {0} ({1}s)", bestSolution.Score, sw.Elapsed.TotalSeconds);

        // Pick all intersections with 1 error
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if ((bestSolution.Board[y, x] != BoardField.Empty) || (bestSolution.LightMap[y, x] != Light.Empty))
                    continue;

                Light light = crystalLights.LightMap[y, x];
                Light color = light & Light.ColorMask;
                Light blueDirection = light & Light.BlueDirectionMask;
                Light yellowDirection = light & Light.YellowDirectionMask;
                Light redDirection = light & Light.RedDirectionMask;
                bool blueMultiple = blueDirection != Light.Empty && blueDirection != Light.BlueDown && blueDirection != Light.BlueLeft && blueDirection != Light.BlueRight && blueDirection != Light.BlueUp;
                bool yellowMultiple = yellowDirection != Light.Empty && yellowDirection != Light.YellowDown && yellowDirection != Light.YellowLeft && yellowDirection != Light.YellowRight && yellowDirection != Light.YellowUp;
                bool redMultiple = redDirection != Light.Empty && redDirection != Light.RedDown && redDirection != Light.RedLeft && redDirection != Light.RedRight && redDirection != Light.RedUp;
                Position position = new Position(x, y);

                if (blueMultiple && !yellowMultiple && !redMultiple)
                {
                    if (yellowDirection == Light.Empty || redDirection == Light.Empty)
                    {
                        int score = extendedState.GetLanternScore(position, Light.Blue, costLantern).Score;
                        if (score > bestSolution.Score)
                        {
                            bestSolution.PutLantern(position, Light.Blue, costLantern);
                            extendedState.PutLantern(position, Light.Blue, costLantern);
                        }
                    }
                }
                else if (!blueMultiple && yellowMultiple && !redMultiple)
                {
                    if (blueDirection == Light.Empty || redDirection == Light.Empty)
                    {
                        int score = extendedState.GetLanternScore(position, Light.Yellow, costLantern).Score;
                        if (score > bestSolution.Score)
                        {
                            bestSolution.PutLantern(position, Light.Yellow, costLantern);
                            extendedState.PutLantern(new Position(x, y), Light.Yellow, costLantern);
                        }
                    }
                }
                else if (!blueMultiple && !yellowMultiple && redMultiple)
                {
                    if (blueDirection == Light.Empty || yellowDirection == Light.Empty)
                    {
                        int score = extendedState.GetLanternScore(position, Light.Red, costLantern).Score;
                        if (score > bestSolution.Score)
                        {
                            bestSolution.PutLantern(position, Light.Red, costLantern);
                            extendedState.PutLantern(new Position(x, y), Light.Red, costLantern);
                        }
                    }
                }
            }

        Console.Error.WriteLine("Greedy 1 error: {0} ({1}s)", bestSolution.Score, sw.Elapsed.TotalSeconds);

        // Pick all intersections with any number of errors
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if ((bestSolution.Board[y, x] != BoardField.Empty) || (bestSolution.LightMap[y, x] != Light.Empty))
                    continue;

                Light light = crystalLights.LightMap[y, x];
                Light color = light & Light.ColorMask;
                Light blueDirection = light & Light.BlueDirectionMask;
                Light yellowDirection = light & Light.YellowDirectionMask;
                Light redDirection = light & Light.RedDirectionMask;
                bool blueMultiple = blueDirection != Light.Empty && blueDirection != Light.BlueDown && blueDirection != Light.BlueLeft && blueDirection != Light.BlueRight && blueDirection != Light.BlueUp;
                bool yellowMultiple = yellowDirection != Light.Empty && yellowDirection != Light.YellowDown && yellowDirection != Light.YellowLeft && yellowDirection != Light.YellowRight && yellowDirection != Light.YellowUp;
                bool redMultiple = redDirection != Light.Empty && redDirection != Light.RedDown && redDirection != Light.RedLeft && redDirection != Light.RedRight && redDirection != Light.RedUp;
                Position position = new Position(x, y);

                if (blueMultiple)
                {
                    int score = extendedState.GetLanternScore(position, Light.Blue, costLantern).Score;
                    if (score > bestSolution.Score)
                    {
                        bestSolution.PutLantern(position, Light.Blue, costLantern);
                        extendedState.PutLantern(position, Light.Blue, costLantern);
                        continue;
                    }
                }
                if (yellowMultiple)
                {
                    int score = extendedState.GetLanternScore(position, Light.Yellow, costLantern).Score;
                    if (score > bestSolution.Score)
                    {
                        bestSolution.PutLantern(position, Light.Yellow, costLantern);
                        extendedState.PutLantern(position, Light.Yellow, costLantern);
                        continue;
                    }
                }
                if (redMultiple)
                {
                    int score = extendedState.GetLanternScore(position, Light.Red, costLantern).Score;
                    if (score > bestSolution.Score)
                    {
                        bestSolution.PutLantern(position, Light.Red, costLantern);
                        extendedState.PutLantern(position, Light.Red, costLantern);
                        continue;
                    }
                }
            }

        Console.Error.WriteLine("Greedy 2 errors: {0} ({1}s)", bestSolution.Score, sw.Elapsed.TotalSeconds);

        // Put lanterns as long as our score increases
        State previousState = CloneState(bestSolution);

        while (sw.Elapsed < maxTime)
        {
            Position bestLanternPosition = new Position();
            Light bestLanternColor = Light.Empty;
            int bestScore = 0;

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if ((bestSolution.Board[y, x] != BoardField.Empty) || (bestSolution.LightMap[y, x] != Light.Empty))
                        continue;

                    Light light = crystalLights.LightMap[y, x];
                    Light color = light & Light.ColorMask;

                    if (color == Light.Empty)
                        continue;

                    Position position = new Position(x, y);

                    // Try to put Blue lantern
                    if ((color & Light.Blue) == Light.Blue)
                    {
                        int score = extendedState.GetLanternScore(position, Light.Blue, costLantern).Score;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestLanternColor = Light.Blue;
                            bestLanternPosition = position;
                        }
                    }

                    // Try to put Yellow lantern
                    if ((color & Light.Yellow) == Light.Yellow)
                    {
                        int score = extendedState.GetLanternScore(position, Light.Yellow, costLantern).Score;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestLanternColor = Light.Yellow;
                            bestLanternPosition = position;
                        }
                    }

                    // Try to put Red lantern
                    if ((color & Light.Red) == Light.Red)
                    {
                        int score = extendedState.GetLanternScore(position, Light.Red, costLantern).Score;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestLanternColor = Light.Red;
                            bestLanternPosition = position;
                        }
                    }
                }

            if (bestScore == 0)
                break;

            if (bestScore <= bestSolution.Score)
                break;
            bestSolution.PutLantern(bestLanternPosition, bestLanternColor, costLantern);
            previousState.PutLantern(bestLanternPosition, bestLanternColor, costLantern);
            extendedState.PutLantern(bestLanternPosition, bestLanternColor, costLantern);
        }

        Console.Error.WriteLine("Greedy lanterns: {0} ({1}s)", bestSolution.Score, sw.Elapsed.TotalSeconds);

        // Continue with lanterns, mirrors and obstacles
        int removals = 0, maxRemovals = (bestSolution.Lanterns.Count + bestSolution.Obstacles.Count + bestSolution.Mirrors.Count);
        State solution = CloneState(bestSolution);

        while (sw.Elapsed < maxTime)
        {
            // Try adding stuff to the board
            Position bestLanternPosition = new Position();
            Light bestLanternColor = Light.Empty;
            int bestLanternScore = 0;
            Position bestObstaclePosition = new Position();
            int bestObstacleScore = 0;
            Position bestMirrorPosition = new Position();
            bool bestMirrorSlash = false;
            int bestMirrorScore = 0;

            solution.Copy(previousState);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (solution.Board[y, x] != BoardField.Empty)
                        continue;

                    // Should we put lantern?
                    if (solution.LightMap[y, x] == Light.Empty)
                    {
                        Light light = crystalLights.LightMap[y, x];
                        Light color = light & Light.ColorMask;

                        if (color == Light.Empty)
                            continue;

                        Position position = new Position(x, y);

                        // Try to put Blue lantern
                        if ((color & Light.Blue) == Light.Blue)
                        {
                            if (solution.PutLantern(position, Light.Blue, costLantern))
                                if (solution.Score > bestLanternScore)
                                {
                                    bestLanternScore = solution.Score;
                                    bestLanternColor = Light.Blue;
                                    bestLanternPosition = position;
                                }
                            solution.RemoveLantern(position, costLantern);
                        }

                        // Try to put Yellow lantern
                        if ((color & Light.Yellow) == Light.Yellow)
                        {
                            if (solution.PutLantern(position, Light.Yellow, costLantern))
                                if (solution.Score > bestLanternScore)
                                {
                                    bestLanternScore = solution.Score;
                                    bestLanternColor = Light.Yellow;
                                    bestLanternPosition = position;
                                }
                            solution.RemoveLantern(position, costLantern);
                        }

                        // Try to put Red lantern
                        if ((color & Light.Red) == Light.Red)
                        {
                            if (solution.PutLantern(position, Light.Red, costLantern))
                                if (solution.Score > bestLanternScore)
                                {
                                    bestLanternScore = solution.Score;
                                    bestLanternColor = Light.Red;
                                    bestLanternPosition = position;
                                }
                            solution.RemoveLantern(position, costLantern);
                        }
                    }
                    else
                    {
                        Position position = new Position(x, y);

                        // Try to put Obstacle
                        if (previousState.Obstacles.Count < maxObstacles)
                        {
                            solution.PutObstacle(position, costObstacle);
                            if (solution.Score > bestObstacleScore)
                            {
                                bestObstacleScore = solution.Score;
                                bestObstaclePosition = position;
                            }
                            solution.RemoveObstacle(position, costObstacle);
                        }

                        // Try to put slash Mirror /
                        if (previousState.Mirrors.Count < maxMirrors)
                        {
                            if (solution.PutMirror(position, BoardField.MirrorSlash, costMirror))
                            {
                                if (solution.Score > bestMirrorScore)
                                {
                                    bestMirrorScore = solution.Score;
                                    bestMirrorPosition = position;
                                    bestMirrorSlash = true;
                                }
                                solution.RemoveMirror(position, costMirror);
                            }
                        }

                        // Try to put backslash Mirror \
                        if (previousState.Mirrors.Count < maxMirrors)
                        {
                            if (solution.PutMirror(position, BoardField.MirrorBackSlash, costMirror))
                            {
                                if (solution.Score > bestMirrorScore)
                                {
                                    bestMirrorScore = solution.Score;
                                    bestMirrorPosition = position;
                                    bestMirrorSlash = false;
                                }
                                solution.RemoveMirror(position, costMirror);
                            }
                        }
                    }
                }

            // Try removing something from the board
            Position bestLanternRemovalPosition = new Position();
            int bestLanternRemovalScore = 0;
            Position bestObstacleRemovalPosition = new Position();
            int bestObstacleRemovalScore = 0;
            Position bestMirrorRemovalPosition = new Position();
            int bestMirrorRemovalScore = 0;

            if (removals < maxRemovals)
            {
                for (int i = 0; i < previousState.Lanterns.Count; i++)
                {
                    solution.RemoveLantern(previousState.Lanterns[i].Position, costLantern);
                    if (solution.Score > bestLanternRemovalScore)
                    {
                        bestLanternRemovalPosition = previousState.Lanterns[i].Position;
                        bestLanternRemovalScore = solution.Score;
                    }
                    solution.PutLantern(previousState.Lanterns[i].Position, (Light)previousState.Lanterns[i].Color, costLantern);
                }

                for (int i = 0; i < previousState.Obstacles.Count; i++)
                {
                    if (!solution.RemoveObstacle(previousState.Obstacles[i].Position, costObstacle))
                    {
                        if (solution.Score > bestObstacleRemovalScore)
                        {
                            bestObstacleRemovalPosition = previousState.Obstacles[i].Position;
                            bestObstacleRemovalScore = solution.Score;
                        }
                        solution.PutObstacle(previousState.Obstacles[i].Position, costObstacle);
                    }
                }

                for (int i = 0; i < previousState.Mirrors.Count; i++)
                {
                    if (!solution.RemoveMirror(previousState.Mirrors[i].Position, costMirror))
                    {
                        if (solution.Score > bestMirrorRemovalScore)
                        {
                            bestMirrorRemovalPosition = previousState.Mirrors[i].Position;
                            bestMirrorRemovalScore = solution.Score;
                        }
                        solution.PutMirror(previousState.Mirrors[i].Position, previousState.Mirrors[i].Slash ? BoardField.MirrorSlash : BoardField.MirrorBackSlash, costMirror);
                    }
                }
            }

            // Check what was the best
            if (bestLanternScore == 0 && bestObstacleScore == 0 && bestMirrorScore == 0)
                break;

            if (bestLanternScore >= bestMirrorScore && bestLanternScore >= bestObstacleScore && bestLanternScore >= bestLanternRemovalScore && bestLanternScore >= bestMirrorRemovalScore && bestLanternScore >= bestObstacleRemovalScore)
                previousState.PutLantern(bestLanternPosition, bestLanternColor, costLantern);
            else if (bestMirrorScore >= bestLanternScore && bestMirrorScore >= bestObstacleScore && bestMirrorScore >= bestLanternRemovalScore && bestMirrorScore >= bestMirrorRemovalScore && bestMirrorScore >= bestObstacleRemovalScore)
            {
                previousState.PutMirror(bestMirrorPosition, bestMirrorSlash ? BoardField.MirrorSlash : BoardField.MirrorBackSlash, costMirror);
                crystalLights.PutMirror(bestMirrorPosition, bestMirrorSlash ? BoardField.MirrorSlash : BoardField.MirrorBackSlash, costMirror);
            }
            else if (bestObstacleScore >= bestLanternScore && bestObstacleScore >= bestMirrorScore && bestObstacleScore >= bestLanternRemovalScore && bestObstacleScore >= bestMirrorRemovalScore && bestObstacleScore >= bestObstacleRemovalScore)
            {
                previousState.PutObstacle(bestObstaclePosition, costObstacle);
                crystalLights.PutObstacle(bestObstaclePosition, costObstacle);
            }
            else if (bestLanternRemovalScore >= bestLanternScore && bestLanternRemovalScore >= bestObstacleScore && bestLanternRemovalScore >= bestMirrorScore && bestLanternRemovalScore >= bestObstacleRemovalScore && bestLanternRemovalScore >= bestMirrorRemovalScore)
            {
                previousState.RemoveLantern(bestLanternRemovalPosition, costLantern);
                removals++;
            }
            else if (bestObstacleRemovalScore >= bestLanternScore && bestObstacleRemovalScore >= bestObstacleScore && bestObstacleRemovalScore >= bestMirrorScore && bestObstacleRemovalScore >= bestMirrorRemovalScore && bestObstacleRemovalScore >= bestLanternRemovalScore)
            {
                previousState.RemoveObstacle(bestObstacleRemovalPosition, costObstacle);
                crystalLights.RemoveObstacle(bestObstacleRemovalPosition, costObstacle);
                removals++;
            }
            else if (bestMirrorRemovalScore >= bestLanternScore && bestMirrorRemovalScore >= bestObstacleScore && bestMirrorRemovalScore >= bestMirrorScore && bestMirrorRemovalScore >= bestLanternRemovalScore && bestMirrorRemovalScore >= bestObstacleRemovalScore)
            {
                previousState.RemoveMirror(bestMirrorRemovalPosition, costObstacle);
                crystalLights.RemoveMirror(bestMirrorRemovalPosition, costObstacle);
                removals++;
            }

            if (previousState.Score > bestSolution.Score)
                bestSolution.Copy(previousState);
        }

        Console.Error.WriteLine("Greedy: {0} ({1}s)", bestSolution.Score, sw.Elapsed.TotalSeconds);

        return bestSolution;
    }

    private class StateCostComparerClass : IComparer<State>
    {
        public int Compare(State x, State y)
        {
            return y.Score - x.Score;
        }
    }

    static StateCostComparerClass StateCostComparer = new StateCostComparerClass();

    private class StatePotentialCostComparerClass : IComparer<State>
    {
        public int Compare(State x, State y)
        {
            return y.PotentialScore - x.PotentialScore;
        }
    }

    static StatePotentialCostComparerClass StatePotentialCostComparer = new StatePotentialCostComparerClass();

    private static void AddSolution(State[] solutions, ref int solutionsCount, State solution, State[] stateCache, ref int stateCacheCount)
    {
        if (solutionsCount < solutions.Length)
        {
            if (solutionsCount > 0)
            {
                int index = Array.BinarySearch(solutions, 0, solutionsCount, solution, StatePotentialCostComparer);

                if (index < 0)
                {
                    index = ~index;
                }
                else
                {
                    while (index + 1 < solutionsCount && solutions[index].PotentialScore == solutions[index + 1].PotentialScore)
                        index++;
                    for (int i = index; i >= 0 && solutions[i].PotentialScore == solution.PotentialScore; i--)
                        if (solutions[i].Hash == solution.Hash && solutions[i].Same(solution))
                            return;
                    index++;
                }
                if (index != solutionsCount)
                    Array.Copy(solutions, index, solutions, index + 1, solutionsCount - index);
                solutions[index] = CloneState(stateCache, ref stateCacheCount, solution);
                solutionsCount++;
            }
            else
                solutions[solutionsCount++] = CloneState(stateCache, ref stateCacheCount, solution);
        }
        else
        {
            if (solutions[solutions.Length - 1].PotentialScore >= solution.PotentialScore)
                return;

            int index = Array.BinarySearch(solutions, 0, solutionsCount, solution, StatePotentialCostComparer);

            if (index < 0)
                index = ~index;
            else
            {
                while (index + 1 < solutionsCount && solutions[index].PotentialScore == solutions[index + 1].PotentialScore)
                    index++;
                for (int i = index; i >= 0 && solutions[i].PotentialScore == solution.PotentialScore; i--)
                    if (solutions[i].Hash == solution.Hash && solutions[i].Same(solution))
                        return;
                if (index < solutionsCount + 1)
                    index++;
            }
            stateCache[stateCacheCount++] = solutions[solutions.Length - 1];
            if (index != solutions.Length - 1)
                Array.Copy(solutions, index, solutions, index + 1, solutions.Length - 1 - index);
            solutions[index] = CloneState(stateCache, ref stateCacheCount, solution);
        }
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

    private static State CloneState(State inputState)
    {
        int height = inputState.Board.GetLength(0);
        int width = inputState.Board.GetLength(1);
        State state = new State(width, height);

        state.Copy(inputState);
        return state;
    }
}
