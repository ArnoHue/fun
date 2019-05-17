using System;
using System.Collections.Generic;
using SoftwareArchitects.Battleships;

namespace Battleships
{

  [Player(Name = "Black Buccaneer")]
  public class BlackBuccaneer : Player
  {
    private class ShipPositionOption
    {
      private int free;
      private bool done;
      private Direction dir;
      private Coordinates nextCoord;

      public ShipPositionOption(Direction posDir)
      {
        dir = posDir;
        free = 0;
        done = false;
        nextCoord = null;
      }

      public void Reset()
      {
        free = 0;
        done = false;
        nextCoord = null;
      }

      public void MarkInvalid()
      {
        free = 0;
        done = true;
        nextCoord = null;
      }

      public Coordinates NextCoord
      {
        get { return nextCoord; }
        set { nextCoord = value; }
      }

      public int Free
      {
        get { return free; }
        set { free = value; }
      }

      public bool Done
      {
        get { return done; }
        set { done = value; }
      }

      public Direction Dir
      {
        get { return dir; }
      }

    }

    private class Direction
    {
      private Orientation orient;
      private int dX;
      private int dY;

      public Direction(Orientation or, int x, int y)
      {
        orient = or;
        dX = x;
        dY = y;
      }

      public Orientation Orient
      {
        get { return orient; }
      }

      public int DX
      {
        get { return dX; }
      }

      public int DY
      {
        get { return dY; }
      }
    }

    private class SinkTryResult
    {
      private Direction dir;
      private Coordinates sinkCoord;

      public SinkTryResult(Coordinates sinkCoordParam, Direction dirParam)
      {
        sinkCoord = sinkCoordParam;
        dir = dirParam;
      }

      public Coordinates SinkCoord
      {
        get { return sinkCoord; }
        set { sinkCoord = value; }
      }

      public Direction Dir
      {
        get { return dir; }
        set { dir = value; }
      }
    }

    private enum Orientation : int
    {
      EAST = 0,
      WEST = 1,
      NORTH = 2,
      SOUTH = 3
    }

    private enum Mode { ScanMode, SinkMode };

    private static readonly IDictionary<Orientation, Direction> DIRECTIONS = new Dictionary<Orientation, Direction>();
    private const int FIELD_DIM_X = 10;
    private const int FIELD_DIM_Y = 10;

    private const int EAST_IDX = (int)Orientation.EAST;
    private const int WEST_IDX = (int)Orientation.WEST;
    private const int SOUTH_IDX = (int)Orientation.SOUTH;
    private const int NORTH_IDX = (int)Orientation.NORTH;

    private Random rnd = new Random();
    private Mode mode = Mode.ScanMode;
    private Coordinates sinkCoord = null;
    private bool[,] impossibleCoords = new bool[10, 10];

    private int seekedShipSize = 5;
    private int hitCount = 0;

    private int[] sinksRequired = new int[] { 0, 0, 1, 2, 1, 1 };

    static ThePrivateer()
    {
      DIRECTIONS[Orientation.EAST] = new Direction(Orientation.EAST, 1, 0);
      DIRECTIONS[Orientation.WEST] = new Direction(Orientation.WEST, -1, 0);
      DIRECTIONS[Orientation.SOUTH] = new Direction(Orientation.SOUTH, 0, 1);
      DIRECTIONS[Orientation.NORTH] = new Direction(Orientation.NORTH, 0, -1);
    }

    public ThePrivateer()
    {
    }

