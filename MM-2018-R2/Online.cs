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
        PotentialScore -= cost;
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
        Hash = Hash ^ ((int)fieldColor << 16) ^ position.X ^ (position.Y << 8);
        return true;
    }

    public void RemoveLantern(Position position, int cost, int previousHash)
    {
        ClearColorLeft(position.X - 1, position.Y);
        ClearColorRight(position.X + 1, position.Y);
        ClearColorUp(position.X, position.Y - 1);
        ClearColorDown(position.X, position.Y + 1);
        Score += cost;
        PotentialScore += cost;
        Board[position.Y, position.X] = BoardField.Empty;
        for (int i = Lanterns.Count - 1; i >= 0; i--)
        {
            Position lp = Lanterns[i].Position;

            if (lp.X == position.X && lp.Y == position.Y)
            {
                Lanterns.RemoveAt(i);
                break;
            }
        }
        Hash = previousHash;
    }

    public void PutObstacle(Position position, int cost)
    {
        ClearColor(position);
        Score -= cost;
        PotentialScore -= cost;
        Obstacles.Add(new Obstacle()
        {
            Position = position,
        });
        Board[position.Y, position.X] |= BoardField.Obstacle;
        Hash = Hash ^ (position.X << 17) ^ (position.Y << 9);
    }

    public bool RemoveObstacle(Position position, int cost, int previousHash)
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
        for (int i = Obstacles.Count - 1; i >= 0; i--)
        {
            Position lp = Obstacles[i].Position;

            if (lp.X == position.X && lp.Y == position.Y)
            {
                Obstacles.RemoveAt(i);
                break;
            }
        }
        Board[position.Y, position.X] = BoardField.Empty;
        Hash = previousHash;
        return true;
    }

    public bool PutMirror(Position position, BoardField mirrorType, int cost)
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

        // Correct light path
        ClearColor(position);
        if (mirrorType == BoardField.MirrorSlash)
        {
            if ((light & Light.LeftMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueLeft) == Light.BlueLeft ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowLeft) == Light.YellowLeft ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedLeft) == Light.RedLeft ? Light.Red : Light.Empty);

                if (!AddColorDown(position.X, position.Y + 1, color))
                    throw new Exception("ASDFASDF");
            }
            if ((light & Light.RightMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueRight) == Light.BlueRight ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowRight) == Light.YellowRight ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedRight) == Light.RedRight ? Light.Red : Light.Empty);

                if (!AddColorUp(position.X, position.Y - 1, color))
                    throw new Exception("ASDFASDF");
            }
            if ((light & Light.DownMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueDown) == Light.BlueDown ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowDown) == Light.YellowDown ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedDown) == Light.RedDown ? Light.Red : Light.Empty);

                if (!AddColorLeft(position.X - 1, position.Y, color))
                    throw new Exception("ASDFASDF");
            }
            if ((light & Light.UpMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueUp) == Light.BlueUp ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowUp) == Light.YellowUp ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedUp) == Light.RedUp ? Light.Red : Light.Empty);

                if (!AddColorRight(position.X + 1, position.Y, color))
                    throw new Exception("ASDFASDF");
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
                    throw new Exception("ASDFASDF");
            }
            if ((light & Light.RightMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueRight) == Light.BlueRight ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowRight) == Light.YellowRight ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedRight) == Light.RedRight ? Light.Red : Light.Empty);

                if (!AddColorDown(position.X, position.Y + 1, color))
                    throw new Exception("ASDFASDF");
            }
            if ((light & Light.DownMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueDown) == Light.BlueDown ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowDown) == Light.YellowDown ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedDown) == Light.RedDown ? Light.Red : Light.Empty);

                if (!AddColorRight(position.X + 1, position.Y, color))
                    throw new Exception("ASDFASDF");
            }
            if ((light & Light.UpMask) != Light.Empty)
            {
                Light color = ((light & Light.BlueUp) == Light.BlueUp ? Light.Blue : Light.Empty)
                    | ((light & Light.YellowUp) == Light.YellowUp ? Light.Yellow : Light.Empty)
                    | ((light & Light.RedUp) == Light.RedUp ? Light.Red : Light.Empty);

                if (!AddColorLeft(position.X - 1, position.Y, color))
                    throw new Exception("ASDFASDF");
            }
        }
        Score -= cost;
        PotentialScore -= cost;
        Mirrors.Add(new Mirror()
        {
            Position = position,
            Slash = mirrorType == BoardField.MirrorSlash,
        });
        Board[position.Y, position.X] |= mirrorType;
        Hash = Hash ^ (position.X << 11) ^ (position.Y << 3) ^ ((int)mirrorType << 20);
        return true;
    }

    public bool RemoveMirror(Position position, int cost, int previousHash)
    {
        Light light = LightMap[position.Y, position.X];
        bool hasLeft = (light & Light.LeftMask) != Light.Empty;
        bool hasRight = (light & Light.RightMask) != Light.Empty;
        bool hasDown = (light & Light.DownMask) != Light.Empty;
        bool hasUp = (light & Light.UpMask) != Light.Empty;

        if (hasLeft && hasRight)
            return false;
        if (hasUp && hasDown)
            return false;

        if (Board[position.Y, position.X] == BoardField.MirrorSlash)
        {
            if (hasLeft)
                ClearColorDown(position.X, position.Y + 1);
            if (hasRight)
                ClearColorUp(position.X, position.Y - 1);
            if (hasDown)
                ClearColorLeft(position.X - 1, position.Y);
            if (hasUp)
                ClearColorRight(position.X + 1, position.Y);
        }
        else
        {
            if (hasLeft)
                ClearColorUp(position.X, position.Y - 1);
            if (hasRight)
                ClearColorDown(position.X, position.Y + 1);
            if (hasDown)
                ClearColorRight(position.X + 1, position.Y);
            if (hasUp)
                ClearColorLeft(position.X - 1, position.Y);
        }
        if (hasLeft)
        {
            Light color = ((light & Light.BlueLeft) == Light.BlueLeft ? Light.Blue : Light.Empty)
                | ((light & Light.YellowLeft) == Light.YellowLeft ? Light.Yellow : Light.Empty)
                | ((light & Light.RedLeft) == Light.RedLeft ? Light.Red : Light.Empty);

            AddColorLeft(position.X - 1, position.Y, color);
        }
        if (hasRight)
        {
            Light color = ((light & Light.BlueRight) == Light.BlueRight ? Light.Blue : Light.Empty)
                | ((light & Light.YellowRight) == Light.YellowRight ? Light.Yellow : Light.Empty)
                | ((light & Light.RedRight) == Light.RedRight ? Light.Red : Light.Empty);

            AddColorRight(position.X + 1, position.Y, color);
        }
        if (hasDown)
        {
            Light color = ((light & Light.BlueDown) == Light.BlueDown ? Light.Blue : Light.Empty)
                | ((light & Light.YellowDown) == Light.YellowDown ? Light.Yellow : Light.Empty)
                | ((light & Light.RedDown) == Light.RedDown ? Light.Red : Light.Empty);

            AddColorDown(position.X, position.Y + 1, color);
        }
        if (hasUp)
        {
            Light color = ((light & Light.BlueUp) == Light.BlueUp ? Light.Blue : Light.Empty)
                | ((light & Light.YellowUp) == Light.YellowUp ? Light.Yellow : Light.Empty)
                | ((light & Light.RedUp) == Light.RedUp ? Light.Red : Light.Empty);

            AddColorUp(position.X, position.Y - 1, color);
        }
        Score += cost;
        PotentialScore += cost;
        for (int i = Mirrors.Count - 1; i >= 0; i--)
        {
            Position lp = Mirrors[i].Position;

            if (lp.X == position.X && lp.Y == position.Y)
            {
                Mirrors.RemoveAt(i);
                break;
            }
        }
        Board[position.Y, position.X] = BoardField.Empty;
        Hash = previousHash;
        return true;
    }

    public void BacktraceCrystal(int x, int y)
    {
        BoardField field = Board[y, x];
        Light color = (Light)(field & BoardField.ColorMask);

        AddColorDown(x, y + 1, color);
        AddColorUp(x, y - 1, color);
        AddColorLeft(x - 1, y, color);
        AddColorRight(x + 1, y, color);
    }

    public bool HasColor(Position position)
    {
        return (LightMap[position.Y, position.X] & Light.ColorMask) != Light.Empty;
    }

    public bool HasObject(Position position)
    {
        return (Board[position.Y, position.X] & BoardField.ObjectMask) != BoardField.Empty;
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
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
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
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
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
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
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
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
                break;
            x++;
        }
    }

    private bool AddColorDown(int x, int y, Light color)
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
                return false;

            // Stop if we hit an object
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
                break;
            y++;
        }
        return true;
    }

    private bool AddColorUp(int x, int y, Light color)
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
                return false;

            // Stop if we hit an object
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
                break;
            y--;
        }
        return true;
    }

    private bool AddColorRight(int x, int y, Light color)
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
                return false;

            // Stop if we hit an object
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
                break;
            x++;
        }
        return true;
    }

    private bool AddColorLeft(int x, int y, Light color)
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
                return false;

            // Stop if we hit an object
            if ((field & BoardField.ObjectMask) != BoardField.Empty)
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

