using System;
using System.Collections.Generic;
using UnityEngine;

namespace StrayCatCafe
{
    public enum Upgrade { Sign, Cushion, Tower, Expansion }

    public enum Staff { Barista, Caretaker }

    public class StaffInfo
    {
        public readonly Staff Key;
        public readonly string Name;
        public readonly int Wage;
        public readonly string Effect;

        public StaffInfo(Staff key, string name, int wage, string effect)
        {
            Key = key;
            Name = name;
            Wage = wage;
            Effect = effect;
        }
    }

    /// <summary>이야기 진행 단계. 집에서 시작해 첫 고양이를 얼떨결에 키우고, 아이들을 모아 카페를 엽니다.</summary>
    public enum StoryPhase { Intro, FirstCat, Collecting, CafeOpen }

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
        public int Orders;
        public int Tips;
        /// <summary>주문을 못 받아 기다리다 돌아간 손님.</summary>
        public int Lost;
        public int Wages;
        public int Rescued;
        public List<string> NewAnimals = new List<string>();
    }

    [Serializable]
    public class SaveData
    {
        /// <summary>0 = 이야기 단계가 생기기 전 저장. 그 경우 식구가 있으면 카페를 운영 중이던 것으로 봅니다.</summary>
        public int version;
        public int money;
        public int day;
        public int phase;
        public int kibble;
        public int kits;
        public int[] snacks;
        public bool[] hired;
        public bool[] rescued;
        public bool[] upgrades;
        public bool[] seen;
        public List<Animal> animals;
    }

    /// <summary>돈, 간식·사료 재고, 업그레이드, 식구들, 도감 기록. 저장·불러오기도 담당합니다.</summary>
    public class CafeEconomy
    {
        public const int StartingMoney = 20000;
        public const int TipPerAnimal = 500;
        public const int BaseCapacity = 6;
        public const int ExpansionCapacity = 4;
        /// <summary>카페를 열기 전 집에서 돌볼 수 있는 마리수.</summary>
        public const int HomeCapacity = 3;
        /// <summary>이만큼 모이면 빈 가게를 빌려 카페를 열 수 있습니다.</summary>
        public const int AnimalsToOpenCafe = 3;
        public const int KibblePrice = 300;
        public const int KibbleBundle = 10;
        public const int BowlCapacity = 8;
        public const int SaveVersion = 3;
        public const int KitPrice = 2000;

        public static readonly StaffInfo[] Staffs =
        {
            new StaffInfo(Staff.Barista, "바리스타 알바", 9000, "주문을 알아서 만들어 줍니다 (외출 중에도)"),
            new StaffInfo(Staff.Caretaker, "돌봄 알바", 7000, "밥그릇을 채우고 아이들을 쓰다듬어 줍니다"),
        };

        public static readonly UpgradeInfo[] Upgrades =
        {
            new UpgradeInfo(Upgrade.Sign, "귀여운 간판", 15000, "손님이 더 자주 옵니다"),
            new UpgradeInfo(Upgrade.Cushion, "푹신한 방석", 12000, "아이들 기분 상한선 +15"),
            new UpgradeInfo(Upgrade.Tower, "큰 캣타워", 20000, "카페 매력 +4"),
            new UpgradeInfo(Upgrade.Expansion, "가게 확장", 30000, "데려올 수 있는 아이 +4"),
        };

        public int Money { get; private set; }
        public int Day { get; private set; } = 1;
        public StoryPhase Phase { get; private set; } = StoryPhase.Intro;
        public bool CafeOpen => Phase == StoryPhase.CafeOpen;
        public bool CanOpenCafe => !CafeOpen && Animals.Count >= AnimalsToOpenCafe;
        public int Kibble { get; private set; } = 10;
        public int Kits { get; private set; } = 1;
        public bool[] Hired { get; } = new bool[Staffs.Length];
        /// <summary>치료해서 데려온 적이 있는 품종.</summary>
        public bool[] Rescued { get; } = new bool[SpeciesCatalog.All.Length];
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
        public bool Has(Staff s) => Hired[(int)s];

        public int DailyWages
        {
            get
            {
                int w = 0;
                for (int i = 0; i < Staffs.Length; i++) if (Hired[i]) w += Staffs[i].Wage;
                return w;
            }
        }

        /// <summary>고용·해고를 바꿉니다. 일당은 하루가 끝날 때 빠져나갑니다.</summary>
        public void ToggleHire(Staff s) => Hired[(int)s] = !Hired[(int)s];

        public bool TryBuyKit()
        {
            if (Money < KitPrice) return false;
            Money -= KitPrice;
            Kits++;
            return true;
        }

        public bool UseKit()
        {
            if (Kits <= 0) return false;
            Kits--;
            return true;
        }

        /// <summary>주문한 커피·간식 값을 받습니다.</summary>
        public int PayOrder(MenuItem item)
        {
            Money += item.Price;
            Report.Income += item.Price;
            Report.Orders++;
            return item.Price;
        }

        public void LoseCustomer() => Report.Lost++;

        /// <summary>하루 끝: 알바 일당을 지급합니다. 돈이 모자라면 있는 만큼만 나갑니다.</summary>
        public int PayWages()
        {
            int wages = Mathf.Min(Money, DailyWages);
            Money -= wages;
            Report.Wages = wages;
            return wages;
        }
        public int Capacity => CafeOpen ? BaseCapacity + (Has(Upgrade.Expansion) ? ExpansionCapacity : 0) : HomeCapacity;

        public void Advance(StoryPhase phase)
        {
            if (phase > Phase) Phase = phase;
        }

        public bool TryOpenCafe()
        {
            if (!CanOpenCafe) return false;
            Phase = StoryPhase.CafeOpen;
            return true;
        }
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
            if (animal.Rescued)
            {
                Rescued[animal.SpeciesIndex] = true;
                Report.Rescued++;
            }
            Report.NewAnimals.Add(animal.Rescued ? $"{animal.Name} ({animal.Species.Name}, 치료)" : $"{animal.Name} ({animal.Species.Name})");
        }

        public void Spot(Species species) => Seen[species.Index] = true;

        /// <summary>손님이 나가며 두고 가는 팁. 좋아하는 종류의 아이가 기분 좋게 있을수록 많습니다.</summary>
        public int PayTip(int tip)
        {
            Money += tip;
            Report.Income += tip;
            Report.Tips += tip;
            Report.Visitors++;
            return tip;
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
                version = SaveVersion,
                money = Money,
                day = Day,
                phase = (int)Phase,
                kibble = Kibble,
                kits = Kits,
                snacks = (int[])Snacks.Clone(),
                hired = (bool[])Hired.Clone(),
                rescued = (bool[])Rescued.Clone(),
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
            eco.Phase = (StoryPhase)Mathf.Clamp(data.phase, 0, (int)StoryPhase.CafeOpen);
            eco.Kibble = data.kibble;
            eco.Kits = data.version >= 3 ? data.kits : 1;
            eco.Report = new DayReport { Day = eco.Day };
            Array.Clear(eco.Snacks, 0, eco.Snacks.Length);
            if (data.snacks != null) Array.Copy(data.snacks, eco.Snacks, Mathf.Min(data.snacks.Length, eco.Snacks.Length));
            if (data.upgrades != null) Array.Copy(data.upgrades, eco.Owned, Mathf.Min(data.upgrades.Length, eco.Owned.Length));
            if (data.seen != null) Array.Copy(data.seen, eco.Seen, Mathf.Min(data.seen.Length, eco.Seen.Length));
            if (data.hired != null) Array.Copy(data.hired, eco.Hired, Mathf.Min(data.hired.Length, eco.Hired.Length));
            if (data.rescued != null) Array.Copy(data.rescued, eco.Rescued, Mathf.Min(data.rescued.Length, eco.Rescued.Length));
            if (data.animals != null)
            {
                foreach (Animal a in data.animals)
                {
                    if (a.SpeciesIndex < 0 || a.SpeciesIndex >= SpeciesCatalog.All.Length) continue;
                    eco.Animals.Add(a);
                }
            }
            // 단계가 생기기 전 저장: 식구가 있으면 이미 카페를 운영하던 것으로 봅니다.
            if (data.version < 2 && eco.Animals.Count > 0) eco.Phase = StoryPhase.CafeOpen;
            return eco;
        }
    }
}
