using UnityEngine;

namespace StrayCatCafe
{
    /// <summary>사람 스프라이트 표시: 걷기 두 프레임, 앉기, 좌우 뒤집기, y 기준 정렬.</summary>
    public class CharacterView : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private CharacterSprites _frames;
        private float _animTime;
        private bool _sitting;

        public static int SortOrder(float y) => 100 - Mathf.RoundToInt(y * 10f);

        public void Setup(CharacterSprites frames)
        {
            _frames = frames;
            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = frames.Idle;
        }

        public void SetSitting(bool sitting)
        {
            _sitting = sitting;
            if (sitting) _sr.sprite = _frames.Sit;
        }

        public void Animate(bool moving, float dirX, float dt)
        {
            if (_sitting)
            {
                _sr.sprite = _frames.Sit;
            }
            else if (moving)
            {
                _animTime += dt * 8f;
                _sr.sprite = ((int)_animTime) % 2 == 0 ? _frames.Walk1 : _frames.Walk2;
            }
            else
            {
                _sr.sprite = _frames.Idle;
            }
            if (Mathf.Abs(dirX) > 0.01f) _sr.flipX = dirX > 0f;
            _sr.sortingOrder = SortOrder(transform.position.y);
        }
    }

    /// <summary>타일 길찾기로 움직이는 몸. 동물·손님이 함께 씁니다.</summary>
    public class PathWalker
    {
        private readonly TileGrid _grid;
        private System.Collections.Generic.List<Vector2Int> _path;
        private int _index;

        public Vector2 Position;
        public float Speed;
        public bool Moving { get; private set; }
        public float DirX { get; private set; }

        public PathWalker(TileGrid grid, Vector2 start, float speed)
        {
            _grid = grid;
            Position = start;
            Speed = speed;
        }

        public Vector2Int Tile => _grid.WorldToTile(Position);
        public bool Arrived => _path == null || _index >= _path.Count;

        public bool SetTarget(Vector2Int tile, System.Func<Vector2Int, bool> passable = null)
        {
            _path = _grid.FindPath(Tile, tile, passable);
            _index = 0;
            return _path != null;
        }

        public void Stop()
        {
            _path = null;
            Moving = false;
        }

        public void Tick(float dt)
        {
            Moving = false;
            if (Arrived) return;
            Vector2 goal = _grid.TileToWorld(_path[_index]);
            Vector2 delta = goal - Position;
            float step = Speed * dt;
            if (delta.magnitude <= step)
            {
                Position = goal;
                _index++;
            }
            else
            {
                Position += delta.normalized * step;
                DirX = delta.x;
            }
            Moving = true;
        }
    }
}
