using System.Collections.Generic;
using UnityEngine;

namespace StrayCatCafe
{
    /// <summary>
    /// 길고양이 카페 진행 관리. 동네에서 길고양이·강아지와 친해져 데려오고, 카페에서 손님을 받아 돈을 벌고,
    /// 간식·사료·업그레이드를 사며 가게를 키웁니다. 카페 안과 동네는 같은 씬의 떨어진 자리에 있고
    /// 주인이 문을 드나들 때 카메라가 옮겨 갑니다. 씬에는 이 컴포넌트와 메인 카메라만 있으면 됩니다.
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
        [Header("Town")]
        [SerializeField] private int _maxStrays = 4;
        [SerializeField] private float _strayRespawnSeconds = 25f;
        [Header("Cafe")]
        [SerializeField] private int _maxCustomers = 8;

        private static readonly Vector2 TownOrigin = new Vector2(100f, 0f);

        private enum State { Open, Closing, Summary }
        private enum Panel { None, Shop, Office, Collection }

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
        private static readonly Color Ink = new Color(0.3f, 0.19f, 0.15f);
        private static readonly Color Good = new Color(0.2f, 0.55f, 0.3f);
        private static readonly Color Bad = new Color(0.82f, 0.25f, 0.28f);
        private static readonly Color Gold = new Color(0.85f, 0.55f, 0.1f);
        private static readonly Color Love = new Color(0.9f, 0.35f, 0.45f);

        private CafeMap _cafe;
        private TownMap _town;
        private CafeArt _art;
        private CafeEconomy _eco;
        private CafeAudio _audio;
        private Camera _cam;
        private System.Random _rng;

        private GameObject _player;
        private CharacterView _playerView;
        private Vector2 _playerPos;
        private bool _inTown, _sneaking;

        private readonly List<AnimalView> _animals = new List<AnimalView>();
        private readonly List<Customer> _customers = new List<Customer>();
        private readonly List<StrayView> _strays = new List<StrayView>();
        private readonly List<StrayView> _followers = new List<StrayView>();
        private readonly List<FloatingText> _texts = new List<FloatingText>();
        private bool[] _seatTaken;
        private int _bowlFood;
        private SpriteRenderer _bowlSprite, _towerSprite;

        private State _state;
        private float _hour, _customerTimer, _strayTimer;
        private int _seed;
        private Panel _panel;
        private int _cursor, _selectedSnack = 1, _collectionPage;
        private const int CollectionPerPage = 16;
        private string _prompt, _banner;
        private float _bannerTimer;
        private GUIStyle _panelStyle, _labelStyle, _titleStyle, _floatStyle, _smallStyle;

        public CafeEconomy Economy => _eco;

        // ---------- 시작 ----------

