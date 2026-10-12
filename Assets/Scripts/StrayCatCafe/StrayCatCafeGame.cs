using System.Collections.Generic;
using UnityEngine;

namespace StrayCatCafe
{
    /// <summary>
    /// 길고양이 카페 진행 관리. 주인공의 집에서 시작해 첫 산책에서 고양이가 따라오고, 동네 풀숲·물가를 돌며
    /// 아이들을 모아 빈 가게를 빌려 카페를 엽니다. 카페에서는 손님에게 커피·간식을 팔아 돈을 벌고, 알바생을
    /// 고용하면 주인이 밖에 나가 있는 동안에도 가게가 돌아갑니다. 집·카페·동네는 같은 씬의 떨어진 자리에 있고
    /// 문을 드나들 때 카메라가 옮겨 갑니다. 씬에는 이 컴포넌트와 메인 카메라만 있으면 됩니다.
    /// </summary>
    public class StrayCatCafeGame : MonoBehaviour
    {
        [Header("Day")]
        [SerializeField] private float _dayLengthSeconds = 150f;
        [SerializeField] private int _openHour = 9;
        [SerializeField] private int _closeHour = 21;
        [Header("Player")]
        [SerializeField] private float _playerSpeed = 5f;
        [SerializeField] private float _sneakSpeed = 1.8f;
        [SerializeField] private float _serveSeconds = 1.2f;
        [Header("Town")]
        [SerializeField] private int _maxStrays = 4;
        [SerializeField] private float _strayRespawnSeconds = 25f;
        [SerializeField, Range(0f, 1f)] private float _injuredChance = 0.25f;
        [Header("Cafe")]
        [SerializeField] private int _maxCustomers = 10;
        [SerializeField] private float _patienceSeconds = 26f;
        [SerializeField] private float _baristaSeconds = 2.5f;
        [SerializeField] private float _caretakerSeconds = 8f;

        private static readonly Vector2 HomeOrigin = new Vector2(0f, 40f);
        private static readonly Vector2 TownOrigin = new Vector2(100f, 0f);

        private enum State { Open, Closing, Summary }
        private enum Panel { None, Shop, Office, Collection }
        private enum Place { Home, Cafe, Town }

        private struct FloatingText
        {
            public Vector2 Pos;
            public string Text;
            public Color Color;
            public float Age;
        }

        private const string SaveKey = "StrayCatCafe.Save";
        private const float TextSeconds = 1.3f;
        private const float InteractRange = 1.7f;
        private const int CollectionCols = 8;
        private static readonly Color Ink = new Color(0.3f, 0.19f, 0.15f);
        private static readonly Color Good = new Color(0.2f, 0.55f, 0.3f);
        private static readonly Color Bad = new Color(0.82f, 0.25f, 0.28f);
        private static readonly Color Gold = new Color(0.85f, 0.55f, 0.1f);
        private static readonly Color Love = new Color(0.9f, 0.35f, 0.45f);

        private CafeMap _home, _cafe;
        private TownMap _town;
        private CafeArt _art;
        private CafeEconomy _eco;
        private CafeAudio _audio;
        private Camera _cam;
        private System.Random _rng;

        private GameObject _player;
        private CharacterView _playerView;
        private Vector2 _playerPos;
        private Place _place;
        private bool _sneaking;

        private readonly List<AnimalView> _animals = new List<AnimalView>();
        private readonly List<Customer> _customers = new List<Customer>();
        private readonly List<Customer> _queue = new List<Customer>();
        private readonly List<StrayView> _strays = new List<StrayView>();
        private readonly List<StrayView> _followers = new List<StrayView>();
        private readonly List<FloatingText> _texts = new List<FloatingText>();
        private bool[] _seatTaken;
        private int _bowlFood;
        private SpriteRenderer _bowlSprite, _homeBowlSprite, _towerSprite;
        private StaffView _barista, _caretaker;

        private State _state;
        private float _hour, _customerTimer, _strayTimer, _doorNagTimer, _serveTimer, _baristaTimer, _caretakerTimer;
        private int _seed;
        private Panel _panel;
        private int _cursor, _selectedSnack = 1, _collectionCursor;
        private string _prompt, _banner;
        private float _bannerTimer;
        private GUIStyle _panelStyle, _labelStyle, _titleStyle, _floatStyle, _smallStyle, _cellStyle;

        public CafeEconomy Economy => _eco;

        private bool InTown => _place == Place.Town;
        private CafeMap Interior => _place == Place.Home ? _home : _cafe;
        private TileGrid Grid => InTown ? (TileGrid)_town : Interior;
        /// <summary>식구들이 사는 곳. 카페를 열기 전엔 집입니다.</summary>
        private CafeMap AnimalHome => _eco.CafeOpen ? _cafe : _home;
        private Place AnimalPlace => _eco.CafeOpen ? Place.Cafe : Place.Home;
        private Customer FrontCustomer => _queue.Count > 0 && _queue[0].ReadyToOrder ? _queue[0] : null;

        // ---------- 시작 ----------

        private void Start()
        {
            _home = new CafeMap(CafeMap.HomeLayout) { Origin = HomeOrigin };
            _cafe = new CafeMap();
            _town = new TownMap { Origin = TownOrigin };
            _art = new CafeArt();
            _eco = CafeEconomy.FromJson(PlayerPrefs.GetString(SaveKey, ""));
            _audio = new CafeAudio(gameObject);
            _rng = new System.Random(System.Environment.TickCount);
            _seatTaken = new bool[_cafe.Seats.Count];
            _bowlFood = 4;

            SetupCamera();
            BuildInterior(_home, "Home", out _homeBowlSprite, out _);
            BuildInterior(_cafe, "Cafe", out _bowlSprite, out _towerSprite);
            BuildTown();
            BuildPlayer();
            foreach (Animal a in _eco.Animals) SpawnAnimalView(a, AnimalHome.Floor[_rng.Next(AnimalHome.Floor.Count)]);
            SyncStaff();
            StartDay();
        }

        private void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                _cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            _cam.orthographic = true;
            _cam.orthographicSize = 6.5f;
            _cam.backgroundColor = new Color(0.55f, 0.75f, 0.42f);
            _cam.clearFlags = CameraClearFlags.SolidColor;
        }

