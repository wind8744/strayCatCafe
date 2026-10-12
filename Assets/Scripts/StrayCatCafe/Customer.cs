using System;
using UnityEngine;

namespace StrayCatCafe
{
    /// <summary>
    /// 카페 손님. 문으로 들어와 카운터 앞 줄에 서서 커피나 간식을 주문하고, 주문을 받으면 값을 치르고
    /// 빈자리에 앉아 아이들을 구경한 뒤 팁을 두고 나갑니다. 너무 오래 기다리면 화를 내고 그냥 갑니다.
    /// </summary>
    public class Customer : MonoBehaviour
    {
        private enum State { ToQueue, Waiting, ToSeat, Sitting, Leaving }

        /// <summary>null 이면 고양이·강아지를 가리지 않습니다.</summary>
        public AnimalKind? Prefers { get; private set; }
        public MenuItem Order { get; private set; }
        public int SeatIndex { get; private set; } = -1;
        public bool Done { get; private set; }
        public bool Served { get; private set; }
        public bool Angry { get; private set; }
        public bool Seated => _state == State.Sitting;
        /// <summary>줄에 서 있거나 줄로 가는 중.</summary>
        public bool InQueue => _state == State.ToQueue || _state == State.Waiting;
        /// <summary>줄 맨 앞 칸에 도착해 주문을 기다리는 중.</summary>
        public bool ReadyToOrder => _state == State.Waiting;
        public float Patience { get; private set; }
        public float PatienceMax { get; private set; }
        public Vector2 Position => _walker.Position;

        private CharacterView _view;
        private CafeMap _map;
        private PathWalker _walker;
        private State _state;
        private float _timer, _petTimer;
        private Vector2Int _queueSpot;
        private Action<Customer> _onLeave;
        private Action<Customer> _onPet;

        public void Setup(CharacterSprites sprites, CafeMap map, MenuItem order, AnimalKind? prefers, float stay,
            float patience, Action<Customer> onLeave, Action<Customer> onPet)
        {
            _map = map;
            Order = order;
            Prefers = prefers;
            _onLeave = onLeave;
            _onPet = onPet;
            _timer = stay;
            Patience = PatienceMax = patience;
            _walker = new PathWalker(map, map.TileToWorld(map.Doors[0]), 3f);
            _view = gameObject.AddComponent<CharacterView>();
            _view.Setup(sprites);
            transform.position = _walker.Position;
            _state = State.ToQueue;
            _petTimer = 2f;
        }

        /// <summary>줄이 줄어들면 앞 칸으로 옮겨 섭니다.</summary>
        public void SetQueueSpot(Vector2Int spot)
        {
            if (!InQueue || spot == _queueSpot) return;
            _queueSpot = spot;
            _state = State.ToQueue;
            _walker.SetTarget(spot);
        }

        /// <summary>주문을 받았습니다. 자리가 있으면 앉고, 없으면 바로 나갑니다.</summary>
        public void Serve(int seatIndex)
        {
            if (Served) return;
            Served = true;
            SeatIndex = seatIndex;
            if (seatIndex >= 0)
            {
                _state = State.ToSeat;
                _walker.SetTarget(_map.Seats[seatIndex]);
            }
            else
            {
                Leave();
            }
        }

        /// <summary>기다리다 지쳐서 주문도 않고 나갑니다.</summary>
        public void LeaveAngry()
        {
            Angry = true;
            Leave();
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

        public void Tick(float dt)
        {
            switch (_state)
            {
                case State.ToQueue:
                    Patience -= dt;
                    _walker.Tick(dt);
                    if (_walker.Arrived) _state = State.Waiting;
                    break;

                case State.Waiting:
                    Patience -= dt;
                    break;

                case State.ToSeat:
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

                case State.Leaving:
                    _walker.Tick(dt);
                    if (_walker.Arrived) Done = true;
                    break;
            }

            transform.position = _walker.Position;
            _view.Animate(_walker.Moving, _walker.DirX, dt);
        }
    }
}