        private void Start()
        {
            _cafe = new CafeMap();
            _town = new TownMap { Origin = TownOrigin };
            _art = new CafeArt();
            _eco = CafeEconomy.FromJson(PlayerPrefs.GetString(SaveKey, ""));
            _audio = new CafeAudio(gameObject);
            _rng = new System.Random(System.Environment.TickCount);
            _seatTaken = new bool[_cafe.Seats.Count];
            _bowlFood = 4;

            SetupCamera();
            BuildCafe();
            BuildTown();
            BuildPlayer();
            foreach (Animal a in _eco.Animals) SpawnAnimalView(a, _cafe.Floor[_rng.Next(_cafe.Floor.Count)]);
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

        private void BuildCafe()
        {
            var root = new GameObject("Cafe").transform;
            for (int y = 0; y < _cafe.Height; y++)
            {
                for (int x = 0; x < _cafe.Width; x++)
                {
                    char t = _cafe.TileAt(x, y);
                    Vector2 pos = _cafe.Origin + new Vector2(x, y);
                    MakeSprite(root, $"Tile {x},{y}", _art.CafeTile(t), pos, -5000);
                }
            }
            Vector2 towerPos = _cafe.Origin + new Vector2(_cafe.Tower.x + 0.5f, _cafe.Tower.y + 0.1f);
            _towerSprite = MakeSprite(root, "CatTower", _eco.Has(Upgrade.Tower) ? _art.BigCatTower : _art.CatTower, towerPos,
                CharacterView.SortOrder(towerPos.y));
            Vector2 bowlPos = _cafe.Origin + new Vector2(_cafe.Bowl.x + 0.5f, _cafe.Bowl.y + 0.3f);
            _bowlSprite = MakeSprite(root, "Bowl", _art.BowlFull, bowlPos, CharacterView.SortOrder(bowlPos.y));
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
            _playerPos = _cafe.TileToWorld(_cafe.Spawn + Vector2Int.up);
            _player.transform.position = _playerPos;
            _inTown = false;
        }

        private AnimalView SpawnAnimalView(Animal data, Vector2Int tile)
        {
            var go = new GameObject($"Animal {data.Name}");
            var view = go.AddComponent<AnimalView>();
            view.Setup(data, _art.Animal(data.Species), _cafe, tile, () => _bowlFood > 0, EatFromBowl,
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

        private void StartDay()
        {
            _state = State.Open;
            _hour = _openHour;
            _customerTimer = 4f;
            _strayTimer = 0f;
            _panel = Panel.None;
            foreach (StrayView s in _strays) if (s != null) Destroy(s.gameObject);
            _strays.Clear();
            _followers.Clear();
            for (int i = 0; i < 3; i++) SpawnStray();
            ShowBanner($"{_eco.Day}일째 아침. 길에서 아이들을 찾아 카페로 데려오세요!");
        }

        // ---------- 매 프레임 ----------

        private void Update()
        {
            _prompt = null;
            float dt = Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.M)) _audio.ToggleMusic();
            if (_bannerTimer > 0f) _bannerTimer -= dt;
            UpdateTexts(dt);

            if (_state == State.Summary)
            {
                UpdateSummaryInput();
                return;
            }

            UpdatePanels();
            bool frozen = _panel != Panel.None;
            if (!frozen) UpdatePlayer(dt);

            if (_inTown) UpdateTown(dt, frozen);
            else UpdateCafe(dt, frozen);

            if (_state == State.Open)
            {
                _hour += dt * (_closeHour - _openHour) / _dayLengthSeconds;
                if (_hour >= _closeHour)
                {
                    _state = State.Closing;
                    _panel = Panel.None;
                    foreach (Customer c in _customers) c.Leave();
                    ShowBanner("영업 종료! 손님들이 돌아갑니다.");
                }
            }
            else if (_state == State.Closing && _customers.Count == 0)
            {
                EndDay();
            }

            UpdateCamera();
        }

        private void UpdatePlayer(float dt)
        {
            Vector2 input = Vector2.zero;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) input.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) input.x += 1f;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) input.y -= 1f;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) input.y += 1f;
            _sneaking = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            TileGrid grid = _inTown ? (TileGrid)_town : _cafe;

            float speed = _sneaking ? _sneakSpeed : _playerSpeed;
            Vector2 delta = input.normalized * speed * dt;
            Vector2 next = _playerPos + new Vector2(delta.x, 0f);
            if (grid.IsWalkable(grid.WorldToTile(next))) _playerPos = next;
            next = _playerPos + new Vector2(0f, delta.y);
            if (grid.IsWalkable(grid.WorldToTile(next))) _playerPos = next;

            _player.transform.position = _playerPos;
            _playerView.Animate(input.sqrMagnitude > 0f, input.x, dt);

            Vector2Int tile = grid.WorldToTile(_playerPos);
            if (!_inTown && _cafe.IsDoor(tile) && input.y < 0f) GoToTown();
            else if (_inTown && _town.IsCafeDoor(tile)) GoToCafe();
        }

        private void GoToTown()
        {
            _inTown = true;
            _playerPos = _town.TileToWorld(_town.ExitSpot);
            _player.transform.position = _playerPos;
            Float(_playerPos + Vector2.up * 1.6f, "덤불·나무 근처를 살펴보세요 (Shift = 살금살금)", Ink);
        }

        private void GoToCafe()
        {
            _inTown = false;
            _playerPos = _cafe.TileToWorld(_cafe.Spawn);
            _player.transform.position = _playerPos;
            AdoptFollowers();
        }

        private void AdoptFollowers()
        {
            foreach (StrayView s in _followers)
            {
                if (s == null) continue;
                Animal animal = Animal.Create(s.Species, _rng);
                _eco.Adopt(animal);
                SpawnAnimalView(animal, _cafe.Spawn + Vector2Int.up);
                Float(_cafe.TileToWorld(_cafe.Spawn) + Vector2.up * 1.6f, $"{animal.Name}({s.Species.Name})이(가) 식구가 되었어요!", Love);
                _strays.Remove(s);
                Destroy(s.gameObject);
            }
            if (_followers.Count > 0)
            {
                _audio.Play(_audio.Heart);
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
            var go = new GameObject($"Stray {species.Name}");
            var view = go.AddComponent<StrayView>();
            view.Setup(species, new Encounter(species, _rng), _art.Animal(species), _town, spot, _art.Alert, _art.Heart);
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
                Snack snack = SnackCatalog.All[_selectedSnack];
                _prompt = $"Space: {nearest.Species.Name}에게 {snack.Name} 주기 (보유 {_eco.Snacks[_selectedSnack]}개)  Q/1~4: 간식 바꾸기";
                if (Input.GetKeyDown(KeyCode.Space)) Offer(nearest, snack);
            }
            else if (nearest != null && nearestDist < 4f)
            {
                _prompt = _sneaking ? "살금살금 다가가세요…" : "Shift 를 누른 채 천천히 다가가세요";
            }
        }

        private void Offer(StrayView stray, Snack snack)
        {
            if (_eco.Full)
            {
                Float(_playerPos + Vector2.up * 1.6f, $"카페가 꽉 찼어요 ({_eco.Capacity}마리). 가게 확장이 필요해요", Bad);
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
                    Float(stray.Position + Vector2.up * 1.4f, $"{stray.Species.Name}이(가) 따라와요! 카페로 데려가세요", Love);
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

        // ---------- 카페 ----------

        private void UpdateCafe(float dt, bool frozen)
        {
            bool cushion = _eco.Has(Upgrade.Cushion);
            foreach (AnimalView a in _animals) a.Tick(dt, cushion);
            _bowlSprite.sprite = _bowlFood > 0 ? _art.BowlFull : _art.BowlEmpty;

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
                if (!c.Done) continue;
                _customers.RemoveAt(i);
                Destroy(c.gameObject);
            }

            if (frozen) return;
            Vector2Int tile = _cafe.WorldToTile(_playerPos);
            if (_cafe.IsNextToCounter(tile))
            {
                _prompt = "Space: 운영 메뉴 (업그레이드)";
                if (Input.GetKeyDown(KeyCode.Space)) OpenPanel(Panel.Office);
                return;
            }
            if (_cafe.IsNextToBowl(tile))
            {
                _prompt = $"Space: 밥그릇 채우기 (그릇 {_bowlFood}/{CafeEconomy.BowlCapacity}, 사료 {_eco.Kibble}개)";
                if (Input.GetKeyDown(KeyCode.Space)) FillBowl();
                return;
            }

            AnimalView nearest = null;
            float nearestDist = float.MaxValue;
            foreach (AnimalView a in _animals)
            {
                float d = Vector2.Distance(a.Position, _playerPos);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = a;
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
            if (_eco.Animals.Count == 0) return 18f;
            float interval = Mathf.Clamp(16f - _eco.Charm * 0.5f, 3.5f, 16f);
            if (_eco.Has(Upgrade.Sign)) interval *= 0.75f;
            return interval;
        }

        private void SpawnCustomer()
        {
            if (_customers.Count >= _maxCustomers) return;
            int seat = -1;
            if (_eco.Animals.Count > 0)
            {
                var free = new List<int>();
                for (int i = 0; i < _seatTaken.Length; i++) if (!_seatTaken[i]) free.Add(i);
                if (free.Count == 0) return;
                seat = free[_rng.Next(free.Count)];
                _seatTaken[seat] = true;
            }
            int roll = _rng.Next(3);
            AnimalKind? prefers = roll == 0 ? AnimalKind.Cat : roll == 1 ? AnimalKind.Dog : (AnimalKind?)null;
            var go = new GameObject("Customer");
            var customer = go.AddComponent<Customer>();
            customer.Setup(_art.RandomCustomer(_seed++), _cafe, seat, prefers, 10f + (float)_rng.NextDouble() * 10f, OnCustomerLeave, OnCustomerPet);
            _customers.Add(customer);
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
            int paid = _eco.Pay(tip);
            Float(c.Position + Vector2.up * 1.8f, tip > 0 ? $"+{paid:N0}원 (팁 {tip:N0})" : $"+{paid:N0}원", Gold);
            _audio.Play(_audio.Coin);
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
            if (_inTown)
            {
                // 밖에서 하루가 끝나면 따라오던 아이들과 함께 카페로 돌아온다.
                GoToCafe();
            }
            _audio.Play(_audio.DayEnd);
            Save();
        }

        private void UpdateSummaryInput()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                _eco.NextDay();
                _bowlFood = Mathf.Max(_bowlFood, 0);
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
                int pages = CollectionPages;
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) _collectionPage = (_collectionPage + 1) % pages;
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) _collectionPage = (_collectionPage + pages - 1) % pages;
                return;
            }

            int count = _panel switch
            {
                Panel.Shop => SnackCatalog.All.Length + 1,
                Panel.Office => CafeEconomy.Upgrades.Length,
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
                else
                {
                    ok = _eco.TryBuyKibble();
                    what = $"사료 {CafeEconomy.KibbleBundle}개";
                }
            }
            else
            {
                UpgradeInfo info = CafeEconomy.Upgrades[_cursor];
                ok = _eco.TryBuyUpgrade(info.Key);
                what = info.Name;
                if (ok && info.Key == Upgrade.Tower) _towerSprite.sprite = _art.BigCatTower;
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
            TileGrid grid = _inTown ? (TileGrid)_town : _cafe;
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
            _bannerTimer = 5f;
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
            _panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = bg }, padding = new RectOffset(14, 14, 10, 10) };
            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Ink }, richText = true, wordWrap = true };
            _smallStyle = new GUIStyle(_labelStyle) { fontSize = 13 };
            _titleStyle = new GUIStyle(_labelStyle) { fontSize = 22, fontStyle = FontStyle.Bold };
            _floatStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerCenter, wordWrap = false };
        }

        private Rect WorldRect(Vector2 world, float w, float h)
        {
            Vector3 sp = _cam.WorldToScreenPoint(world);
            return new Rect(sp.x - w / 2f, Screen.height - sp.y - h, w, h);
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
                var r = new Rect(Screen.width / 2f - 300f, 70f, 600f, 40f);
                GUI.Box(r, "", _panelStyle);
                GUI.Label(new Rect(r.x + 10f, r.y + 8f, r.width - 20f, 30f), $"<b>{_banner}</b>", _labelStyle);
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
                snacks += i == _selectedSnack && _inTown ? $"<b>[{name} {_eco.Snacks[i]}]</b> " : $"{name} {_eco.Snacks[i]}  ";
            }
            string where = _inTown ? "동네" : "카페";
            string line1 = $"<b>{_eco.Day}일째</b>  {h:00}:{m:00}  ·  {where}  ·  <b>{_eco.Money:N0}원</b>";
            string line2 = $"식구 {_eco.Animals.Count}/{_eco.Capacity}  (고양이 {_eco.Count(AnimalKind.Cat)} · 강아지 {_eco.Count(AnimalKind.Dog)})  ·  매력 {_eco.Charm:0.0}  ·  그릇 {_bowlFood}/{CafeEconomy.BowlCapacity}  사료 {_eco.Kibble}";
            string line3 = $"간식: {snacks}";
            if (_followers.Count > 0) line3 += $"   ♥ 따라오는 아이 {_followers.Count}";

            var r = new Rect(10f, 10f, 620f, 86f);
            GUI.Box(r, "", _panelStyle);
            GUI.Label(new Rect(r.x + 12f, r.y + 6f, r.width - 24f, 24f), line1, _labelStyle);
            GUI.Label(new Rect(r.x + 12f, r.y + 30f, r.width - 24f, 24f), line2, _smallStyle);
            GUI.Label(new Rect(r.x + 12f, r.y + 54f, r.width - 24f, 24f), line3, _smallStyle);

            string hint = $"Tab 도감 · M 음악 {(_audio.MusicOn ? "켬" : "끔")} · 이동 WASD/방향키 · Shift 살금살금";
            if (Screen.width >= 1000) GUI.Label(new Rect(Screen.width - 330f, 10f, 320f, 24f), hint, _smallStyle);
            else GUI.Label(new Rect(r.x + 12f, r.y + r.height + 2f, r.width - 24f, 24f), hint, _smallStyle);
        }

        private void DrawWorldOverlays()
        {
            foreach (FloatingText t in _texts)
            {
                float alpha = t.Age < TextSeconds ? 1f : 1f - (t.Age - TextSeconds) / TextSeconds;
                _floatStyle.normal.textColor = new Color(t.Color.r, t.Color.g, t.Color.b, alpha);
                GUI.Label(WorldRect(t.Pos, 420f, 26f), t.Text, _floatStyle);
            }

            if (!_inTown) return;
            foreach (StrayView s in _strays)
            {
                if (s.Current != StrayView.State.Hiding) continue;
                float dist = Vector2.Distance(s.Position, _playerPos);
                if (dist > 4.5f) continue;
                Encounter e = s.Encounter;
                Rect r = WorldRect(s.Position + Vector2.up * 1.6f, 150f, 44f);
                GUI.Box(r, "", _panelStyle);
                bool known = _eco.Seen[s.Species.Index];
                GUI.Label(new Rect(r.x + 8f, r.y + 2f, r.width - 16f, 20f), known ? $"<b>{s.Species.Name}</b> {s.Species.PersonalityName}" : "<b>???</b>", _smallStyle);
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
            Rect r = PanelRect(520f, 330f);
            GUI.Box(r, "", _panelStyle);
            GUI.Label(new Rect(r.x + 16f, r.y + 10f, r.width, 30f), "간식 가게", _titleStyle);
            GUI.Label(new Rect(r.x + 16f, r.y + 40f, r.width - 32f, 24f), $"보유 {_eco.Money:N0}원   ↑↓ 고르기 · Space 구입 · Esc 닫기", _smallStyle);
            float y = r.y + 76f;
            for (int i = 0; i < SnackCatalog.All.Length; i++)
            {
                Snack s = SnackCatalog.All[i];
                string line = $"{(i == _cursor ? "▶ " : "   ")}{s.Name}  {s.Price:N0}원   보유 {_eco.Snacks[i]}   고양이 +{s.CatBond:0} / 강아지 +{s.DogBond:0}";
                GUI.Label(new Rect(r.x + 16f, y, r.width - 32f, 26f), i == _cursor ? $"<b>{line}</b>" : line, _labelStyle);
                y += 30f;
            }
            string kibble = $"{(_cursor == SnackCatalog.All.Length ? "▶ " : "   ")}사료 {CafeEconomy.KibbleBundle}개  {CafeEconomy.KibblePrice * CafeEconomy.KibbleBundle:N0}원   보유 {_eco.Kibble}   카페 밥그릇에 채웁니다";
            GUI.Label(new Rect(r.x + 16f, y + 6f, r.width - 32f, 26f), _cursor == SnackCatalog.All.Length ? $"<b>{kibble}</b>" : kibble, _labelStyle);
            GUI.Label(new Rect(r.x + 16f, r.y + r.height - 48f, r.width - 32f, 40f),
                "좋아하는 간식을 주면 친밀도가 1.8배로 오릅니다. 도감(Tab)에서 확인하세요.", _smallStyle);
        }

        private void DrawOffice()
        {
            Rect r = PanelRect(560f, 300f);
            GUI.Box(r, "", _panelStyle);
            GUI.Label(new Rect(r.x + 16f, r.y + 10f, r.width, 30f), "운영 메뉴", _titleStyle);
            GUI.Label(new Rect(r.x + 16f, r.y + 40f, r.width - 32f, 24f), $"보유 {_eco.Money:N0}원   ↑↓ 고르기 · Space 구입 · Esc 닫기", _smallStyle);
            float y = r.y + 76f;
            for (int i = 0; i < CafeEconomy.Upgrades.Length; i++)
            {
                UpgradeInfo u = CafeEconomy.Upgrades[i];
                string state = _eco.Has(u.Key) ? "보유 중" : $"{u.Price:N0}원";
                string line = $"{(i == _cursor ? "▶ " : "   ")}{u.Name}  ·  {state}  ·  {u.Effect}";
                GUI.Label(new Rect(r.x + 16f, y, r.width - 32f, 26f), i == _cursor ? $"<b>{line}</b>" : line, _labelStyle);
                y += 30f;
            }
            GUI.Label(new Rect(r.x + 16f, r.y + r.height - 70f, r.width - 32f, 60f),
                $"입장료 {CafeEconomy.EntryFee:N0}원. 손님이 좋아하는 종류의 아이가 기분 좋게(50 이상) 있으면 한 마리마다 팁 {CafeEconomy.TipPerAnimal:N0}원 (최대 5마리).", _smallStyle);
        }

        private static int CollectionPages => (SpeciesCatalog.All.Length + CollectionPerPage - 1) / CollectionPerPage;

        private void DrawCollection()
        {
            Rect r = PanelRect(760f, 460f);
            GUI.Box(r, "", _panelStyle);
            int seen = 0;
            foreach (bool b in _eco.Seen) if (b) seen++;
            int pages = CollectionPages;
            _collectionPage = Mathf.Clamp(_collectionPage, 0, pages - 1);
            GUI.Label(new Rect(r.x + 16f, r.y + 10f, r.width, 30f), $"도감  {seen}/{SpeciesCatalog.All.Length}   <size=14>({_collectionPage + 1}/{pages} 쪽, ←→ 넘기기)</size>", _titleStyle);
            GUI.Label(new Rect(r.x + 16f, r.y + 40f, r.width - 32f, 24f), "가까이서 본 아이는 이름이, 데려온 아이는 좋아하는 간식이 기록됩니다. Tab/Esc 닫기", _smallStyle);
            float colW = (r.width - 32f) / 2f;
            int first = _collectionPage * CollectionPerPage;
            for (int i = first; i < SpeciesCatalog.All.Length && i < first + CollectionPerPage; i++)
            {
                Species s = SpeciesCatalog.All[i];
                int slot = i - first;
                int col = slot / 8, row = slot % 8;
                var cell = new Rect(r.x + 16f + col * colW, r.y + 72f + row * 44f, colW - 8f, 42f);
                int owned = _eco.OwnedCount(i);
                string line;
                if (!_eco.Seen[i]) line = $"<b>???</b>  {s.KindName} · {s.RarityName}";
                else
                {
                    line = $"<b>{s.Name}</b>  {s.KindName} · {s.RarityName} · {s.PersonalityName} · 매력 {s.Charm}";
                    line += owned > 0 ? $"\n<size=12>좋아하는 간식: {SnackCatalog.All[s.FavoriteSnack].Name} · {owned}마리 · {s.Note}</size>" : $"\n<size=12>{s.Note}</size>";
                }
                GUI.Label(cell, line, _smallStyle);
            }
        }

        private void DrawSummary()
        {
            DayReport rep = _eco.Report;
            Rect r = PanelRect(520f, 330f);
            GUI.Box(r, "", _panelStyle);
            GUI.Label(new Rect(r.x + 16f, r.y + 10f, r.width, 30f), $"{rep.Day}일째 영업 끝", _titleStyle);
            float y = r.y + 50f;
            void Line(string text)
            {
                GUI.Label(new Rect(r.x + 16f, y, r.width - 32f, 26f), text, _labelStyle);
                y += 28f;
            }
            Line($"손님 {rep.Visitors}명  ·  매출 {rep.Income:N0}원 (팁 {rep.Tips:N0}원)");
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