    public override void Move(PlayingField playingField)
    {
      if (mode == Mode.ScanMode)
      {
        Coordinates coord = GetNextScanCoord(playingField);
        if (playingField.Fire(coord.X, coord.Y) == State.HitShip)
        {
          sinkCoord = coord;
          mode = Mode.SinkMode;
          hitCount = 1;
        }
      }
      else if (mode == Mode.SinkMode)
      {
        SinkTryResult sinkTry = GetNextSinkTry(playingField);

        State res = playingField.Fire(sinkTry.SinkCoord.X, sinkTry.SinkCoord.Y);

        if (res == State.HitShip || res == State.SunkShip)
        {
          sinkCoord = sinkTry.SinkCoord;
          hitCount++;

          if (res == State.SunkShip)
          {

            sinksRequired[hitCount] = sinksRequired[hitCount] - 1;

            int sunkX = sinkCoord.X;
            int sunkY = sinkCoord.Y;

            for (int i = 0; i < hitCount; i++)
            {
              SetImpossibleCoord(sunkX - 1, sunkY - 1);
              SetImpossibleCoord(sunkX - 1, sunkY);
              SetImpossibleCoord(sunkX - 1, sunkY + 1);
              SetImpossibleCoord(sunkX, sunkY - 1);
              SetImpossibleCoord(sunkX, sunkY);
              SetImpossibleCoord(sunkX, sunkY + 1);
              SetImpossibleCoord(sunkX + 1, sunkY - 1);
              SetImpossibleCoord(sunkX + 1, sunkY);
              SetImpossibleCoord(sunkX + 1, sunkY + 1);

              sunkX = sunkX - sinkTry.Dir.DX;
              sunkY = sunkY - sinkTry.Dir.DY;
            }

            while (seekedShipSize > 0 && sinksRequired[seekedShipSize] == 0)
            {
              seekedShipSize--;
            }

            hitCount = 0;
            sinkCoord = null;
            mode = Mode.ScanMode;
          }
        }
      }
    }

    private ShipPositionOption[] CreateShipPositionOptions()
    {
      ShipPositionOption[] options = new ShipPositionOption[4];
      options[EAST_IDX] = new ShipPositionOption(DIRECTIONS[Orientation.EAST]);
      options[WEST_IDX] = new ShipPositionOption(DIRECTIONS[Orientation.WEST]);
      options[SOUTH_IDX] = new ShipPositionOption(DIRECTIONS[Orientation.SOUTH]);
      options[NORTH_IDX] = new ShipPositionOption(DIRECTIONS[Orientation.NORTH]);
      return options;
    }

    private Coordinates GetNextScanCoord(PlayingField playingField)
    {
      ShipPositionOption[] options = CreateShipPositionOptions();
      Coordinates bestCoord = null;
      int bestCnt = 0;
      int bestNeighborFact = 0;
      bool bestMatching = false;

      for (int x = 0; x < FIELD_DIM_X; x++)
      {
        for (int y = 0; y < FIELD_DIM_Y; y++)
        {
          if (IsUnknown(playingField, x, y))
          {
            int cnt = 0;
            int neighborFact = 0;

            for (int shipSize = 0; shipSize < sinksRequired.Length; shipSize++)
            {
              if (sinksRequired[shipSize] > 0)
              {
                for (int optIdx = 0; optIdx < options.Length; optIdx++)
                {
                  ShipPositionOption option = options[optIdx];
                  option.Reset();
                  for (int d = 1; d < shipSize && !option.Done; d++)
                  {
                    if (IsUnknown(playingField, x + d * option.Dir.DX, y + d * option.Dir.DY))
                    {
                      option.Free = option.Free + 1;
                    }
                    else
                    {
                      option.Done = true;
                    }

                  }

                }
                int cntHor = Math.Max(0, options[EAST_IDX].Free + options[WEST_IDX].Free + 2 - shipSize);
                int cntVer = Math.Max(0, options[SOUTH_IDX].Free + options[NORTH_IDX].Free + 2 - shipSize);
                cnt += cntHor + cntVer;

              }

            }

            foreach (Direction dir1 in DIRECTIONS.Values)
            {
              int x1 = x + dir1.DX;
              int y1 = y + dir1.DY;
              if (IsUnknown(playingField, x1, y1))
              {
                foreach (Direction dir2 in DIRECTIONS.Values)
                {
                  int x2 = x1 + dir2.DX;
                  int y2 = y1 + dir2.DY;
                  if (x2 != x || y2 != y)
                  {
                    if (IsWaterOrImpossible(playingField, x2, y2))
                    {
                      neighborFact++;
                    }
                  }
                }
              }
            }

            bool matching = MatchesScanPattern(x, y);
            if (cnt > bestCnt || (cnt == bestCnt && (neighborFact > bestNeighborFact || (neighborFact == bestNeighborFact && ((!bestMatching && matching) || (bestMatching == matching && rnd.Next(2) == 1))))))
            {
              bestCnt = cnt;
              bestNeighborFact = neighborFact;
              bestMatching = matching;
              bestCoord = new Coordinates(x, y);
            }
          }
        }
      }
      return bestCoord;
    }

