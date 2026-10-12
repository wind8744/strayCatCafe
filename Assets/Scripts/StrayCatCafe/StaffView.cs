using UnityEngine;

namespace StrayCatCafe
{
    /// <summary>알바생. 바리스타는 카운터 뒤에 서 있고, 돌봄 알바는 카페 안을 돌아다닙니다.</summary>
    public class StaffView : MonoBehaviour
    {
        private CharacterView _view;
        private CafeMap _map;
        private PathWalker _walker;
        private bool _wander;
        private float _timer;
        private System.Random _rng;

        public Vector2 Position => _walker.Position;
        public bool Arrived => _walker.Arrived;

        public void Setup(CharacterSprites sprites, CafeMap map, Vector2Int spot, bool wander, int seed)
        {
            _map = map;
            _wander = wander;
            _rng = new System.Random(seed);
            _walker = new PathWalker(map, map.TileToWorld(spot), 2.6f);
            _view = gameObject.AddComponent<CharacterView>();
            _view.Setup(sprites);
            transform.position = _walker.Position;
            _timer = 2f;
        }

        public void GoTo(Vector2Int tile) => _walker.SetTarget(tile);

        public void Tick(float dt)
        {
            _walker.Tick(dt);
            if (_wander && _walker.Arrived)
            {
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _timer = 2f + (float)_rng.NextDouble() * 3f;
                    _walker.SetTarget(_map.Floor[_rng.Next(_map.Floor.Count)]);
                }
            }
            transform.position = _walker.Position;
            _view.Animate(_walker.Moving, _walker.DirX, dt);
        }
    }
}
