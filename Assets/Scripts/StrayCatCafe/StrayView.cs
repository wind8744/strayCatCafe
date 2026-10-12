using UnityEngine;

namespace StrayCatCafe
{
    /// <summary>
    /// 동네에 숨어 있는 길고양이·강아지. 경계심이 차면 도망가고, 친해지면 주인을 졸졸 따라옵니다.
    /// </summary>
    public class StrayView : MonoBehaviour
    {
        public enum State { Hiding, Fleeing, Following }

        public Species Species { get; private set; }
        public Encounter Encounter { get; private set; }
        public State Current { get; private set; }
        public bool Gone { get; private set; }
        public Vector2 Position => transform.position;

        private AnimalSprites _frames;
        private SpriteRenderer _sr, _bubble;
        private Sprite _alert, _heart;
        private TownMap _map;
        private Vector2 _home;
        private float _timer, _animTime, _bubbleTimer;
        private Vector2 _fleeDir;
        private bool _moving;
        private float _dirX;

        public void Setup(Species species, Encounter encounter, AnimalSprites frames, TownMap map, Vector2Int spot,
            Sprite alert, Sprite heart)
        {
            Species = species;
            Encounter = encounter;
            _frames = frames;
            _map = map;
            _alert = alert;
            _heart = heart;
            _home = map.TileToWorld(spot);
            transform.position = _home;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = frames.Sleep;
            var bubble = new GameObject("Bubble");
            bubble.transform.SetParent(transform, false);
            bubble.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            _bubble = bubble.AddComponent<SpriteRenderer>();
            _bubble.enabled = false;
            _timer = 2f;
        }

        public void StartFollowing()
        {
            Current = State.Following;
            _bubble.sprite = _heart;
            _bubble.enabled = true;
            _bubbleTimer = 2f;
        }

        public void Flee(Vector2 from)
        {
            Current = State.Fleeing;
            _fleeDir = ((Vector2)transform.position - from).normalized;
            if (_fleeDir.sqrMagnitude < 0.01f) _fleeDir = Vector2.right;
            _timer = 1.6f;
            _bubble.sprite = _alert;
            _bubble.enabled = true;
            _bubbleTimer = 1.6f;
        }

        public void ShowHeart()
        {
            _bubble.sprite = _heart;
            _bubble.enabled = true;
            _bubbleTimer = 1f;
        }

        public void Tick(float dt, Vector2 player, bool sneaking)
        {
            float dist = Vector2.Distance(player, transform.position);
            _moving = false;
            switch (Current)
            {
                case State.Hiding:
                    Encounter.Observe(dist, sneaking, dt);
                    if (Encounter.Fled)
                    {
                        Flee(player);
                        break;
                    }
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        // 제자리에서 꼼지락거리기
                        _timer = 1.5f + Random.value * 2f;
                        _dirX = Random.value < 0.5f ? -1f : 1f;
                    }
                    if (_bubbleTimer <= 0f)
                    {
                        _bubble.sprite = _alert;
                        _bubble.enabled = Encounter.Wariness > 0.6f && Mathf.Repeat(Time.time, 0.6f) < 0.4f;
                    }
                    break;

                case State.Fleeing:
                    _timer -= dt;
                    Vector2 next = (Vector2)transform.position + _fleeDir * 6f * dt;
                    if (_map.IsWalkable(_map.WorldToTile(next))) transform.position = next;
                    else _fleeDir = new Vector2(-_fleeDir.y, _fleeDir.x);
                    _moving = true;
                    _dirX = _fleeDir.x;
                    if (_timer <= 0f) Gone = true;
                    break;

                case State.Following:
                    if (dist > 1.3f)
                    {
                        Vector2 dir = (player - (Vector2)transform.position).normalized;
                        Vector2 step = (Vector2)transform.position + dir * 4.5f * dt;
                        if (_map.IsWalkable(_map.WorldToTile(step)) || dist > 4f) transform.position = step;
                        _moving = true;
                        _dirX = dir.x;
                    }
                    break;
            }

            if (Current == State.Hiding && Encounter.Bond <= 0f && !_moving) _sr.sprite = Species.Personality == Personality.Lazy ? _frames.Sleep : _frames.Idle;
            else if (_moving)
            {
                _animTime += dt * 8f;
                _sr.sprite = ((int)_animTime) % 2 == 0 ? _frames.Walk : _frames.Idle;
            }
            else _sr.sprite = _frames.Idle;
            if (Mathf.Abs(_dirX) > 0.01f) _sr.flipX = _dirX > 0f;
            _sr.sortingOrder = CharacterView.SortOrder(transform.position.y);
            _bubble.sortingOrder = _sr.sortingOrder + 1;

            if (_bubbleTimer > 0f)
            {
                _bubbleTimer -= dt;
                if (_bubbleTimer <= 0f) _bubble.enabled = false;
            }
        }
    }
}
