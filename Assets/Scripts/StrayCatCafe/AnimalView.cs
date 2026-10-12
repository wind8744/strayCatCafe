using System;
using UnityEngine;

namespace StrayCatCafe
{
    /// <summary>
    /// 카페 안에서 사는 아이. 어슬렁거리고, 방석에서 자고, 배고프면 밥그릇으로 갑니다.
    /// </summary>
    public class AnimalView : MonoBehaviour
    {
        private enum State { Idle, Walk, Sleep, GoEat, Eat }

        public Animal Data { get; private set; }

        private AnimalSprites _frames;
        private SpriteRenderer _sr, _bubble;
        private CafeMap _map;
        private PathWalker _walker;
        private Func<bool> _bowlHasFood, _eatFromBowl;
        private Sprite _zzz, _heart;
        private State _state;
        private float _timer, _animTime, _bubbleTimer;
        private bool _toBed;
        private System.Random _rng;

        public Vector2 Position => _walker.Position;
        public bool Asleep => _state == State.Sleep;

        public void Setup(Animal data, AnimalSprites frames, CafeMap map, Vector2Int startTile,
            Func<bool> bowlHasFood, Func<bool> eatFromBowl, Sprite zzz, Sprite heart, int seed)
        {
            Data = data;
            _frames = frames;
            _map = map;
            _bowlHasFood = bowlHasFood;
            _eatFromBowl = eatFromBowl;
            _zzz = zzz;
            _heart = heart;
            _rng = new System.Random(seed);
            _walker = new PathWalker(map, map.TileToWorld(startTile), 2.2f);
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = frames.Idle;
            var bubble = new GameObject("Bubble");
            bubble.transform.SetParent(transform, false);
            bubble.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            _bubble = bubble.AddComponent<SpriteRenderer>();
            _bubble.enabled = false;
            _timer = 1f + (float)_rng.NextDouble() * 2f;
            transform.position = _walker.Position;
        }

        public void ShowHeart()
        {
            _bubble.sprite = _heart;
            _bubble.enabled = true;
            _bubbleTimer = 1.2f;
        }

        public void Tick(float dt, bool cushion)
        {
            Data.Tick(dt, cushion);
            _timer -= dt;

            switch (_state)
            {
                case State.Idle:
                    if (_timer > 0f) break;
                    Decide();
                    break;

                case State.Walk:
                case State.GoEat:
                    _walker.Tick(dt);
                    if (!_walker.Arrived) break;
                    if (_state == State.GoEat)
                    {
                        _state = State.Eat;
                        _timer = 2f;
                    }
                    else if (_toBed)
                    {
                        _state = State.Sleep;
                        _timer = 8f + (float)_rng.NextDouble() * 8f;
                    }
                    else
                    {
                        _state = State.Idle;
                        _timer = 1.5f + (float)_rng.NextDouble() * 3f;
                    }
                    break;

                case State.Sleep:
                    if (_timer > 0f) break;
                    _state = State.Idle;
                    _timer = 0.5f;
                    break;

                case State.Eat:
                    if (_timer > 0f) break;
                    if (_eatFromBowl()) Data.Eat();
                    _state = State.Idle;
                    _timer = 2f;
                    break;
            }

            // 그림
            if (_state == State.Sleep) _sr.sprite = _frames.Sleep;
            else if (_walker.Moving)
            {
                _animTime += dt * 6f;
                _sr.sprite = ((int)_animTime) % 2 == 0 ? _frames.Walk : _frames.Idle;
            }
            else _sr.sprite = _frames.Idle;
            if (_walker.Moving && Mathf.Abs(_walker.DirX) > 0.01f) _sr.flipX = _walker.DirX > 0f;

            transform.position = _walker.Position;
            _sr.sortingOrder = CharacterView.SortOrder(_walker.Position.y);
            _bubble.sortingOrder = _sr.sortingOrder + 1;

            if (_bubbleTimer > 0f)
            {
                _bubbleTimer -= dt;
                if (_bubbleTimer <= 0f) _bubble.enabled = false;
            }
            else if (_state == State.Sleep)
            {
                _bubble.sprite = _zzz;
                _bubble.enabled = Mathf.Repeat(Time.time, 1.2f) < 0.8f;
            }
            else if (Data.Starving)
            {
                _bubble.enabled = false;
            }
        }

        private void Decide()
        {
            _toBed = false;
            if (Data.Hungry && _bowlHasFood())
            {
                if (_walker.SetTarget(_map.BowlSpot))
                {
                    _state = State.GoEat;
                    return;
                }
            }

            double roll = _rng.NextDouble();
            bool lazy = Data.Species.Personality == Personality.Lazy;
            if (roll < (lazy ? 0.5 : 0.2) && _map.Beds.Count > 0)
            {
                Vector2Int bed = _map.Beds[_rng.Next(_map.Beds.Count)];
                if (_walker.SetTarget(bed))
                {
                    _toBed = true;
                    _state = State.Walk;
                    return;
                }
            }
            if (roll < 0.35 && Data.Species.Kind == AnimalKind.Cat && _walker.SetTarget(_map.TowerSpot))
            {
                _state = State.Walk;
                return;
            }

            Vector2Int target = _map.Floor[_rng.Next(_map.Floor.Count)];
            if (_walker.SetTarget(target)) _state = State.Walk;
            else _timer = 1f;
        }
    }
}
