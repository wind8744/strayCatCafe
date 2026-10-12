using System;
using UnityEngine;

namespace StrayCatCafe
{
    /// <summary>
    /// 카페 손님. 문으로 들어와 빈자리에 앉아 아이들을 구경하고, 시간이 지나면 돈을 내고 나갑니다.
    /// 아이가 한 마리도 없으면 잠깐 둘러보다 그냥 나갑니다.
    /// </summary>
    public class Customer : MonoBehaviour
    {
        private enum State { Entering, Sitting, Browsing, Leaving }

        /// <summary>null 이면 고양이·강아지를 가리지 않습니다.</summary>
        public AnimalKind? Prefers { get; private set; }
        public int SeatIndex { get; private set; } = -1;
        public bool Done { get; private set; }
        public bool Seated => _state == State.Sitting;
        public Vector2 Position => _walker.Position;

        private CharacterView _view;
        private CafeMap _map;
        private PathWalker _walker;
        private State _state;
        private float _timer, _petTimer;
        private Action<Customer> _onLeave;
        private Action<Customer> _onPet;

        public void Setup(CharacterSprites sprites, CafeMap map, int seatIndex, AnimalKind? prefers, float stay,
            Action<Customer> onLeave, Action<Customer> onPet)
        {
            _map = map;
            SeatIndex = seatIndex;
            Prefers = prefers;
            _onLeave = onLeave;
            _onPet = onPet;
            _walker = new PathWalker(map, map.TileToWorld(map.Doors[0]), 3f);
            _view = gameObject.AddComponent<CharacterView>();
            _view.Setup(sprites);
            transform.position = _walker.Position;

            if (seatIndex >= 0)
            {
                _state = State.Entering;
                _walker.SetTarget(map.Seats[seatIndex]);
                _timer = stay;
            }
            else
            {
                _state = State.Browsing;
                _walker.SetTarget(map.Spawn + Vector2Int.up * 2);
                _timer = 3f;
            }
            _petTimer = 2f;
        }

        public void Tick(float dt)
        {
            switch (_state)
            {
                case State.Entering:
                    _walker.Tick(dt);
                    if (_walker.Arrived)
                    {
                        _state = State.Sitting;
                        _view.SetSitting(true);
                    }
                    break;

                case State.Sitting:
                    _timer -= dt;
                    _petTimer -= dt;
                    if (_petTimer <= 0f)
                    {
                        _petTimer = 3.5f;
                        _onPet?.Invoke(this);
                    }
                    if (_timer <= 0f) Leave();
                    break;

                case State.Browsing:
                    _walker.Tick(dt);
                    _timer -= dt;
                    if (_timer <= 0f) Leave();
                    break;

                case State.Leaving:
                    _walker.Tick(dt);
                    if (_walker.Arrived) Done = true;
                    break;
            }

            transform.position = _walker.Position;
            _view.Animate(_walker.Moving, _walker.DirX, dt);
        }

        /// <summary>영업이 끝나면 앉아 있던 손님도 일어납니다.</summary>
        public void Leave()
        {
            if (_state == State.Leaving) return;
            bool paid = _state == State.Sitting;
            _state = State.Leaving;
            _view.SetSitting(false);
            _walker.SetTarget(_map.Doors[0]);
            if (paid) _onLeave?.Invoke(this);
        }
    }
}
