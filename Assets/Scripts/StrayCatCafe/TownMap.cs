using System.Collections.Generic;
using UnityEngine;

namespace StrayCatCafe
{
    /// <summary>
    /// 카페 밖 동네. T 나무, g 풀밭, s 보도, r 도로, B 덤불, w 연못, h/H 집 지붕·벽, E 주인공 집 문,
    /// c/C 카페 지붕·벽, D 카페 문, S 간식 가게, n 벤치, l 가로등.
    /// 길고양이·강아지는 덤불·나무·물가 옆 풀밭에 숨어 있습니다.
    /// </summary>
    public class TownMap : TileGrid
    {
        public static readonly string[] DefaultLayout =
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TggggggggggggggggggggggggggggggggggT",
            "TgBggghhhhhhggggggggggghhhhhhgggBggT",
            "TgggggHHHHHHgggggBgggggHHHHHHggggggT",
            "TgTgggHHEHHHgggggggggggHHHHHHggTgggT",
            "TggggssssssssssssssssssssssssssggggT",
            "TggggrrrrrrrrrrrrrrrrrrrrrrrrrrggggT",
            "TggggssssssssssssssssssssssssssggggT",
            "TggggssggggggggggggggggggggggssggggT",
            "TgBggssgggcccccccccggggggggggssggggT",
            "TggggssgggCCCCCCCCCggggggggggssggggT",
            "TggggssgggCCCCCCCCCgggSSgggggssggggT",
            "TgTggssgggCCCCDDCCCggggggggggssgTggT",
            "TggggssssssssssssssssssssssssssggggT",
            "TggnggggggggBggggggggggBggggggnggggT",
            "TgggggggggggwwwwwgTggggggggggggggggT",
            "TgBgggggTggwwwwwwwgggggggggBgggggggT",
            "TggggggggggwwwwwwglgggggggggggggBggT",
            "TggTggggggggwwwwgggggggTgggggggggggT",
            "TgggggggBgggggggggggggggggggBggggggT",
            "TggggggggggggggggggggggggggggggggggT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        /// <summary>문에서 이 거리(칸) 안에는 아이들이 숨지 않습니다.</summary>
        public const int QuietRadius = 5;

        public List<Vector2Int> CafeDoors { get; } = new List<Vector2Int>();
        public List<Vector2Int> HomeDoors { get; } = new List<Vector2Int>();
        /// <summary>집에서 나온 주인이 서는 보도 칸.</summary>
        public Vector2Int HomeExitSpot => HomeDoors[0] + Vector2Int.down;
        public List<Vector2Int> Shop { get; } = new List<Vector2Int>();
        public List<Vector2Int> HidingSpots { get; } = new List<Vector2Int>();
        /// <summary>카페에서 나온 주인이 서는 보도 칸.</summary>
        public Vector2Int ExitSpot => CafeDoors[0] + Vector2Int.down;

        public TownMap() : this(DefaultLayout) { }

        public TownMap(string[] layout) : base(layout)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var p = new Vector2Int(x, y);
                    switch (Tiles[x, y])
                    {
                        case 'D': CafeDoors.Add(p); break;
                        case 'E': HomeDoors.Add(p); break;
                        case 'S': Shop.Add(p); break;
                    }
                }
            }

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var p = new Vector2Int(x, y);
                    if (Tiles[x, y] != 'g' || !NextToCover(p)) continue;
                    if (Mathf.Abs(p.x - ExitSpot.x) + Mathf.Abs(p.y - ExitSpot.y) <= QuietRadius) continue;
                    HidingSpots.Add(p);
                }
            }
        }

        private bool NextToCover(Vector2Int p)
        {
            Vector2Int[] around = { p + Vector2Int.up, p + Vector2Int.down, p + Vector2Int.left, p + Vector2Int.right };
            foreach (Vector2Int q in around)
            {
                char t = TileAt(q.x, q.y);
                if (t == 'B' || t == 'T' || t == 'w') return true;
            }
            return false;
        }

        public override char TileAt(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return 'T';
            return Tiles[x, y];
        }

        public override bool IsWalkable(Vector2Int p)
        {
            char t = TileAt(p.x, p.y);
            return t == 'g' || t == 's' || t == 'r' || t == 'D' || t == 'E';
        }

        public bool IsCafeDoor(Vector2Int p) => TileAt(p.x, p.y) == 'D';
        public bool IsHomeDoor(Vector2Int p) => TileAt(p.x, p.y) == 'E';
        public bool IsWater(Vector2Int p) => TileAt(p.x, p.y) == 'w';

        public bool IsNextToShop(Vector2Int p)
        {
            foreach (Vector2Int s in Shop) if (CafeMap.Adjacent(p, s)) return true;
            return false;
        }
    }
}