        private static SpriteRenderer MakeSprite(Transform parent, string name, Sprite sprite, Vector2 pos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private void BuildInterior(CafeMap map, string name, out SpriteRenderer bowl, out SpriteRenderer tower)
        {
            var root = new GameObject(name).transform;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    Vector2 pos = map.Origin + new Vector2(x, y);
                    MakeSprite(root, $"Tile {x},{y}", _art.CafeTile(map.TileAt(x, y)), pos, -5000);
                }
            }
            tower = null;
            if (map.HasTower)
            {
                Vector2 towerPos = map.Origin + new Vector2(map.Tower.x + 0.5f, map.Tower.y + 0.1f);
                tower = MakeSprite(root, "CatTower", _eco.Has(Upgrade.Tower) ? _art.BigCatTower : _art.CatTower, towerPos,
                    CharacterView.SortOrder(towerPos.y));
            }
            Vector2 bowlPos = map.Origin + new Vector2(map.Bowl.x + 0.5f, map.Bowl.y + 0.3f);
            bowl = MakeSprite(root, "Bowl", _art.BowlFull, bowlPos, CharacterView.SortOrder(bowlPos.y));
        }

        private void BuildTown()
        {
            var root = new GameObject("Town").transform;
            for (int y = 0; y < _town.Height; y++)
            {
                for (int x = 0; x < _town.Width; x++)
                {
                    char t = _town.TileAt(x, y);
                    Vector2 pos = _town.Origin + new Vector2(x, y);
                    char baseTile = t == 'T' || t == 'l' ? 'g' : t;
                    MakeSprite(root, $"Tile {x},{y}", _art.TownTile(baseTile), pos, -5000);
                    if (t == 'T' || t == 'l')
                    {
                        Vector2 foot = pos + new Vector2(0.5f, 0.1f);
                        MakeSprite(root, t == 'T' ? "Tree" : "Lamp", t == 'T' ? _art.Tree : _art.Lamp, foot, CharacterView.SortOrder(foot.y));
                    }
                }
            }
        }

        private void BuildPlayer()
        {
            _player = new GameObject("Owner");
            _playerView = _player.AddComponent<CharacterView>();
            _playerView.Setup(_art.Player);
            // 카페를 운영 중이면 카페에서, 아니면 집에서 하루를 시작한다.
            _place = AnimalPlace;
            _playerPos = Interior.TileToWorld(Interior.Spawn + Vector2Int.up);
            _player.transform.position = _playerPos;
        }

        private AnimalView SpawnAnimalView(Animal data, Vector2Int tile)
        {
            var go = new GameObject($"Animal {data.Name}");
            var view = go.AddComponent<AnimalView>();
            view.Setup(data, _art.Animal(data.Species), AnimalHome, tile, () => _bowlFood > 0, EatFromBowl,
                _art.Zzz, _art.Heart, _seed++);
            _animals.Add(view);
            return view;
        }

        private bool EatFromBowl()
        {
            if (_bowlFood <= 0) return false;
            _bowlFood--;
            return true;
        }

        /// <summary>고용 상태에 맞춰 알바생을 카페에 세우거나 치웁니다.</summary>
        private void SyncStaff()
        {
            bool barista = _eco.CafeOpen && _eco.Has(Staff.Barista);
            bool caretaker = _eco.CafeOpen && _eco.Has(Staff.Caretaker);
            if (barista && _barista == null)
            {
                _barista = new GameObject("Barista").AddComponent<StaffView>();
                _barista.Setup(_art.Barista, _cafe, _cafe.BaristaSpot, false, _seed++);
            }
            else if (!barista && _barista != null)
            {
                Destroy(_barista.gameObject);
                _barista = null;
            }
            if (caretaker && _caretaker == null)
            {
                _caretaker = new GameObject("Caretaker").AddComponent<StaffView>();
                _caretaker.Setup(_art.Caretaker, _cafe, _cafe.Floor[_rng.Next(_cafe.Floor.Count)], true, _seed++);
            }
            else if (!caretaker && _caretaker != null)
            {
                Destroy(_caretaker.gameObject);
                _caretaker = null;
            }
        }

        private void StartDay()
        {
            _state = State.Open;
            _hour = _openHour;
            _customerTimer = 4f;
            _strayTimer = 0f;
            _serveTimer = 0f;
            _panel = Panel.None;
            foreach (StrayView s in _strays) if (s != null) Destroy(s.gameObject);
            _strays.Clear();
            _followers.Clear();
            for (int i = 0; i < 3; i++) SpawnStray();
            switch (_eco.Phase)
            {
                case StoryPhase.Intro:
                case StoryPhase.FirstCat:
                    ShowBanner("조용한 아침. 날씨도 좋은데 산책이나 나가 볼까? (아래 문으로 나가기)");
                    break;
                case StoryPhase.Collecting:
                    ShowBanner($"{_eco.Day}일째. 풀숲·물가에서 친구를 찾아요. 카페 열기까지 {Mathf.Max(0, CafeEconomy.AnimalsToOpenCafe - _eco.Animals.Count)}마리!");
                    break;
                default:
                    ShowBanner(_eco.Has(Staff.Barista)
                        ? $"{_eco.Day}일째 아침. 바리스타가 가게를 보는 동안 새 친구를 찾으러 나가도 좋아요."
                        : $"{_eco.Day}일째 아침. 카운터에서 주문을 받아 커피·간식을 팔아요. 알바를 고용하면 외출 중에도 장사가 됩니다.");
                    break;
            }
        }

        // ---------- 매 프레임 ----------

        private void Update()
        {
            _prompt = null;
            float dt = Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.M)) _audio.ToggleMusic();
            if (_bannerTimer > 0f) _bannerTimer -= dt;
            if (_doorNagTimer > 0f) _doorNagTimer -= dt;
            UpdateTexts(dt);

            if (_state == State.Summary)
            {
                UpdateSummaryInput();
                return;
            }

            UpdatePanels();
            bool frozen = _panel != Panel.None;
            if (!frozen) UpdatePlayer(dt);

            SimulateWorld(dt);
            if (InTown) UpdateTown(dt, frozen);
            else UpdateInterior(dt, frozen);

            if (_state == State.Open)
            {
                _hour += dt * (_closeHour - _openHour) / _dayLengthSeconds;
                if (_hour >= _closeHour)
                {
                    _state = State.Closing;
                    _panel = Panel.None;
                    foreach (Customer c in _customers) c.Leave();
                    _queue.Clear();
                    ShowBanner(_eco.CafeOpen ? "영업 종료! 손님들이 돌아갑니다." : "해가 졌어요. 오늘은 여기까지.");
                }
            }
            else if (_state == State.Closing && _customers.Count == 0)
            {
                EndDay();
            }

            UpdateCamera();
        }

        /// <summary>주인이 어디에 있든 식구들과 카페는 계속 돌아갑니다.</summary>
        private void SimulateWorld(float dt)
        {
            bool cushion = _eco.Has(Upgrade.Cushion);
            foreach (AnimalView a in _animals) a.Tick(dt, cushion);
            _homeBowlSprite.sprite = _bowlSprite.sprite = _bowlFood > 0 ? _art.BowlFull : _art.BowlEmpty;
            if (!_eco.CafeOpen) return;

            if (_state == State.Open)
            {
                _customerTimer -= dt;
                if (_customerTimer <= 0f)
                {
                    _customerTimer = CustomerInterval();
                    SpawnCustomer();
                }
            }
            for (int i = _customers.Count - 1; i >= 0; i--)
            {
                Customer c = _customers[i];
                c.Tick(dt);
                if (c.InQueue && c.Patience <= 0f)
                {
                    _queue.Remove(c);
                    c.LeaveAngry();
                    _eco.LoseCustomer();
                    Float(c.Position + Vector2.up * 1.8f, "너무 오래 기다렸어요… (주문 놓침)", Bad);
                    _audio.Play(_audio.Deny);
                    ArrangeQueue();
                }
                if (!c.Done) continue;
                _customers.RemoveAt(i);
                Destroy(c.gameObject);
            }

            if (_barista != null)
            {
                _barista.Tick(dt);
                _baristaTimer -= dt;
                if (_baristaTimer <= 0f && FrontCustomer != null)
                {
                    _baristaTimer = _baristaSeconds;
                    ServeFront();
                }
            }
            if (_caretaker != null)
            {
                _caretaker.Tick(dt);
                _caretakerTimer -= dt;
                if (_caretakerTimer <= 0f)
                {
                    _caretakerTimer = _caretakerSeconds;
                    CaretakerAct();
                }
            }
        }

        private void CaretakerAct()
        {
            if (_bowlFood <= 2 && _eco.Kibble > 0)
            {
                int added = _eco.FillBowl(_bowlFood);
                _bowlFood += added;
                _caretaker.GoTo(_cafe.BowlSpot);
                Float(_cafe.TileToWorld(_cafe.Bowl) + Vector2.up * 1.2f, $"돌봄 알바가 사료 {added}개를 채웠어요", Good);
                return;
            }
            AnimalView best = null;
            float bestDist = float.MaxValue;
            foreach (AnimalView a in _animals)
            {
                if (a.Asleep) continue;
                float d = Vector2.Distance(a.Position, _caretaker.Position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = a;
                }
            }
            if (best == null) return;
            best.Data.Pet();
            best.ShowHeart();
            _caretaker.GoTo(_cafe.WorldToTile(best.Position));
        }

        private void UpdatePlayer(float dt)
        {
            Vector2 input = Vector2.zero;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) input.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) input.x += 1f;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) input.y -= 1f;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) input.y += 1f;
            _sneaking = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            TileGrid grid = Grid;

            float speed = _sneaking ? _sneakSpeed : _playerSpeed;
            Vector2 delta = input.normalized * speed * dt;
            Vector2 next = _playerPos + new Vector2(delta.x, 0f);
            if (grid.IsWalkable(grid.WorldToTile(next))) _playerPos = next;
            next = _playerPos + new Vector2(0f, delta.y);
            if (grid.IsWalkable(grid.WorldToTile(next))) _playerPos = next;

            _player.transform.position = _playerPos;
            _playerView.Animate(input.sqrMagnitude > 0f, input.x, dt);

            Vector2Int tile = grid.WorldToTile(_playerPos);
            if (!InTown)
            {
                if (Interior.IsDoor(tile) && input.y < 0f) GoToTown(_place == Place.Home ? _town.HomeExitSpot : _town.ExitSpot);
            }
            else if (_town.IsHomeDoor(tile))
            {
                EnterInterior(Place.Home);
            }
            else if (_town.IsCafeDoor(tile))
            {
                if (_eco.CafeOpen) EnterInterior(Place.Cafe);
                else TryCafeDoor();
            }
        }

        private void GoToTown(Vector2Int spot)
        {
            _place = Place.Town;
            _serveTimer = 0f;
            _playerPos = _town.TileToWorld(spot);
            _player.transform.position = _playerPos;
            if (_eco.Phase == StoryPhase.Intro) FirstWalk();
            else if (_eco.Phase != StoryPhase.CafeOpen || _eco.Day == 1)
                Float(_playerPos + Vector2.up * 1.6f, "덤불·나무·물가 근처를 살펴보세요 (Shift = 살금살금)", Ink);
            else if (_eco.CafeOpen && !_eco.Has(Staff.Barista))
                Float(_playerPos + Vector2.up * 1.6f, "가게를 비웠어요. 바리스타가 없으면 손님이 기다리다 갑니다", Bad);
        }

        /// <summary>첫 산책: 길을 가는데 웬 고양이가 졸졸 따라옵니다.</summary>
        private void FirstWalk()
        {
            Species species = SpeciesCatalog.All[16]; // 코리안숏헤어
            Vector2Int spot = _town.HomeExitSpot + Vector2Int.right * 3;
            var go = new GameObject($"Stray {species.Name}");
            var view = go.AddComponent<StrayView>();
            view.Setup(species, new Encounter(species, _rng), _art.Animal(species), _town, spot, _art.Alert, _art.Heart, _art.Bandage);
            view.StartFollowing();
            _strays.Add(view);
            _followers.Add(view);
            _eco.Spot(species);
            _eco.Advance(StoryPhase.FirstCat);
            ShowBanner("어? 웬 고양이가 졸졸 따라오네… 집(왼쪽 위 문)으로 돌아가 볼까요?");
            _audio.PlayVoice(AnimalKind.Cat);
        }

        private void TryCafeDoor()
        {
            if (_eco.CanOpenCafe)
            {
                _prompt = "Space: 빈 가게를 빌려 고양이 카페 열기!";
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    OpenCafe();
                    return;
                }
            }
            else if (_doorNagTimer <= 0f)
            {
                _doorNagTimer = 3f;
                Float(_playerPos + Vector2.up * 1.6f,
                    $"빈 가게예요. 아이들이 {CafeEconomy.AnimalsToOpenCafe}마리 모이면 카페를 열 수 있어요 (지금 {_eco.Animals.Count}마리)", Ink);
            }
            // 문 안으로는 못 들어가므로 한 칸 아래로 밀어낸다.
            _playerPos = _town.TileToWorld(_town.ExitSpot);
            _player.transform.position = _playerPos;
        }

        private void OpenCafe()
        {
            if (!_eco.TryOpenCafe()) return;
            foreach (AnimalView v in _animals) Destroy(v.gameObject);
            _animals.Clear();
            foreach (Animal a in _eco.Animals) SpawnAnimalView(a, _cafe.Floor[_rng.Next(_cafe.Floor.Count)]);
            _audio.Play(_audio.DayEnd);
            EnterInterior(Place.Cafe);
            SyncStaff();
            ShowBanner("고양이 카페 개업! 손님이 카운터에서 커피·간식을 주문해요. 카운터 옆에서 Space로 만들어 주고, 운영 메뉴(O)에서 알바를 고용할 수 있어요.");
            Save();
        }

        private void EnterInterior(Place place)
        {
            _place = place;
            _serveTimer = 0f;
            _playerPos = Interior.TileToWorld(Interior.Spawn);
            _player.transform.position = _playerPos;
            if (place == AnimalPlace) AdoptFollowers();
            else if (_followers.Count > 0)
                Float(_playerPos + Vector2.up * 1.6f, $"따라온 아이는 {(AnimalPlace == Place.Home ? "집" : "카페")}에 데려가야 해요", Ink);
        }

        private void AdoptFollowers()
        {
            bool firstCat = _eco.Phase == StoryPhase.FirstCat;
            foreach (StrayView s in _followers)
            {
                if (s == null) continue;
                Animal animal = Animal.Create(s.Species, _rng, s.Encounter.WasInjured);
                _eco.Adopt(animal);
                SpawnAnimalView(animal, Interior.Spawn + Vector2Int.up);
                Float(Interior.TileToWorld(Interior.Spawn) + Vector2.up * 1.6f, $"{animal.Name}({s.Species.Name})이(가) 식구가 되었어요!", Love);
                _strays.Remove(s);
                Destroy(s.gameObject);
            }
            if (_followers.Count > 0)
            {
                _audio.Play(_audio.Heart);
                if (firstCat)
                {
                    _eco.Advance(StoryPhase.Collecting);
                    ShowBanner($"얼떨결에 {_eco.Animals[0].Name}와(과) 같이 살게 됐어요! 풀숲·물가에서 친구를 더 찾아 {CafeEconomy.AnimalsToOpenCafe}마리가 되면 빈 가게를 빌려 카페를 열 수 있어요.");
                }
                else if (_eco.CanOpenCafe)
                {
                    ShowBanner($"{CafeEconomy.AnimalsToOpenCafe}마리가 모였어요! 동네 한가운데 빈 가게 문 앞에서 카페를 열 수 있어요.");
                }
                Save();
            }
            _followers.Clear();
        }

        // ---------- 동네 ----------

        private void SpawnStray()
        {
            if (_strays.Count >= _maxStrays || _town.HidingSpots.Count == 0) return;
            Vector2Int spot = _town.HidingSpots[_rng.Next(_town.HidingSpots.Count)];
            foreach (StrayView s in _strays)
            {
                if (Vector2.Distance(s.Position, _town.TileToWorld(spot)) < 3f) return;
            }
            float luck = _eco.Day * 0.1f;
            Species species = SpeciesCatalog.Roll(_rng, luck);
            bool injured = _eco.Phase >= StoryPhase.Collecting && _rng.NextDouble() < _injuredChance;
            var go = new GameObject($"Stray {species.Name}");
            var view = go.AddComponent<StrayView>();
            view.Setup(species, new Encounter(species, _rng, injured), _art.Animal(species), _town, spot, _art.Alert, _art.Heart, _art.Bandage);
            _strays.Add(view);
        }

        private void UpdateTown(float dt, bool frozen)
        {
            _strayTimer -= dt;
            if (_strayTimer <= 0f)
            {
                _strayTimer = _strayRespawnSeconds;
                SpawnStray();
            }

            StrayView nearest = null;
            float nearestDist = float.MaxValue;
            for (int i = _strays.Count - 1; i >= 0; i--)
            {
                StrayView s = _strays[i];
                s.Tick(dt, _playerPos, _sneaking);
                if (s.Gone)
                {
                    _strays.RemoveAt(i);
                    Destroy(s.gameObject);
                    continue;
                }
                float d = Vector2.Distance(s.Position, _playerPos);
                if (s.Current == StrayView.State.Hiding && d < 3.5f) _eco.Spot(s.Species);
                if (s.Current == StrayView.State.Hiding && d < nearestDist)
                {
                    nearestDist = d;
                    nearest = s;
                }
            }

            if (frozen) return;
            Vector2Int tile = _town.WorldToTile(_playerPos);
            if (_town.IsNextToShop(tile))
            {
                _prompt = "Space: 간식 가게 열기";
                if (Input.GetKeyDown(KeyCode.Space)) OpenPanel(Panel.Shop);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Q)) _selectedSnack = (_selectedSnack + 1) % SnackCatalog.All.Length;
            for (int i = 0; i < SnackCatalog.All.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) _selectedSnack = i;
            }

            if (nearest != null && nearestDist < InteractRange)
            {
                if (nearest.Injured)
                {
                    _prompt = $"Space: 다친 {nearest.Species.Name} 치료하기 (구급상자 {_eco.Kits}개)";
                    if (Input.GetKeyDown(KeyCode.Space)) Treat(nearest);
                    return;
                }
                Snack snack = SnackCatalog.All[_selectedSnack];
                _prompt = $"Space: {nearest.Species.Name}에게 {snack.Name} 주기 (보유 {_eco.Snacks[_selectedSnack]}개)  Q/1~4: 간식 바꾸기";
                if (Input.GetKeyDown(KeyCode.Space)) Offer(nearest, snack);
            }
            else if (nearest != null && nearestDist < 4f)
            {
                _prompt = nearest.Injured ? "다친 아이예요. 가까이 가서 치료해 주세요" : _sneaking ? "살금살금 다가가세요…" : "Shift 를 누른 채 천천히 다가가세요";
            }
            else if (_eco.Phase == StoryPhase.FirstCat)
            {
                _prompt = "고양이가 따라와요. 왼쪽 위 집 문으로 돌아가 보세요";
            }
        }

        private void Treat(StrayView stray)
        {
            if (!_eco.UseKit())
            {
                Float(_playerPos + Vector2.up * 1.6f, "구급상자가 없어요. 간식 가게에서 사 오세요", Bad);
                _audio.Play(_audio.Deny);
                return;
            }
            stray.Encounter.Treat();
            stray.ShowHeart();
            _eco.Spot(stray.Species);
            Float(stray.Position + Vector2.up * 1.4f, "치료해 줬어요! 고마워해요 ♥ 이제 간식을 주면 따라올 거예요", Love);
            _audio.Play(_audio.Heart);
        }

        private void Offer(StrayView stray, Snack snack)
        {
            if (_eco.Full)
            {
                string why = _eco.CafeOpen
                    ? $"카페가 꽉 찼어요 ({_eco.Capacity}마리). 가게 확장이 필요해요"
                    : "집이 좁아요. 빈 가게에서 카페를 열면 더 데려올 수 있어요";
                Float(_playerPos + Vector2.up * 1.6f, why, Bad);
                _audio.Play(_audio.Deny);
                return;
            }
            if (!_eco.UseSnack(snack.Index))
            {
                Float(_playerPos + Vector2.up * 1.6f, $"{snack.Name}이(가) 없어요. 간식 가게에서 사 오세요", Bad);
                _audio.Play(_audio.Deny);
                return;
            }

            Encounter.Result result = stray.Encounter.Offer(snack);
            bool favorite = snack.Index == stray.Species.FavoriteSnack;
            switch (result)
            {
                case Encounter.Result.Bonded:
                    stray.StartFollowing();
                    _followers.Add(stray);
                    _eco.Spot(stray.Species);
                    Float(stray.Position + Vector2.up * 1.4f, $"{stray.Species.Name}이(가) 따라와요! {(AnimalPlace == Place.Home ? "집" : "카페")}으로 데려가세요", Love);
                    _audio.Play(_audio.Heart);
                    break;
                case Encounter.Result.Fled:
                    stray.Flee(_playerPos);
                    Float(stray.Position + Vector2.up * 1.4f, "앗, 도망갔어요…", Bad);
                    _audio.Play(_audio.Flee);
                    break;
                default:
                    stray.ShowHeart();
                    Float(stray.Position + Vector2.up * 1.4f, favorite ? "좋아하는 간식! ♥♥" : "냠냠 ♥", Love);
                    _audio.PlayVoice(stray.Species.Kind);
                    break;
            }
        }

        // ---------- 집·카페 ----------

        private void UpdateInterior(float dt, bool frozen)
        {
            if (frozen) return;
            CafeMap map = Interior;
            Vector2Int tile = map.WorldToTile(_playerPos);
            bool nextToCounter = _place == Place.Cafe && map.IsNextToCounter(tile);
            if (!nextToCounter) _serveTimer = 0f;

            if (nextToCounter)
            {
                if (Input.GetKeyDown(KeyCode.O))
                {
                    OpenPanel(Panel.Office);
                    return;
                }
                Customer front = FrontCustomer;
                if (_serveTimer > 0f)
                {
                    _serveTimer -= dt;
                    int pct = Mathf.RoundToInt(100f * (1f - _serveTimer / _serveSeconds));
                    _prompt = $"만드는 중… {pct}%";
                    if (_serveTimer <= 0f && front != null) ServeFront();
                    return;
                }
                if (front != null)
                {
                    _prompt = $"Space: {front.Order.Name} 만들어 주기 ({front.Order.Price:N0}원)   O: 운영 메뉴   대기 {_queue.Count}명";
                    if (Input.GetKeyDown(KeyCode.Space)) _serveTimer = _serveSeconds;
                }
                else
                {
                    _prompt = _queue.Count > 0 ? "손님이 줄로 오는 중…   O: 운영 메뉴" : "Space / O: 운영 메뉴 (업그레이드·알바)";
                    if (Input.GetKeyDown(KeyCode.Space)) OpenPanel(Panel.Office);
                }
                return;
            }
            if (map.IsNextToBowl(tile))
            {
                _prompt = $"Space: 밥그릇 채우기 (그릇 {_bowlFood}/{CafeEconomy.BowlCapacity}, 사료 {_eco.Kibble}개)";
                if (Input.GetKeyDown(KeyCode.Space)) FillBowl();
                return;
            }

            AnimalView nearest = null;
            float nearestDist = float.MaxValue;
            if (_place == AnimalPlace)
            {
                foreach (AnimalView a in _animals)
                {
                    float d = Vector2.Distance(a.Position, _playerPos);
                    if (d < nearestDist)
                    {
                        nearestDist = d;
                        nearest = a;
                    }
                }
            }
            if (nearest != null && nearestDist < 1.3f)
            {
                _prompt = $"Space: {nearest.Data.Name} 쓰다듬기 ({nearest.Data.MoodName}, 배고픔 {Mathf.RoundToInt(nearest.Data.Hunger)})";
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    nearest.Data.Pet();
                    nearest.ShowHeart();
                    _audio.PlayVoice(nearest.Data.Species.Kind);
                }
            }
            else if (_eco.Phase == StoryPhase.Intro)
            {
                _prompt = "아래 문으로 나가서 산책해 보세요";
            }
            else if (_place == Place.Cafe && _queue.Count > 0 && _barista == null)
            {
                _prompt = $"손님 {_queue.Count}명이 주문을 기다려요. 카운터 옆으로 가세요";
            }
            else if (_animals.Count == 0)
            {
                _prompt = "아래 문으로 나가서 길에 있는 아이들을 데려오세요";
            }
        }

        private void FillBowl()
        {
            int added = _eco.FillBowl(_bowlFood);
            if (added <= 0)
            {
                Float(_playerPos + Vector2.up * 1.6f, _eco.Kibble <= 0 ? "사료가 없어요. 간식 가게에서 사 오세요" : "그릇이 이미 가득해요", Bad);
                _audio.Play(_audio.Deny);
                return;
            }
            _bowlFood += added;
            Float(_playerPos + Vector2.up * 1.6f, $"사료 {added}개를 채웠어요", Good);
            _audio.Play(_audio.Fill);
        }

        private float CustomerInterval()
        {
            float interval = Mathf.Clamp(16f - _eco.Charm * 0.5f, 3.5f, 16f);
            if (_eco.Has(Upgrade.Sign)) interval *= 0.75f;
            return interval;
        }

        private void SpawnCustomer()
        {
            if (_customers.Count >= _maxCustomers || _queue.Count >= _cafe.QueueSpots.Count) return;
            int roll = _rng.Next(3);
            AnimalKind? prefers = roll == 0 ? AnimalKind.Cat : roll == 1 ? AnimalKind.Dog : (AnimalKind?)null;
            var go = new GameObject("Customer");
            var customer = go.AddComponent<Customer>();
            customer.Setup(_art.RandomCustomer(_seed++), _cafe, MenuCatalog.Roll(_rng), prefers,
                10f + (float)_rng.NextDouble() * 10f, _patienceSeconds, OnCustomerLeave, OnCustomerPet);
            _customers.Add(customer);
            _queue.Add(customer);
            ArrangeQueue();
        }

        private void ArrangeQueue()
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                Vector2Int spot = _cafe.QueueSpots[Mathf.Min(i, _cafe.QueueSpots.Count - 1)];
                _queue[i].SetQueueSpot(spot);
            }
        }

        /// <summary>줄 맨 앞 손님의 주문을 완성해 값을 받고 자리로 보냅니다.</summary>
        private void ServeFront()
        {
            Customer c = FrontCustomer;
            if (c == null) return;
            _queue.RemoveAt(0);
            int seat = -1;
            var free = new List<int>();
            for (int i = 0; i < _seatTaken.Length; i++) if (!_seatTaken[i]) free.Add(i);
            if (free.Count > 0)
            {
                seat = free[_rng.Next(free.Count)];
                _seatTaken[seat] = true;
            }
            int paid = _eco.PayOrder(c.Order);
            c.Serve(seat);
            Float(c.Position + Vector2.up * 1.8f, $"+{paid:N0}원 {c.Order.Name}", Gold);
            _audio.Play(_audio.Coin);
            ArrangeQueue();
        }

        private void OnCustomerLeave(Customer c)
        {
            if (c.SeatIndex >= 0) _seatTaken[c.SeatIndex] = false;
            int tip = 0, counted = 0;
            foreach (Animal a in _eco.Animals)
            {
                if (counted >= 5) break;
                if (c.Prefers.HasValue && a.Species.Kind != c.Prefers.Value) continue;
                if (a.Happiness < 50f) continue;
                tip += CafeEconomy.TipPerAnimal;
                counted++;
            }
            _eco.PayTip(tip);
            if (tip > 0)
            {
                Float(c.Position + Vector2.up * 1.8f, $"팁 +{tip:N0}원", Gold);
                _audio.Play(_audio.Heart);
            }
        }

        private void OnCustomerPet(Customer c)
        {
            AnimalView best = null;
            float bestDist = 2.5f;
            foreach (AnimalView a in _animals)
            {
                if (a.Asleep) continue;
                float d = Vector2.Distance(a.Position, c.Position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = a;
                }
            }
            if (best == null) return;
            best.Data.Pet();
            best.ShowHeart();
        }

        // ---------- 하루 끝 ----------

        private void EndDay()
        {
            _state = State.Summary;
            _panel = Panel.None;
            // 밖에서 하루가 끝나면 따라오던 아이들과 함께 집(또는 카페)으로 돌아온다.
            if (InTown) EnterInterior(AnimalPlace);
            _eco.PayWages();
            _audio.Play(_audio.DayEnd);
            Save();
        }

        private void UpdateSummaryInput()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                _eco.NextDay();
                Save();
                StartDay();
            }
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                PlayerPrefs.DeleteKey(SaveKey);
                PlayerPrefs.Save();
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void Save()
        {
            PlayerPrefs.SetString(SaveKey, _eco.ToJson());
            PlayerPrefs.Save();
        }

        // ---------- 패널 ----------

        private void OpenPanel(Panel panel)
        {
            _panel = panel;
            _cursor = 0;
        }

        private void UpdatePanels()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _panel = _panel == Panel.Collection ? Panel.None : Panel.Collection;
                _cursor = 0;
                return;
            }
            if (_panel == Panel.None) return;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            {
                _panel = Panel.None;
                return;
            }

            if (_panel == Panel.Collection)
            {
                int n = SpeciesCatalog.All.Length;
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) _collectionCursor = (_collectionCursor + 1) % n;
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) _collectionCursor = (_collectionCursor + n - 1) % n;
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) _collectionCursor = Mathf.Min(n - 1, _collectionCursor + CollectionCols);
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) _collectionCursor = Mathf.Max(0, _collectionCursor - CollectionCols);
                return;
            }

            int count = _panel switch
            {
                Panel.Shop => SnackCatalog.All.Length + 2,
                Panel.Office => CafeEconomy.Upgrades.Length + CafeEconomy.Staffs.Length,
                _ => 0,
            };
            if (count == 0) return;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) _cursor = (_cursor + count - 1) % count;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) _cursor = (_cursor + 1) % count;
            if (!Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.Return)) return;

            bool ok;
            string what;
            if (_panel == Panel.Shop)
            {
                if (_cursor < SnackCatalog.All.Length)
                {
                    ok = _eco.TryBuySnack(_cursor);
                    what = SnackCatalog.All[_cursor].Name;
                }
                else if (_cursor == SnackCatalog.All.Length)
                {
                    ok = _eco.TryBuyKibble();
                    what = $"사료 {CafeEconomy.KibbleBundle}개";
                }
                else
                {
                    ok = _eco.TryBuyKit();
                    what = "구급상자";
                }
            }
            else if (_cursor < CafeEconomy.Upgrades.Length)
            {
                UpgradeInfo info = CafeEconomy.Upgrades[_cursor];
                ok = _eco.TryBuyUpgrade(info.Key);
                what = info.Name;
                if (ok && info.Key == Upgrade.Tower && _towerSprite != null) _towerSprite.sprite = _art.BigCatTower;
            }
            else
            {
                StaffInfo info = CafeEconomy.Staffs[_cursor - CafeEconomy.Upgrades.Length];
                _eco.ToggleHire(info.Key);
                SyncStaff();
                bool hired = _eco.Has(info.Key);
                Float(_playerPos + Vector2.up * 1.6f, hired ? $"{info.Name} 고용! 일당 {info.Wage:N0}원은 하루 끝에 나가요" : $"{info.Name}을(를) 내보냈어요", hired ? Good : Ink);
                _audio.Play(hired ? _audio.Buy : _audio.Deny);
                Save();
                return;
            }

            if (ok)
            {
                Float(_playerPos + Vector2.up * 1.6f, $"{what} 구입!", Good);
                _audio.Play(_audio.Buy);
                Save();
            }
            else
            {
                Float(_playerPos + Vector2.up * 1.6f, "돈이 모자라거나 이미 가지고 있어요", Bad);
                _audio.Play(_audio.Deny);
            }
        }

        // ---------- 카메라·텍스트 ----------

        private void UpdateCamera()
        {
            TileGrid grid = Grid;
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;
            Vector2 min = grid.Origin, max = grid.Origin + new Vector2(grid.Width, grid.Height);
            float x = max.x - min.x <= halfW * 2f ? (min.x + max.x) / 2f : Mathf.Clamp(_playerPos.x, min.x + halfW, max.x - halfW);
            float y = max.y - min.y <= halfH * 2f ? (min.y + max.y) / 2f : Mathf.Clamp(_playerPos.y, min.y + halfH, max.y - halfH);
            _cam.transform.position = new Vector3(x, y, -10f);
        }

        private void Float(Vector2 pos, string text, Color color) =>
            _texts.Add(new FloatingText { Pos = pos, Text = text, Color = color });

        private void ShowBanner(string text)
        {
            _banner = text;
            _bannerTimer = 7f;
        }

        private void UpdateTexts(float dt)
        {
            for (int i = _texts.Count - 1; i >= 0; i--)
            {
                FloatingText t = _texts[i];
                t.Age += dt;
                t.Pos += Vector2.up * dt * 0.6f;
                if (t.Age >= TextSeconds * 2f) _texts.RemoveAt(i);
                else _texts[i] = t;
            }
        }

        // ---------- HUD ----------

        private void EnsureStyles()
        {
            if (_panelStyle != null) return;
            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(1f, 0.97f, 0.92f, 0.94f));
            bg.Apply();
            var cell = new Texture2D(1, 1);
            cell.SetPixel(0, 0, new Color(0.93f, 0.85f, 0.72f, 1f));
            cell.Apply();
            _panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = bg }, padding = new RectOffset(14, 14, 10, 10) };
            _cellStyle = new GUIStyle(GUI.skin.box) { normal = { background = cell } };
            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Ink }, richText = true, wordWrap = true };
            _smallStyle = new GUIStyle(_labelStyle) { fontSize = 13 };
            _titleStyle = new GUIStyle(_labelStyle) { fontSize = 22, fontStyle = FontStyle.Bold };
            _floatStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerCenter, wordWrap = false };
        }

        /// <summary>월드 좌표 위에 놓는 GUI 사각형. 화면 밖으로 나가지 않게 가장자리에서 밀어 넣습니다.</summary>
        private Rect WorldRect(Vector2 world, float w, float h)
        {
            Vector3 sp = _cam.WorldToScreenPoint(world);
            float x = Mathf.Clamp(sp.x - w / 2f, 4f, Mathf.Max(4f, Screen.width - w - 4f));
            float y = Mathf.Clamp(Screen.height - sp.y - h, 4f, Mathf.Max(4f, Screen.height - h - 4f));
            return new Rect(x, y, w, h);
        }

        private void OnGUI()
        {
            if (_cam == null || _eco == null) return;
            EnsureStyles();

            DrawHud();
            DrawWorldOverlays();

            if (_panel == Panel.Shop) DrawShop();
            else if (_panel == Panel.Office) DrawOffice();
            else if (_panel == Panel.Collection) DrawCollection();
            if (_state == State.Summary) DrawSummary();

            if (_bannerTimer > 0f && _state != State.Summary)
            {
                string text = $"<b>{_banner}</b>";
                float width = Mathf.Min(680f, Screen.width - 40f);
                float height = _labelStyle.CalcHeight(new GUIContent(text), width - 20f) + 14f;
                var r = new Rect(Screen.width / 2f - width / 2f, 70f, width, height);
                GUI.Box(r, "", _panelStyle);
                GUI.Label(new Rect(r.x + 10f, r.y + 6f, r.width - 20f, height - 10f), text, _labelStyle);
            }

            if (!string.IsNullOrEmpty(_prompt) && _panel == Panel.None && _state != State.Summary)
            {
                var r = new Rect(Screen.width / 2f - 320f, Screen.height - 54f, 640f, 40f);
                GUI.Box(r, "", _panelStyle);
                GUI.Label(new Rect(r.x + 10f, r.y + 8f, r.width - 20f, 30f), _prompt, _labelStyle);
            }
        }

        private void DrawHud()
        {
            int h = Mathf.FloorToInt(Mathf.Min(_hour, _closeHour));
            int m = Mathf.FloorToInt((Mathf.Min(_hour, _closeHour) - h) * 60f);
            string snacks = "";
            for (int i = 0; i < SnackCatalog.All.Length; i++)
            {
                string name = SnackCatalog.All[i].Name;
                snacks += i == _selectedSnack && InTown ? $"<b>[{name} {_eco.Snacks[i]}]</b> " : $"{name} {_eco.Snacks[i]}  ";
            }
            string where = _place switch { Place.Home => "집", Place.Cafe => "카페", _ => "동네" };
            string line1 = $"<b>{_eco.Day}일째</b>  {h:00}:{m:00}  ·  {where}  ·  <b>{_eco.Money:N0}원</b>";
            if (_eco.CafeOpen) line1 += $"   <size=13>오늘 매출 {_eco.Report.Income:N0}원 · 대기 손님 {_queue.Count}명</size>";
            string progress = _eco.CafeOpen
                ? $"매력 {_eco.Charm:0.0}"
                : $"카페 열기까지 {Mathf.Min(_eco.Animals.Count, CafeEconomy.AnimalsToOpenCafe)}/{CafeEconomy.AnimalsToOpenCafe}마리";
            string line2 = $"식구 {_eco.Animals.Count}/{_eco.Capacity}  (고양이 {_eco.Count(AnimalKind.Cat)} · 강아지 {_eco.Count(AnimalKind.Dog)})  ·  {progress}  ·  그릇 {_bowlFood}/{CafeEconomy.BowlCapacity}  사료 {_eco.Kibble}  구급상자 {_eco.Kits}";
            string line3 = $"간식: {snacks}";
            if (_eco.CafeOpen)
            {
                var staff = new List<string>();
                for (int i = 0; i < CafeEconomy.Staffs.Length; i++) if (_eco.Hired[i]) staff.Add(CafeEconomy.Staffs[i].Name);
                line3 += staff.Count > 0 ? $"   알바: {string.Join(", ", staff)}" : "   알바 없음";
            }
            if (_followers.Count > 0) line3 += $"   ♥ 따라오는 아이 {_followers.Count}";

            var r = new Rect(10f, 10f, 660f, 86f);
            GUI.Box(r, "", _panelStyle);
            GUI.Label(new Rect(r.x + 12f, r.y + 6f, r.width - 24f, 24f), line1, _labelStyle);
            GUI.Label(new Rect(r.x + 12f, r.y + 30f, r.width - 24f, 24f), line2, _smallStyle);
            GUI.Label(new Rect(r.x + 12f, r.y + 54f, r.width - 24f, 24f), line3, _smallStyle);

            string hint = $"Tab 수집북 · M 음악 {(_audio.MusicOn ? "켬" : "끔")} · 이동 WASD/방향키 · Shift 살금살금";
            if (Screen.width >= 1040) GUI.Label(new Rect(Screen.width - 330f, 10f, 320f, 24f), hint, _smallStyle);
            else GUI.Label(new Rect(r.x + 12f, r.y + r.height + 2f, r.width - 24f, 24f), hint, _smallStyle);
        }

        private void DrawWorldOverlays()
        {
            foreach (FloatingText t in _texts)
            {
                float alpha = t.Age < TextSeconds ? 1f : 1f - (t.Age - TextSeconds) / TextSeconds;
                _floatStyle.normal.textColor = new Color(t.Color.r, t.Color.g, t.Color.b, alpha);
                GUI.Label(WorldRect(t.Pos, 520f, 26f), t.Text, _floatStyle);
            }

            if (_place == Place.Cafe)
            {
                foreach (Customer c in _queue)
                {
                    if (!c.ReadyToOrder) continue;
                    Rect r = WorldRect(c.Position + Vector2.up * 1.7f, 120f, 36f);
                    GUI.Box(r, "", _panelStyle);
                    GUI.Label(new Rect(r.x + 6f, r.y + 1f, r.width - 12f, 20f), $"<size=12>{c.Order.Name}</size>", _smallStyle);
                    DrawBar(new Rect(r.x + 8f, r.y + 24f, r.width - 16f, 6f), c.Patience / c.PatienceMax, c.Patience < 8f ? Bad : Good);
                }
                return;
            }

            if (!InTown) return;
            foreach (StrayView s in _strays)
            {
                if (s.Current != StrayView.State.Hiding) continue;
                float dist = Vector2.Distance(s.Position, _playerPos);
                if (dist > 4.5f) continue;
                Encounter e = s.Encounter;
                Rect r = WorldRect(s.Position + Vector2.up * 1.6f, 150f, 44f);
                GUI.Box(r, "", _panelStyle);
                bool known = _eco.Seen[s.Species.Index];
                string name = known ? $"<b>{s.Species.Name}</b> {s.Species.PersonalityName}" : "<b>???</b>";
                if (s.Injured) name = $"<color=#c03040><b>다침!</b></color> {(known ? s.Species.Name : "???")}";
                GUI.Label(new Rect(r.x + 8f, r.y + 2f, r.width - 16f, 20f), name, _smallStyle);
                DrawBar(new Rect(r.x + 8f, r.y + 24f, 62f, 8f), e.Bond / Encounter.BondGoal, Love);
                DrawBar(new Rect(r.x + 78f, r.y + 24f, 62f, 8f), e.Wariness, Bad);
                GUI.Label(new Rect(r.x + 8f, r.y + 30f, 70f, 14f), "<size=10>친밀</size>", _smallStyle);
                GUI.Label(new Rect(r.x + 78f, r.y + 30f, 70f, 14f), "<size=10>경계</size>", _smallStyle);
            }
        }

        private static void DrawBar(Rect r, float value, Color color)
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.25f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(value), r.height), Texture2D.whiteTexture);
            GUI.color = old;
        }

        private Rect PanelRect(float w, float h) => new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f, w, h);

        private void DrawShop()
        {
            Rect r = PanelRect(540f, 360f);
            GUI.Box(r, "", _panelStyle);
            GUI.Label(new Rect(r.x + 16f, r.y + 10f, r.width, 30f), "간식 가게", _titleStyle);
            GUI.Label(new Rect(r.x + 16f, r.y + 40f, r.width - 32f, 24f), $"보유 {_eco.Money:N0}원   ↑↓ 고르기 · Space 구입 · Esc 닫기", _smallStyle);
            float y = r.y + 76f;
            void Row(int index, string text)
            {
                GUI.Label(new Rect(r.x + 16f, y, r.width - 32f, 26f), index == _cursor ? $"<b>▶ {text}</b>" : $"   {text}", _labelStyle);
                y += 30f;
            }
            for (int i = 0; i < SnackCatalog.All.Length; i++)
            {
                Snack s = SnackCatalog.All[i];
                Row(i, $"{s.Name}  {s.Price:N0}원   보유 {_eco.Snacks[i]}   고양이 +{s.CatBond:0} / 강아지 +{s.DogBond:0}");
            }
            y += 6f;
            Row(SnackCatalog.All.Length, $"사료 {CafeEconomy.KibbleBundle}개  {CafeEconomy.KibblePrice * CafeEconomy.KibbleBundle:N0}원   보유 {_eco.Kibble}   밥그릇에 채웁니다");
            Row(SnackCatalog.All.Length + 1, $"구급상자  {CafeEconomy.KitPrice:N0}원   보유 {_eco.Kits}   다친 아이를 치료합니다");
            GUI.Label(new Rect(r.x + 16f, r.y + r.height - 44f, r.width - 32f, 40f),
                "좋아하는 간식은 친밀도 1.8배. 다친 아이는 치료해 줘야 간식을 받고, 고마워서 금방 따라옵니다.", _smallStyle);
        }

        private void DrawOffice()
        {
            Rect r = PanelRect(600f, 380f);
            GUI.Box(r, "", _panelStyle);
            GUI.Label(new Rect(r.x + 16f, r.y + 10f, r.width, 30f), "운영 메뉴", _titleStyle);
            GUI.Label(new Rect(r.x + 16f, r.y + 40f, r.width - 32f, 24f), $"보유 {_eco.Money:N0}원   ↑↓ 고르기 · Space 구입/고용 · Esc 닫기", _smallStyle);
            float y = r.y + 72f;
            void Row(int index, string text)
            {
                GUI.Label(new Rect(r.x + 16f, y, r.width - 32f, 26f), index == _cursor ? $"<b>▶ {text}</b>" : $"   {text}", _labelStyle);
                y += 28f;
            }
            for (int i = 0; i < CafeEconomy.Upgrades.Length; i++)
            {
                UpgradeInfo u = CafeEconomy.Upgrades[i];
                Row(i, $"{u.Name}  ·  {(_eco.Has(u.Key) ? "보유 중" : $"{u.Price:N0}원")}  ·  {u.Effect}");
            }
            y += 8f;
            GUI.Label(new Rect(r.x + 16f, y, r.width - 32f, 22f), "<b>알바생</b> (일당은 하루 끝에 지급)", _smallStyle);
            y += 24f;
            for (int i = 0; i < CafeEconomy.Staffs.Length; i++)
            {
                StaffInfo s = CafeEconomy.Staffs[i];
                Row(CafeEconomy.Upgrades.Length + i, $"{s.Name}  ·  {(_eco.Has(s.Key) ? "근무 중 (Space: 내보내기)" : $"일당 {s.Wage:N0}원 (Space: 고용)")}  ·  {s.Effect}");
            }
            GUI.Label(new Rect(r.x + 16f, r.y + r.height - 46f, r.width - 32f, 40f),
                $"커피·간식 주문으로 돈을 법니다. 좋아하는 종류의 아이가 기분 좋게 있으면 손님이 한 마리마다 팁 {CafeEconomy.TipPerAnimal:N0}원을 둡니다.", _smallStyle);
        }

        /// <summary>스타듀밸리 수집 탭처럼 그림 격자와 상세 칸으로 보여 주는 도감.</summary>
        private void DrawCollection()
        {
            int n = SpeciesCatalog.All.Length;
            int rows = (n + CollectionCols - 1) / CollectionCols;
            // 작은 창에서는 칸을 줄여 격자가 다 보이게 한다.
            float cell = Mathf.Clamp((Screen.height - 130f) / rows, 40f, 64f);
            float gridW = CollectionCols * cell;
            float panelW = Mathf.Min(Screen.width - 20f, gridW + 360f);
            float panelH = Mathf.Min(Screen.height - 20f, 110f + rows * cell);
            Rect r = PanelRect(panelW, panelH);
            GUI.Box(r, "", _panelStyle);

            int seen = 0, ownedKinds = 0, rescuedKinds = 0;
            for (int i = 0; i < n; i++)
            {
                if (_eco.Seen[i]) seen++;
                if (_eco.OwnedCount(i) > 0) ownedKinds++;
                if (_eco.Rescued[i]) rescuedKinds++;
            }
            GUI.Label(new Rect(r.x + 16f, r.y + 8f, r.width, 30f), $"수집북   <size=14>본 아이 {seen}/{n} · 데려온 종류 {ownedKinds}/{n} · 치료한 종류 {rescuedKinds}</size>", _titleStyle);
            GUI.Label(new Rect(r.x + 16f, r.y + 38f, r.width - 32f, 22f), "방향키로 고르기 · Tab/Esc 닫기. 가까이서 본 아이는 그림이, 데려온 아이는 좋아하는 간식이 기록됩니다.", _smallStyle);

            float gx = r.x + 16f, gy = r.y + 66f;
            _collectionCursor = Mathf.Clamp(_collectionCursor, 0, n - 1);
            for (int i = 0; i < n; i++)
            {
                Species s = SpeciesCatalog.All[i];
                int col = i % CollectionCols, row = i / CollectionCols;
                var box = new Rect(gx + col * cell, gy + row * cell, cell - 4f, cell - 4f);
                bool selected = i == _collectionCursor;
                Color old = GUI.color;
                GUI.color = selected ? new Color(1f, 0.85f, 0.6f) : Color.white;
                GUI.Box(box, "", _cellStyle);
                bool known = _eco.Seen[i];
                Texture tex = _art.Animal(s).Idle.texture;
                GUI.color = known ? Color.white : new Color(0.32f, 0.26f, 0.24f, 1f);
                GUI.DrawTexture(new Rect(box.x + 6f, box.y + 6f, box.width - 12f, box.height - 18f), tex, ScaleMode.ScaleToFit);
                GUI.color = old;
                int owned = _eco.OwnedCount(i);
                if (owned > 0) GUI.Label(new Rect(box.x + 4f, box.y + box.height - 18f, box.width - 8f, 16f), $"<size=11><b>×{owned}</b></size>", _smallStyle);
                if (_eco.Rescued[i]) GUI.DrawTexture(new Rect(box.x + box.width - 16f, box.y + 3f, 12f, 12f), _art.Bandage.texture, ScaleMode.ScaleToFit);
                if (!known) GUI.Label(new Rect(box.x + 4f, box.y + box.height - 18f, box.width - 8f, 16f), "<size=11>?</size>", _smallStyle);
            }

            // 상세 칸
            Species sel = SpeciesCatalog.All[_collectionCursor];
            float dx = gx + gridW + 12f, dw = r.x + r.width - dx - 16f;
            if (dw < 120f) return;
            var detail = new Rect(dx, gy, dw, r.y + r.height - gy - 16f);
            GUI.Box(detail, "", _cellStyle);
            bool selKnown = _eco.Seen[_collectionCursor];
            Color oc = GUI.color;
            GUI.color = selKnown ? Color.white : new Color(0.32f, 0.26f, 0.24f, 1f);
            GUI.DrawTexture(new Rect(detail.x + (dw - 96f) / 2f, detail.y + 10f, 96f, 84f), _art.Animal(sel).Idle.texture, ScaleMode.ScaleToFit);
            GUI.color = oc;
            float ty = detail.y + 100f;
            void Line(string text, float height = 22f)
            {
                GUI.Label(new Rect(detail.x + 10f, ty, dw - 20f, height), text, _smallStyle);
                ty += height;
            }
            if (!selKnown)
            {
                Line("<b><size=18>???</size></b>", 28f);
                Line($"{sel.KindName} · {sel.RarityName}");
                Line("아직 만나지 못했어요. 동네를 더 돌아보세요.", 44f);
                return;
            }
            int ownedSel = _eco.OwnedCount(_collectionCursor);
            Line($"<b><size=18>{sel.Name}</size></b>", 28f);
            Line($"{sel.KindName} · {sel.RarityName} · {sel.PersonalityName}");
            Line($"매력 {sel.Charm}{(sel.LongHair ? " · 장모종" : "")}");
            Line(ownedSel > 0 ? $"좋아하는 간식: <b>{SnackCatalog.All[sel.FavoriteSnack].Name}</b>" : "좋아하는 간식: ??? (데려오면 기록)");
            Line($"데려온 아이 {ownedSel}마리{(_eco.Rescued[_collectionCursor] ? " · 치료해서 구한 적 있음" : "")}");
            if (ownedSel > 0)
            {
                var names = new List<string>();
                foreach (Animal a in _eco.Animals) if (a.SpeciesIndex == sel.Index) names.Add(a.Rescued ? $"{a.Name}(치료)" : a.Name);
                Line($"이름: {string.Join(", ", names)}", 40f);
            }
            Line(sel.Note, 60f);
        }

        private void DrawSummary()
        {
            DayReport rep = _eco.Report;
            Rect r = PanelRect(540f, 360f);
            GUI.Box(r, "", _panelStyle);
            GUI.Label(new Rect(r.x + 16f, r.y + 10f, r.width, 30f), _eco.CafeOpen ? $"{rep.Day}일째 영업 끝" : $"{rep.Day}일째 하루 끝", _titleStyle);
            float y = r.y + 50f;
            void Line(string text)
            {
                GUI.Label(new Rect(r.x + 16f, y, r.width - 32f, 26f), text, _labelStyle);
                y += 28f;
            }
            if (_eco.CafeOpen)
            {
                Line($"주문 {rep.Orders}건 · 자리 손님 {rep.Visitors}명 · 놓친 손님 {rep.Lost}명");
                Line($"매출 {rep.Income:N0}원 (팁 {rep.Tips:N0}원) · 알바 일당 -{rep.Wages:N0}원");
            }
            else Line($"카페 열기까지 {Mathf.Max(0, CafeEconomy.AnimalsToOpenCafe - _eco.Animals.Count)}마리 더 필요해요");
            Line($"현재 자금 {_eco.Money:N0}원  ·  식구 {_eco.Animals.Count}/{_eco.Capacity}마리");
            Line(rep.NewAnimals.Count > 0 ? $"새 식구: {string.Join(", ", rep.NewAnimals)}" : "새 식구: 없음");
            float avg = 0f;
            foreach (Animal a in _eco.Animals) avg += a.Happiness;
            if (_eco.Animals.Count > 0) Line($"아이들 평균 기분 {avg / _eco.Animals.Count:0}  ·  배고픈 아이는 밥그릇을 채워 주세요");
            y += 10f;
            Line("<b>Space</b> 다음 날  ·  <b>Backspace</b> 새 게임");
        }
    }
}