    private SinkTryResult GetNextSinkTry(PlayingField playingField)
    {
      ShipPositionOption[] options = CreateShipPositionOptions();

      for (int i = 1; i < seekedShipSize && (!options[EAST_IDX].Done || !options[WEST_IDX].Done || !options[SOUTH_IDX].Done || !options[NORTH_IDX].Done); i++)
      {
        for (int optIdx = 0; optIdx < options.Length; optIdx++)
        {
          ShipPositionOption option = options[optIdx];
          if (!option.Done)
          {
            Coordinates nextCoord = new Coordinates(sinkCoord.X + i * option.Dir.DX, sinkCoord.Y + i * option.Dir.DY);
            if (!IsValidCoord(nextCoord) || impossibleCoords[nextCoord.X, nextCoord.Y])
            {
              option.Done = true;
            }
            else
            {
              State state = playingField.GetState(nextCoord.X, nextCoord.Y);
              if (state == State.Unknown)
              {
                if (option.NextCoord == null)
                {
                  option.NextCoord = nextCoord;
                }
                option.Free = option.Free + 1;
              }
              else if (state == State.HitShip)
              {
                if (option.Dir.Orient == Orientation.EAST || option.Dir.Orient == Orientation.WEST)
                {
                  options[SOUTH_IDX].MarkInvalid();
                  options[NORTH_IDX].MarkInvalid();
                }
                else
                {
                  options[EAST_IDX].MarkInvalid();
                  options[WEST_IDX].MarkInvalid();
                }
              }
              else
              {
                option.Done = true;
              }
            }
          }
        }
      }

      ShipPositionOption bestOpt;
      ShipPositionOption option1;
      ShipPositionOption option2;

      int horFree = options[EAST_IDX].Free + options[WEST_IDX].Free;
      int verFree = options[SOUTH_IDX].Free + options[NORTH_IDX].Free;
      int minHorFree = Math.Min(options[EAST_IDX].Free, options[WEST_IDX].Free);
      int minVerFree = Math.Min(options[SOUTH_IDX].Free, options[NORTH_IDX].Free);

      if (horFree > verFree || (horFree == verFree && (minHorFree > minVerFree || (minHorFree == minVerFree && rnd.Next(2) == 1))))
      {
        option1 = options[EAST_IDX];
        option2 = options[WEST_IDX];
      }
      else
      {
        option1 = options[SOUTH_IDX];
        option2 = options[NORTH_IDX];
      }

      if (option1.Free > option2.Free)
      {
        bestOpt = option1;
      }
      else if (option2.Free > option1.Free)
      {
        bestOpt = option2;
      }
      else
      {
        bestOpt = rnd.Next(2) == 1 ? option1 : option2;
      }

      return new SinkTryResult(bestOpt.NextCoord, bestOpt.Dir);
    }

    private bool IsUnknown(PlayingField playingField, int x, int y)
    {
      return IsValidCoord(x, y) && !impossibleCoords[x, y] && playingField.GetState(x, y) == State.Unknown;
    }

    private bool IsWater(PlayingField playingField, int x, int y)
    {
      return IsValidCoord(x, y) && playingField.GetState(x, y) == State.Water;
    }

    private bool IsWaterOrImpossible(PlayingField playingField, int x, int y)
    {
      return IsValidCoord(x, y) && (impossibleCoords[x, y] || playingField.GetState(x, y) == State.Water);
    }

    private bool MatchesScanPattern(Coordinates coord)
    {
      return coord != null && MatchesScanPattern(coord.X, coord.Y);
    }

    private bool MatchesScanPattern(int x, int y)
    {
      return (x + y) % 2 == 0;
    }

    private bool IsValidCoord(Coordinates coord)
    {
      return IsValidCoord(coord.X, coord.Y);
    }

    private bool IsValidCoord(int x, int y)
    {
      return x >= 0 && x < FIELD_DIM_X && y >= 0 && y < FIELD_DIM_Y;
    }

    private void SetImpossibleCoord(int x, int y)
    {
      if (IsValidCoord(x, y))
      {
        impossibleCoords[x, y] = true;
      }
    }

  }
}