public class CrystalLighting
{
    private Stopwatch sw;
#if LOCAL
    private TimeSpan maxTime = Debugger.IsAttached ? TimeSpan.FromSeconds(99999.5) : TimeSpan.FromSeconds(2.5);
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
        sbyte height = (sbyte)inputState.Board.GetLength(0);
        sbyte width = (sbyte)inputState.Board.GetLength(1);
        Position position = new Position();
        int steps = 0;

        previousSolutions[previousSolutionsCount++] = CloneState(stateCache, ref stateCacheCount, inputState);
        while (sw.Elapsed < maxTime && previousSolutionsCount > 0)
        {
#if LOCAL
            Console.Error.WriteLine("{0}. {1}   {2}s", steps, bestSolution.Score, sw.Elapsed.TotalSeconds);
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
                                solution.RemoveLantern(position, costLantern, previousState.Hash);

                                // Try to put Yellow lantern
                                solution.PutLantern(position, Light.Yellow, costLantern);
                                AddSolution(solutions, ref solutionsCount, solution, stateCache, ref stateCacheCount);
                                solution.RemoveLantern(position, costLantern, previousState.Hash);

                                // Try to put Red lantern
                                solution.PutLantern(position, Light.Red, costLantern);
                                AddSolution(solutions, ref solutionsCount, solution, stateCache, ref stateCacheCount);
                                solution.RemoveLantern(position, costLantern, previousState.Hash);
                            }
                            else
                            {
                                // Try to put Obstacle
                                if (solution.Obstacles.Count < maxObstacles)
                                {
                                    solution.PutObstacle(position, costObstacle);
                                    AddSolution(solutions, ref solutionsCount, solution, stateCache, ref stateCacheCount);
                                    solution.RemoveObstacle(position, costObstacle, previousState.Hash);
                                }

                                // Try to put slash Mirror /
                                if (solution.Mirrors.Count < maxMirrors)
                                {
                                    if (solution.PutMirror(position, BoardField.MirrorSlash, costMirror))
                                    {
                                        AddSolution(solutions, ref solutionsCount, solution, stateCache, ref stateCacheCount);
                                        solution.RemoveMirror(position, costMirror, previousState.Hash);
                                    }
                                }

                                // Try to put backslash Mirror \
                                if (solution.Mirrors.Count < maxMirrors)
                                {
                                    if (solution.PutMirror(position, BoardField.MirrorBackSlash, costMirror))
                                    {
                                        AddSolution(solutions, ref solutionsCount, solution, stateCache, ref stateCacheCount);
                                        solution.RemoveMirror(position, costMirror, previousState.Hash);
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

    private State SolveGreedy(State inputState, int costLantern, int costMirror, int costObstacle, int maxMirrors, int maxObstacles)
    {
        State bestSolution = CloneState(inputState);
        State lightIntersections = CloneState(inputState);
        int width = inputState.Width;
        int height = inputState.Height;
        State solution = CloneState(bestSolution);

        // Backtrace crystals to get potential places for lanterns
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if ((lightIntersections.Board[y, x] & BoardField.Crystal) == BoardField.Crystal)
                    lightIntersections.BacktraceCrystal(x, y);

        // Pick all unique intersections
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if ((bestSolution.Board[y, x] != BoardField.Empty) || (bestSolution.LightMap[y, x] != Light.Empty))
                    continue;

                Light light = lightIntersections.LightMap[y, x];
                Light color = light & Light.ColorMask;

                if (color == Light.Blue)
                {
                    Light direction = light & Light.BlueDirectionMask;

                    if (direction == Light.BlueDown || direction == Light.BlueLeft || direction == Light.BlueRight || direction == Light.BlueUp)
                        continue;
                    bestSolution.PutLantern(new Position((sbyte)x, (sbyte)y), color, costLantern);
                }
                else if (color == Light.Yellow)
                {
                    Light direction = light & Light.YellowDirectionMask;

                    if (direction == Light.YellowDown || direction == Light.YellowLeft || direction == Light.YellowRight || direction == Light.YellowUp)
                        continue;
                    bestSolution.PutLantern(new Position((sbyte)x, (sbyte)y), color, costLantern);
                }
                else if (color == Light.Red)
                {
                    Light direction = light & Light.RedDirectionMask;

                    if (direction == Light.RedDown || direction == Light.RedLeft || direction == Light.RedRight || direction == Light.RedUp)
                        continue;
                    bestSolution.PutLantern(new Position((sbyte)x, (sbyte)y), color, costLantern);
                }
            }

        Console.Error.WriteLine("Greedy clear: {0} ({1}s)", bestSolution.Score, sw.Elapsed.TotalSeconds);

        // Pick all intersections with 1 error
        solution = CloneState(bestSolution);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if ((bestSolution.Board[y, x] != BoardField.Empty) || (bestSolution.LightMap[y, x] != Light.Empty))
                    continue;

                Light light = lightIntersections.LightMap[y, x];
                Light color = light & Light.ColorMask;
                Light blueDirection = light & Light.BlueDirectionMask;
                Light yellowDirection = light & Light.YellowDirectionMask;
                Light redDirection = light & Light.RedDirectionMask;
                bool blueMultiple = blueDirection != Light.Empty && blueDirection != Light.BlueDown && blueDirection != Light.BlueLeft && blueDirection != Light.BlueRight && blueDirection != Light.BlueUp;
                bool yellowMultiple = yellowDirection != Light.Empty && yellowDirection != Light.YellowDown && yellowDirection != Light.YellowLeft && yellowDirection != Light.YellowRight && yellowDirection != Light.YellowUp;
                bool redMultiple = redDirection != Light.Empty && redDirection != Light.RedDown && redDirection != Light.RedLeft && redDirection != Light.RedRight && redDirection != Light.RedUp;
                Position position = new Position((sbyte)x, (sbyte)y);

                if (blueMultiple && !yellowMultiple && !redMultiple)
                {
                    if (yellowDirection == Light.Empty || redDirection == Light.Empty)
                    {
                        solution.PutLantern(position, Light.Blue, costLantern);
                        if (solution.Score > bestSolution.Score)
                            bestSolution.PutLantern(position, Light.Blue, costLantern);
                        else
                            solution.RemoveLantern(position, costLantern, bestSolution.Hash);
                    }
                }
                else if (!blueMultiple && yellowMultiple && !redMultiple)
                {
                    if (blueDirection == Light.Empty || redDirection == Light.Empty)
                    {
                        solution.PutLantern(position, Light.Yellow, costLantern);
                        if (solution.Score > bestSolution.Score)
                            bestSolution.PutLantern(position, Light.Yellow, costLantern);
                        else
                            solution.RemoveLantern(position, costLantern, bestSolution.Hash);
                    }
                }
                else if (!blueMultiple && !yellowMultiple && redMultiple)
                {
                    if (blueDirection == Light.Empty || yellowDirection == Light.Empty)
                    {
                        solution.PutLantern(position, Light.Red, costLantern);
                        if (solution.Score > bestSolution.Score)
                            bestSolution.PutLantern(position, Light.Red, costLantern);
                        else
                            solution.RemoveLantern(position, costLantern, bestSolution.Hash);
                    }
                }
            }

        Console.Error.WriteLine("Greedy 1 error: {0} ({1}s)", bestSolution.Score, sw.Elapsed.TotalSeconds);

        // Pick all intersections with any number of errors
        solution = CloneState(bestSolution);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if ((bestSolution.Board[y, x] != BoardField.Empty) || (bestSolution.LightMap[y, x] != Light.Empty))
                    continue;

                Light light = lightIntersections.LightMap[y, x];
                Light color = light & Light.ColorMask;
                Light blueDirection = light & Light.BlueDirectionMask;
                Light yellowDirection = light & Light.YellowDirectionMask;
                Light redDirection = light & Light.RedDirectionMask;
                bool blueMultiple = blueDirection != Light.Empty && blueDirection != Light.BlueDown && blueDirection != Light.BlueLeft && blueDirection != Light.BlueRight && blueDirection != Light.BlueUp;
                bool yellowMultiple = yellowDirection != Light.Empty && yellowDirection != Light.YellowDown && yellowDirection != Light.YellowLeft && yellowDirection != Light.YellowRight && yellowDirection != Light.YellowUp;
                bool redMultiple = redDirection != Light.Empty && redDirection != Light.RedDown && redDirection != Light.RedLeft && redDirection != Light.RedRight && redDirection != Light.RedUp;
                Position position = new Position((sbyte)x, (sbyte)y);

                if (blueMultiple)
                {
                    solution.PutLantern(position, Light.Blue, costLantern);
                    if (solution.Score > bestSolution.Score)
                    {
                        bestSolution.PutLantern(position, Light.Blue, costLantern);
                        continue;
                    }
                    else
                        solution.RemoveLantern(position, costLantern, bestSolution.Hash);
                }
                if (yellowMultiple)
                {
                    solution.PutLantern(position, Light.Yellow, costLantern);
                    if (solution.Score > bestSolution.Score)
                    {
                        bestSolution.PutLantern(position, Light.Yellow, costLantern);
                        continue;
                    }
                    else
                        solution.RemoveLantern(position, costLantern, bestSolution.Hash);
                }
                if (redMultiple)
                {
                    solution.PutLantern(position, Light.Red, costLantern);
                    if (solution.Score > bestSolution.Score)
                    {
                        bestSolution.PutLantern(position, Light.Red, costLantern);
                        continue;
                    }
                    else
                        solution.RemoveLantern(position, costLantern, bestSolution.Hash);
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
            solution.Copy(previousState);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if ((bestSolution.Board[y, x] != BoardField.Empty) || (bestSolution.LightMap[y, x] != Light.Empty))
                        continue;

                    Light light = lightIntersections.LightMap[y, x];
                    Light color = light & Light.ColorMask;

                    if (color == Light.Empty)
                        continue;

                    Position position = new Position((sbyte)x, (sbyte)y);

                    // Try to put Blue lantern
                    if ((color & Light.Blue) == Light.Blue)
                    {
                        if (solution.PutLantern(position, Light.Blue, costLantern))
                            if (solution.Score > bestScore)
                            {
                                bestScore = solution.Score;
                                bestLanternColor = Light.Blue;
                                bestLanternPosition = position;
                            }
                        solution.RemoveLantern(position, costLantern, previousState.Hash);
                    }

                    // Try to put Yellow lantern
                    if ((color & Light.Yellow) == Light.Yellow)
                    {
                        if (solution.PutLantern(position, Light.Yellow, costLantern))
                            if (solution.Score > bestScore)
                            {
                                bestScore = solution.Score;
                                bestLanternColor = Light.Yellow;
                                bestLanternPosition = position;
                            }
                        solution.RemoveLantern(position, costLantern, previousState.Hash);
                    }

                    // Try to put Red lantern
                    if ((color & Light.Red) == Light.Red)
                    {
                        if (solution.PutLantern(position, Light.Red, costLantern))
                            if (solution.Score > bestScore)
                            {
                                bestScore = solution.Score;
                                bestLanternColor = Light.Red;
                                bestLanternPosition = position;
                            }
                        solution.RemoveLantern(position, costLantern, previousState.Hash);
                    }
                }

            if (bestScore == 0)
                break;

            if (bestScore <= bestSolution.Score)
                break;
            bestSolution.PutLantern(bestLanternPosition, bestLanternColor, costLantern);
            previousState.PutLantern(bestLanternPosition, bestLanternColor, costLantern);
        }

        Console.Error.WriteLine("Greedy lanterns: {0} ({1}s)", bestSolution.Score, sw.Elapsed.TotalSeconds);

        // Continue with lanterns, mirrors and obstacles
        int removals = 0, maxRemovals = (bestSolution.Lanterns.Count + bestSolution.Obstacles.Count + bestSolution.Mirrors.Count) * 3;

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
                        Light light = lightIntersections.LightMap[y, x];
                        Light color = light & Light.ColorMask;

                        if (color == Light.Empty)
                            continue;

                        Position position = new Position((sbyte)x, (sbyte)y);

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
                            solution.RemoveLantern(position, costLantern, previousState.Hash);
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
                            solution.RemoveLantern(position, costLantern, previousState.Hash);
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
                            solution.RemoveLantern(position, costLantern, previousState.Hash);
                        }
                    }
                    else
                    {
                        Position position = new Position((sbyte)x, (sbyte)y);

                        // Try to put Obstacle
                        if (previousState.Obstacles.Count < maxObstacles)
                        {
                            solution.PutObstacle(position, costObstacle);
                            if (solution.Score > bestObstacleScore)
                            {
                                bestObstacleScore = solution.Score;
                                bestObstaclePosition = position;
                            }
                            solution.RemoveObstacle(position, costObstacle, previousState.Hash);
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
                                solution.RemoveMirror(position, costMirror, previousState.Hash);
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
                                solution.RemoveMirror(position, costMirror, previousState.Hash);
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
                    solution.RemoveLantern(previousState.Lanterns[i].Position, costLantern, 0);
                    if (solution.Score > bestLanternRemovalScore)
                    {
                        bestLanternRemovalPosition = previousState.Lanterns[i].Position;
                        bestLanternRemovalScore = solution.Score;
                    }
                    solution.PutLantern(previousState.Lanterns[i].Position, (Light)previousState.Lanterns[i].Color, costLantern);
                }

                for (int i = 0; i < previousState.Obstacles.Count; i++)
                {
                    if (!solution.RemoveObstacle(previousState.Obstacles[i].Position, costObstacle, 0))
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
                    if (!solution.RemoveMirror(previousState.Mirrors[i].Position, costMirror, 0))
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
                lightIntersections.PutMirror(bestMirrorPosition, bestMirrorSlash ? BoardField.MirrorSlash : BoardField.MirrorBackSlash, costMirror);
            }
            else if (bestObstacleScore >= bestLanternScore && bestObstacleScore >= bestMirrorScore && bestObstacleScore >= bestLanternRemovalScore && bestObstacleScore >= bestMirrorRemovalScore && bestObstacleScore >= bestObstacleRemovalScore)
            {
                previousState.PutObstacle(bestObstaclePosition, costObstacle);
                lightIntersections.PutObstacle(bestObstaclePosition, costObstacle);
            }
            else if (bestLanternRemovalScore >= bestLanternScore && bestLanternRemovalScore >= bestObstacleScore && bestLanternRemovalScore >= bestMirrorScore && bestLanternRemovalScore >= bestObstacleRemovalScore && bestLanternRemovalScore >= bestMirrorRemovalScore)
            {
                previousState.RemoveLantern(bestLanternRemovalPosition, costLantern, 0);
                removals++;
            }
            else if (bestObstacleRemovalScore >= bestLanternScore && bestObstacleRemovalScore >= bestObstacleScore && bestObstacleRemovalScore >= bestMirrorScore && bestObstacleRemovalScore >= bestMirrorRemovalScore && bestObstacleRemovalScore >= bestLanternRemovalScore)
            {
                previousState.RemoveObstacle(bestObstacleRemovalPosition, costObstacle, 0);
                lightIntersections.RemoveObstacle(bestObstacleRemovalPosition, costObstacle, 0);
                removals++;
            }
            else if (bestMirrorRemovalScore >= bestLanternScore && bestMirrorRemovalScore >= bestObstacleScore && bestMirrorRemovalScore >= bestMirrorScore && bestMirrorRemovalScore >= bestLanternRemovalScore && bestMirrorRemovalScore >= bestObstacleRemovalScore)
            {
                previousState.RemoveMirror(bestMirrorRemovalPosition, costObstacle, 0);
                lightIntersections.RemoveMirror(bestMirrorRemovalPosition, costObstacle, 0);
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
