using System;
using System.Collections.Generic;
using UnityEngine;

namespace StrayCatCafe
{
    public enum Upgrade { Sign, Cushion, Tower, Expansion }

    public class UpgradeInfo
    {
        public readonly Upgrade Key;
        public readonly string Name;
        public readonly int Price;
        public readonly string Effect;

        public UpgradeInfo(Upgrade key, string name, int price, string effect)
        {
            Key = key;
            Name = name;
            Price = price;
            Effect = effect;
        }
    }

    public class DayReport
    {
        public int Day;
        public int Income;
        public int Visitors;
        public int Tips;
        public List<string> NewAnimals = new List<string>();
    }

    [Serializable]
    public class SaveData
    {
        public int money;
        public int day;
        public int kibble;
        public int[] snacks;
        public bool[] upgrades;
        public bool[] seen;
        public List<Animal> animals;
    }

    /// <summary>돈, 간식·사료 재고, 업그레이드, 식구들, 도감 기록. 저장·불러오기도 담당합니다.</summary>
    public class CafeEconomy
    {
        public const int StartingMoney = 20000;
        public const int EntryFee = 3000;
        public const int TipPerAnimal = 500;
        public const int BaseCapacity = 6;
        public const int ExpansionCapacity = 4;
        public const int KibblePrice = 300;
        public const int KibbleBundle = 10;
        public const int BowlCapacity = 8;

        public static readonly UpgradeInfo[] Upgrades =
        {
            new UpgradeInfo(Upgrade.Sign, "귀여운 간판", 15000, "손님이 더 자주 옵니다"),
            new UpgradeInfo(Upgrade.Cushion, "푹신한 방석", 12000, "아이들 기분 상한선 +15"),
            new UpgradeInfo(Upgrade.Tower, "큰 캣타워", 20000, "카페 매력 +4"),
            new UpgradeInfo(Upgrade.Expansion, "가게 확장", 30000, "데려올 수 있는 아이 +4"),
        };

        public int Money { get; private set; }
        public int Day { get; private set; } = 1;
        public int Kibble { get; private set; } = 10;
        public int[] Snacks { get; } = new int[SnackCatalog.All.Length];
        public bool[] Owned { get; } = new bool[Upgrades.Length];
        public bool[] Seen { get; } = new bool[SpeciesCatalog.All.Length];
        public List<Animal> Animals { get; } = new List<Animal>();
        public DayReport Report { get; private set; } = new DayReport { Day = 1 };

        public CafeEconomy()
        {
            Money = StartingMoney;
            Snacks[0] = 4;
            Snacks[1] = 2;
        }

        public bool Has(Upgrade u) => Owned[(int)u];
        public int Capacity => BaseCapacity + (Has(Upgrade.Expansion) ? ExpansionCapacity : 0);
        public bool Full => Animals.Count >= Capacity;
        public int TotalSnacks { get { int n = 0; foreach (int s in Snacks) n += s; return n; } }

        /// <summary>카페 매력. 손님이 오는 빈도와 팁을 정합니다.</summary>
        public float Charm
        {
            get
            {
                float c = Has(Upgrade.Tower) ? 4f : 0f;
                foreach (Animal a in Animals) c += a.EffectiveCharm;
                return c;
            }
        }

        public int Count(AnimalKind kind)
        {
            int n = 0;
            foreach (Animal a in Animals) if (a.Species.Kind == kind) n++;
            return n;
        }

        public int OwnedCount(int speciesIndex)
        {
            int n = 0;
            foreach (Animal a in Animals) if (a.SpeciesIndex == speciesIndex) n++;
            return n;
        }

        public bool TryBuySnack(int index)
        {
            Snack s = SnackCatalog.All[index];
            if (Money < s.Price) return false;
            Money -= s.Price;
            Snacks[index]++;
            return true;
        }

        public bool TryBuyKibble()
        {
            int price = KibblePrice * KibbleBundle;
            if (Money < price) return false;
            Money -= price;
            Kibble += KibbleBundle;
            return true;
        }

        public bool TryBuyUpgrade(Upgrade u)
        {
            UpgradeInfo info = Upgrades[(int)u];
            if (Owned[(int)u] || Money < info.Price) return false;
            Money -= info.Price;
            Owned[(int)u] = true;
            return true;
        }

        /// <summary>간식 하나를 꺼냅니다. 없으면 false.</summary>
        public bool UseSnack(int index)
        {
            if (Snacks[index] <= 0) return false;
            Snacks[index]--;
            return true;
        }

        /// <summary>그릇에 사료를 채웁니다. 채운 양을 돌려줍니다.</summary>
        public int FillBowl(int current)
        {
            int room = Mathf.Min(BowlCapacity - current, Kibble);
            if (room <= 0) return 0;
            Kibble -= room;
            return room;
        }

        public void Adopt(Animal animal)
        {
            Animals.Add(animal);
            Seen[animal.SpeciesIndex] = true;
            Report.NewAnimals.Add($"{animal.Name} ({animal.Species.Name})");
        }

        public void Spot(Species species) => Seen[species.Index] = true;

        /// <summary>손님이 나가며 내는 돈. 입장료 + 좋아하는 종류 마리수만큼 팁.</summary>
        public int Pay(int tip)
        {
            Money += EntryFee + tip;
            Report.Income += EntryFee + tip;
            Report.Tips += tip;
            Report.Visitors++;
            return EntryFee + tip;
        }

        public void NextDay()
        {
            Day++;
            Report = new DayReport { Day = Day };
        }

        public string ToJson()
        {
            var data = new SaveData
            {
                money = Money,
                day = Day,
                kibble = Kibble,
                snacks = (int[])Snacks.Clone(),
                upgrades = (bool[])Owned.Clone(),
                seen = (bool[])Seen.Clone(),
                animals = new List<Animal>(Animals),
            };
            return JsonUtility.ToJson(data);
        }

        public static CafeEconomy FromJson(string json)
        {
            var eco = new CafeEconomy();
            if (string.IsNullOrEmpty(json)) return eco;
            SaveData data;
            try { data = JsonUtility.FromJson<SaveData>(json); }
            catch (Exception) { return eco; }
            if (data == null) return eco;

            eco.Money = data.money;
            eco.Day = Mathf.Max(1, data.day);
            eco.Kibble = data.kibble;
            eco.Report = new DayReport { Day = eco.Day };
            Array.Clear(eco.Snacks, 0, eco.Snacks.Length);
            if (data.snacks != null) Array.Copy(data.snacks, eco.Snacks, Mathf.Min(data.snacks.Length, eco.Snacks.Length));
            if (data.upgrades != null) Array.Copy(data.upgrades, eco.Owned, Mathf.Min(data.upgrades.Length, eco.Owned.Length));
            if (data.seen != null) Array.Copy(data.seen, eco.Seen, Mathf.Min(data.seen.Length, eco.Seen.Length));
            if (data.animals != null)
            {
                foreach (Animal a in data.animals)
                {
                    if (a.SpeciesIndex < 0 || a.SpeciesIndex >= SpeciesCatalog.All.Length) continue;
                    eco.Animals.Add(a);
                }
            }
            return eco;
        }
    }
}
