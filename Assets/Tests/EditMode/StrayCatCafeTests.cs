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
        public void Layout_HasDoorShopAndHidingSpots()
        {
            Assert.AreEqual(2, _map.CafeDoors.Count);
            Assert.AreEqual(2, _map.Shop.Count);
            Assert.IsTrue(_map.HidingSpots.Count >= 20, $"숨을 곳 {_map.HidingSpots.Count}곳");
            Assert.IsTrue(_map.IsWalkable(_map.ExitSpot));
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
            eco.Pay(500);
            eco.NextDay();

            CafeEconomy loaded = CafeEconomy.FromJson(eco.ToJson());
            Assert.AreEqual(eco.Money, loaded.Money);
            Assert.AreEqual(2, loaded.Day);
            Assert.AreEqual(1, loaded.Snacks[3]);
            Assert.IsTrue(loaded.Has(Upgrade.Sign));
            Assert.AreEqual(1, loaded.Animals.Count);
            Assert.AreEqual(6, loaded.Animals[0].SpeciesIndex);
            Assert.AreEqual(42f, loaded.Animals[0].Happiness);
            Assert.IsTrue(loaded.Seen[6]);
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
