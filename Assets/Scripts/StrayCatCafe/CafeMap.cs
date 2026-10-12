using System.Collections.Generic;
using UnityEngine;

namespace StrayCatCafe
{
    /// <summary>
    /// 카페 안 배치도. # 벽, w 창문, . 바닥, D 문, k 카운터, c 손님 자리, b 밥그릇, t 캣타워, B 방석.
    /// </summary>
    public class CafeMap : TileGrid
    {
        public static readonly string[] DefaultLayout =
        {
            "##################",
            "#w....w....w....w#",
            "#.t..............#",
            "#....c.c....c.c..#",
            "#.b..............#",
            "#....c.c....c.c..#",
            "#.B..............#",
            "#....c.c....c.c..#",
            "#.B......kkk.....#",
            "#................#",
            "#................#",
            "########DD########",
        };

        public List<Vector2Int> Seats { get; } = new List<Vector2Int>();
        public List<Vector2Int> Beds { get; } = new List<Vector2Int>();
        public List<Vector2Int> Counter { get; } = new List<Vector2Int>();
        public List<Vector2Int> Doors { get; } = new List<Vector2Int>();
        /// <summary>동물이 어슬렁거릴 수 있는 바닥 칸.</summary>
        public List<Vector2Int> Floor { get; } = new List<Vector2Int>();
        public Vector2Int Bowl { get; private set; }
        public Vector2Int Tower { get; private set; }
        /// <summary>문으로 들어온 사람이 처음 서는 칸.</summary>
        public Vector2Int Spawn => Doors[0] + Vector2Int.up;

        public CafeMap() : this(DefaultLayout) { }

        public CafeMap(string[] layout) : base(layout)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var p = new Vector2Int(x, y);
                    switch (Tiles[x, y])
                    {
                        case 'c': Seats.Add(p); break;
                        case 'B': Beds.Add(p); Floor.Add(p); break;
                        case 'k': Counter.Add(p); break;
                        case 'D': Doors.Add(p); break;
                        case 'b': Bowl = p; break;
                        case 't': Tower = p; break;
                        case '.': if (y > 1) Floor.Add(p); break;
                    }
                }
            }
        }

        public override char TileAt(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return '#';
            return Tiles[x, y];
        }

        public override bool IsWalkable(Vector2Int p)
        {
            char t = TileAt(p.x, p.y);
            return t == '.' || t == 'c' || t == 'B' || t == 'D';
        }

        public bool IsDoor(Vector2Int p) => TileAt(p.x, p.y) == 'D';

        public static bool Adjacent(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;

        public bool IsNextToCounter(Vector2Int p)
        {
            foreach (Vector2Int c in Counter) if (Adjacent(p, c)) return true;
            return false;
        }

        public bool IsNextToBowl(Vector2Int p) => Adjacent(p, Bowl);

        /// <summary>밥그릇 옆에서 동물이 서는 칸 (아래쪽 우선).</summary>
        public Vector2Int BowlSpot
        {
            get
            {
                Vector2Int[] around = { Bowl + Vector2Int.down, Bowl + Vector2Int.right, Bowl + Vector2Int.up, Bowl + Vector2Int.left };
                foreach (Vector2Int p in around) if (IsWalkable(p)) return p;
                return Bowl + Vector2Int.down;
            }
        }

        public Vector2Int TowerSpot => Tower + Vector2Int.down;
    }
}
