using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace StrayCatCafe.Tests
{
    public class CafeMapTests
    {
        private CafeMap _map;

        [SetUp]
        public void SetUp() => _map = new CafeMap();

        [Test]
        public void Layout_RowsAreSameWidth()
        {
            foreach (string row in CafeMap.DefaultLayout) Assert.AreEqual(CafeMap.DefaultLayout[0].Length, row.Length);
        }

        [Test]
        public void Layout_HasFurniture()
        {
            Assert.AreEqual(12, _map.Seats.Count);
            Assert.AreEqual(2, _map.Beds.Count);
            Assert.AreEqual(3, _map.Counter.Count);
            Assert.AreEqual(2, _map.Doors.Count);
            Assert.IsTrue(_map.Floor.Count > 50);
        }

        [Test]
        public void WallsAndFurniture_AreNotWalkable()
        {
            Assert.IsFalse(_map.IsWalkable(_map.Bowl));
            Assert.IsFalse(_map.IsWalkable(_map.Tower));
            foreach (Vector2Int c in _map.Counter) Assert.IsFalse(_map.IsWalkable(c));
            Assert.IsFalse(_map.IsWalkable(new Vector2Int(0, 5)), "벽");
            Assert.IsFalse(_map.IsWalkable(new Vector2Int(-1, 0)), "맵 밖");
            Assert.IsTrue(_map.IsWalkable(_map.Spawn));
            Assert.IsTrue(_map.IsWalkable(_map.BowlSpot));
            Assert.IsTrue(_map.IsWalkable(_map.TowerSpot));
        }

        [Test]
        public void QueueSpots_AreInFrontOfCounterAndReachable()
        {
            Assert.AreEqual(6, _map.QueueSpots.Count);
            foreach (Vector2Int q in _map.QueueSpots)
            {
                Assert.IsTrue(_map.IsWalkable(q), q.ToString());
                Assert.IsNotNull(_map.FindPath(_map.Spawn, q), q.ToString());
            }
            Assert.IsTrue(CafeMap.Adjacent(_map.QueueSpots[0], _map.Counter[0]), "앞줄은 카운터 바로 앞");
            Assert.IsTrue(_map.IsWalkable(_map.BaristaSpot));
            Assert.IsTrue(_map.IsNextToCounter(_map.BaristaSpot));
        }

        [Test]
        public void FindPath_ReachesEverySeatBedAndBowlFromDoor()
        {
            var goals = new List<Vector2Int>(_map.Seats);
            goals.AddRange(_map.Beds);
            goals.Add(_map.BowlSpot);
            goals.Add(_map.TowerSpot);
            foreach (Vector2Int goal in goals)
            {
                List<Vector2Int> path = _map.FindPath(_map.Spawn, goal);
                Assert.IsNotNull(path, goal.ToString());
                Assert.AreEqual(goal, path[path.Count - 1]);
            }
        }
    }

    public class HomeMapTests
    {
        private CafeMap _home;

        [SetUp]
        public void SetUp() => _home = new CafeMap(CafeMap.HomeLayout);

        [Test]
        public void Layout_RowsAreSameWidth()
        {
            foreach (string row in CafeMap.HomeLayout) Assert.AreEqual(CafeMap.HomeLayout[0].Length, row.Length);
        }

        [Test]
        public void Home_HasBedBowlAndDoorButNoTowerOrSeats()
        {
            Assert.AreEqual(1, _home.Beds.Count);
            Assert.AreEqual(2, _home.Doors.Count);
            Assert.AreEqual(0, _home.Seats.Count);
            Assert.IsFalse(_home.HasTower);
            Assert.IsFalse(_home.IsWalkable(_home.Bowl));
            Assert.IsFalse(_home.IsWalkable(new Vector2Int(1, 6)), "사람 침대");
            Assert.IsTrue(_home.IsWalkable(new Vector2Int(5, 4)), "러그");
            Assert.IsTrue(_home.Floor.Count > 20);
        }

        [Test]
        public void FindPath_ReachesBedAndBowlFromDoor()
        {
            Assert.IsNotNull(_home.FindPath(_home.Spawn, _home.Beds[0]));
            Assert.IsNotNull(_home.FindPath(_home.Spawn, _home.BowlSpot));
            foreach (Vector2Int f in _home.Floor) Assert.IsNotNull(_home.FindPath(_home.Spawn, f), f.ToString());
        }
    }

    public class TownMapTests
    {
        private TownMap _map;

        [SetUp]
        public void SetUp() => _map = new TownMap();

        [Test]
        public void Layout_RowsAreSameWidth()
        {
            foreach (string row in TownMap.DefaultLayout) Assert.AreEqual(TownMap.DefaultLayout[0].Length, row.Length);
        }

        [Test]
        public void Layout_HasDoorsShopPondAndHidingSpots()
        {
            Assert.AreEqual(2, _map.CafeDoors.Count);
            Assert.AreEqual(1, _map.HomeDoors.Count);
            Assert.AreEqual(2, _map.Shop.Count);
            Assert.IsTrue(_map.HidingSpots.Count >= 30, $"숨을 곳 {_map.HidingSpots.Count}곳");
            Assert.IsTrue(_map.IsWalkable(_map.ExitSpot));
            Assert.IsTrue(_map.IsWalkable(_map.HomeExitSpot));
            Assert.IsTrue(_map.IsWalkable(_map.HomeDoors[0]));
            Assert.IsTrue(_map.IsHomeDoor(_map.HomeDoors[0]));
        }

        [Test]
        public void Pond_IsNotWalkableButItsShoreHidesAnimals()
        {
            int water = 0, shore = 0;
            for (int y = 0; y < _map.Height; y++)
                for (int x = 0; x < _map.Width; x++)
                {
                    var p = new Vector2Int(x, y);
                    if (!_map.IsWater(p)) continue;
                    water++;
                    Assert.IsFalse(_map.IsWalkable(p));
                    foreach (Vector2Int q in new[] { p + Vector2Int.up, p + Vector2Int.down, p + Vector2Int.left, p + Vector2Int.right })
                        if (_map.HidingSpots.Contains(q)) shore++;
                }
            Assert.IsTrue(water >= 15, $"물 {water}칸");
            Assert.IsTrue(shore > 0, "물가에 숨을 곳이 있어야 한다");
        }

        [Test]
        public void HomeExit_ReachesCafeDoorAndShop()
        {
            Assert.IsNotNull(_map.FindPath(_map.HomeExitSpot, _map.ExitSpot));
            Assert.IsNotNull(_map.FindPath(_map.HomeExitSpot, _map.Shop[0] + Vector2Int.down));
        }

        [Test]
        public void HidingSpots_AreWalkableAndFarFromDoor()
        {
            foreach (Vector2Int p in _map.HidingSpots)
            {
                Assert.IsTrue(_map.IsWalkable(p), p.ToString());
                int dist = Mathf.Abs(p.x - _map.ExitSpot.x) + Mathf.Abs(p.y - _map.ExitSpot.y);
                Assert.IsTrue(dist > TownMap.QuietRadius, p.ToString());
            }
        }

        [Test]
        public void FindPath_ReachesEveryHidingSpotAndShopFromDoor()
        {
            foreach (Vector2Int p in _map.HidingSpots) Assert.IsNotNull(_map.FindPath(_map.ExitSpot, p), p.ToString());
            Vector2Int shopFront = _map.Shop[0] + Vector2Int.down;
            Assert.IsTrue(_map.IsNextToShop(shopFront));
            Assert.IsNotNull(_map.FindPath(_map.ExitSpot, shopFront));
        }
    }

    public class CatalogTests
    {
        [Test]
        public void Species_IndicesMatchPositions()
        {
            for (int i = 0; i < SpeciesCatalog.All.Length; i++)
            {
                Species s = SpeciesCatalog.All[i];
                Assert.AreEqual(i, s.Index, s.Name);
                Assert.IsTrue(s.FavoriteSnack >= 0 && s.FavoriteSnack < SnackCatalog.All.Length, s.Name);
                Assert.IsTrue(s.Charm > 0, s.Name);
            }
            for (int i = 0; i < SnackCatalog.All.Length; i++) Assert.AreEqual(i, SnackCatalog.All[i].Index);
        }

        [Test]
        public void Roll_RareIsRarerThanCommon()
        {
            var rng = new System.Random(7);
            int rare = 0, common = 0;
            for (int i = 0; i < 4000; i++)
            {
                Rarity r = SpeciesCatalog.Roll(rng).Rarity;
                if (r == Rarity.Rare) rare++;
                else if (r == Rarity.Common) common++;
            }
            Assert.IsTrue(rare > 0);
            Assert.IsTrue(common > rare * 3, $"흔함 {common}, 희귀 {rare}");
        }
    }

    public class EncounterTests
    {
        private static Species Find(string name) => System.Array.Find(SpeciesCatalog.All, s => s.Name == name);

        [Test]
        public void FavoriteSnack_BondsInFewerOffers()
        {
            Species cheese = Find("치즈냥");
            var fav = new Encounter(cheese, new System.Random(1));
            var plain = new Encounter(cheese, new System.Random(1));
            Snack churu = SnackCatalog.All[cheese.FavoriteSnack];
            Snack anchovy = SnackCatalog.All[0];
            Assert.Less(fav.OffersNeeded(churu), plain.OffersNeeded(anchovy));
        }

        [Test]
        public void Offer_WithZeroWariness_NeverFlees()
        {
            Species lazy = Find("흰둥이");
            var e = new Encounter(lazy, new System.Random(3));
            e.Observe(10f, false, 60f); // 멀리서 오래 두면 경계심이 0 으로
            Assert.AreEqual(0f, e.Wariness);
            Encounter.Result result = Encounter.Result.Progress;
            for (int i = 0; i < 20 && result == Encounter.Result.Progress; i++) result = e.Offer(SnackCatalog.All[1]);
            Assert.AreEqual(Encounter.Result.Bonded, result);
            Assert.IsTrue(e.Bonded);
        }

        [Test]
        public void RunningClose_RaisesWarinessUntilFlee()
        {
            var e = new Encounter(Find("고등어"), new System.Random(5));
            float before = e.Wariness;
            e.Observe(1f, false, 0.5f);
            Assert.Greater(e.Wariness, before);
            e.Observe(1f, false, 10f);
            Assert.IsTrue(e.Fled);
            Assert.AreEqual(Encounter.Result.Fled, e.Offer(SnackCatalog.All[2]));
        }

        [Test]
        public void Injured_NeedsTreatmentThenBondsQuickly()
        {
            var e = new Encounter(Find("고등어"), new System.Random(5), injured: true);
            Assert.IsTrue(e.Injured);
            Assert.IsTrue(e.WasInjured);
            Assert.AreEqual(0f, e.Wariness, "다친 아이는 경계하지 않는다");
            e.Observe(0.5f, false, 30f);
            Assert.IsFalse(e.Fled, "다친 아이는 도망가지 못한다");
            Assert.AreEqual(Encounter.Result.NeedsTreatment, e.Offer(SnackCatalog.All[2]));
            Assert.AreEqual(0f, e.Bond);

            Assert.IsTrue(e.Treat());
            Assert.IsFalse(e.Injured);
            Assert.IsTrue(e.WasInjured);
            Assert.AreEqual(60f, e.Bond);
            Assert.IsFalse(e.Treat(), "두 번 치료할 필요 없음");
            Assert.AreEqual(Encounter.Result.Bonded, e.Offer(SnackCatalog.All[2]));
        }

        [Test]
        public void Sneaking_LowersWariness()
        {
            var e = new Encounter(Find("고등어"), new System.Random(5));
            float before = e.Wariness;
            e.Observe(1f, true, 2f);
            Assert.Less(e.Wariness, before);
        }
    }

    public class EconomyTests
    {
        [Test]
        public void Buying_SpendsMoneyAndAddsStock()
        {
            var eco = new CafeEconomy();
            int before = eco.Money;
            Assert.IsTrue(eco.TryBuySnack(2));
            Assert.AreEqual(before - SnackCatalog.All[2].Price, eco.Money);
            Assert.AreEqual(1, eco.Snacks[2]);
            Assert.IsTrue(eco.TryBuyKibble());
            Assert.AreEqual(20, eco.Kibble);
        }

        [Test]
        public void Upgrade_CannotBeBoughtTwice()
        {
            var eco = new CafeEconomy();
            Assert.IsTrue(eco.TryBuyUpgrade(Upgrade.Cushion));
            Assert.IsFalse(eco.TryBuyUpgrade(Upgrade.Cushion));
            Assert.IsFalse(eco.TryBuyUpgrade(Upgrade.Expansion), "돈 부족");
        }

        [Test]
        public void Adopt_RespectsCapacityAndRecordsCollection()
        {
            var eco = new CafeEconomy();
            var rng = new System.Random(2);
            for (int i = 0; i < CafeEconomy.BaseCapacity; i++) eco.Adopt(Animal.Create(SpeciesCatalog.All[0], rng));
            Assert.IsTrue(eco.Full);
            Assert.IsTrue(eco.Seen[0]);
            Assert.AreEqual(CafeEconomy.BaseCapacity, eco.OwnedCount(0));
            Assert.AreEqual(CafeEconomy.BaseCapacity, eco.Report.NewAnimals.Count);
        }

        [Test]
        public void SaveAndLoad_RoundTrips()
        {
            var eco = new CafeEconomy();
            var rng = new System.Random(4);
            eco.TryBuySnack(3);
            eco.TryBuyUpgrade(Upgrade.Sign);
            eco.Adopt(Animal.Create(SpeciesCatalog.All[6], rng));
            eco.Animals[0].Happiness = 42f;
            eco.PayTip(500);
            eco.NextDay();

            CafeEconomy loaded = CafeEconomy.FromJson(eco.ToJson());
            Assert.AreEqual(eco.Money, loaded.Money);
            Assert.AreEqual(2, loaded.Day);
            Assert.AreEqual(eco.Phase, loaded.Phase);
            Assert.AreEqual(1, loaded.Snacks[3]);
            Assert.IsTrue(loaded.Has(Upgrade.Sign));
            Assert.AreEqual(1, loaded.Animals.Count);
            Assert.AreEqual(6, loaded.Animals[0].SpeciesIndex);
            Assert.AreEqual(42f, loaded.Animals[0].Happiness);
            Assert.IsTrue(loaded.Seen[6]);
        }

        [Test]
        public void Story_StartsAtHomeAndOpensCafeWithThreeAnimals()
        {
            var eco = new CafeEconomy();
            var rng = new System.Random(9);
            Assert.AreEqual(StoryPhase.Intro, eco.Phase);
            Assert.IsFalse(eco.CafeOpen);
            Assert.AreEqual(CafeEconomy.HomeCapacity, eco.Capacity);
            Assert.IsFalse(eco.TryOpenCafe());

            eco.Advance(StoryPhase.FirstCat);
            eco.Adopt(Animal.Create(SpeciesCatalog.All[16], rng));
            eco.Advance(StoryPhase.Collecting);
            eco.Advance(StoryPhase.FirstCat); // 뒤로는 가지 않는다
            Assert.AreEqual(StoryPhase.Collecting, eco.Phase);
            Assert.IsFalse(eco.CanOpenCafe);

            eco.Adopt(Animal.Create(SpeciesCatalog.All[0], rng));
            eco.Adopt(Animal.Create(SpeciesCatalog.All[8], rng));
            Assert.IsTrue(eco.Full, "집은 3마리까지");
            Assert.IsTrue(eco.CanOpenCafe);
            Assert.IsTrue(eco.TryOpenCafe());
            Assert.IsTrue(eco.CafeOpen);
            Assert.AreEqual(CafeEconomy.BaseCapacity, eco.Capacity);
            Assert.IsFalse(eco.Full);

            CafeEconomy loaded = CafeEconomy.FromJson(eco.ToJson());
            Assert.AreEqual(StoryPhase.CafeOpen, loaded.Phase);
        }

        [Test]
        public void FromJson_OldSaveWithAnimalsCountsAsCafeOpen()
        {
            var eco = new CafeEconomy();
            eco.Adopt(Animal.Create(SpeciesCatalog.All[1], new System.Random(1)));
            string json = eco.ToJson().Replace($"\"version\":{CafeEconomy.SaveVersion}", "\"version\":0");
            Assert.IsTrue(json.Contains("\"version\":0"), json);
            CafeEconomy loaded = CafeEconomy.FromJson(json);
            Assert.AreEqual(StoryPhase.CafeOpen, loaded.Phase);
            Assert.AreEqual(StoryPhase.Intro, CafeEconomy.FromJson(eco.ToJson()).Phase, "새 형식 저장은 단계를 그대로 둔다");
        }

        [Test]
        public void Orders_StaffAndKits_WorkAndRoundTrip()
        {
            var eco = new CafeEconomy();
            int start = eco.Money;
            MenuItem latte = MenuCatalog.All[1];
            Assert.AreEqual(latte.Price, eco.PayOrder(latte));
            Assert.AreEqual(start + latte.Price, eco.Money);
            Assert.AreEqual(1, eco.Report.Orders);
            eco.PayTip(1000);
            Assert.AreEqual(1, eco.Report.Visitors);
            Assert.AreEqual(latte.Price + 1000, eco.Report.Income);
            eco.LoseCustomer();
            Assert.AreEqual(1, eco.Report.Lost);

            Assert.AreEqual(0, eco.DailyWages);
            eco.ToggleHire(Staff.Barista);
            Assert.IsTrue(eco.Has(Staff.Barista));
            Assert.AreEqual(CafeEconomy.Staffs[0].Wage, eco.DailyWages);
            int before = eco.Money;
            Assert.AreEqual(CafeEconomy.Staffs[0].Wage, eco.PayWages());
            Assert.AreEqual(before - CafeEconomy.Staffs[0].Wage, eco.Money);
            Assert.AreEqual(CafeEconomy.Staffs[0].Wage, eco.Report.Wages);

            Assert.AreEqual(1, eco.Kits);
            Assert.IsTrue(eco.UseKit());
            Assert.IsFalse(eco.UseKit());
            Assert.IsTrue(eco.TryBuyKit());
            Assert.AreEqual(1, eco.Kits);

            var rescued = Animal.Create(SpeciesCatalog.All[2], new System.Random(1), rescued: true);
            eco.Adopt(rescued);
            Assert.IsTrue(eco.Rescued[2]);
            Assert.AreEqual(1, eco.Report.Rescued);

            CafeEconomy loaded = CafeEconomy.FromJson(eco.ToJson());
            Assert.IsTrue(loaded.Has(Staff.Barista));
            Assert.AreEqual(1, loaded.Kits);
            Assert.IsTrue(loaded.Rescued[2]);
            Assert.IsTrue(loaded.Animals[0].Rescued);
        }

        [Test]
        public void Menu_HasDrinksAndSnacksWithPrices()
        {
            bool drink = false, snack = false;
            for (int i = 0; i < MenuCatalog.All.Length; i++)
            {
                Assert.AreEqual(i, MenuCatalog.All[i].Index);
                Assert.Greater(MenuCatalog.All[i].Price, 0);
                if (MenuCatalog.All[i].Drink) drink = true; else snack = true;
            }
            Assert.IsTrue(drink && snack);
        }

        [Test]
        public void FromJson_GarbageFallsBackToNewGame()
        {
            CafeEconomy eco = CafeEconomy.FromJson("not json");
            Assert.AreEqual(CafeEconomy.StartingMoney, eco.Money);
            Assert.AreEqual(1, eco.Day);
        }

        [Test]
        public void Animal_GetsHungryAndSadWhenStarving()
        {
            var a = Animal.Create(SpeciesCatalog.All[0], new System.Random(1));
            int seconds = 0;
            while (!a.Starving && seconds < 200)
            {
                a.Tick(1f, false);
                seconds++;
            }
            Assert.IsTrue(a.Starving, "굶주림에 도달");
            float happyBefore = a.Happiness;
            for (int i = 0; i < 5; i++) a.Tick(1f, false);
            Assert.Less(a.Happiness, happyBefore);
            a.Eat();
            Assert.AreEqual(0f, a.Hunger);
        }
    }
}
