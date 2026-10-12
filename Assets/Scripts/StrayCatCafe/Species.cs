using UnityEngine;

namespace StrayCatCafe
{
    public enum AnimalKind { Cat, Dog }

    public enum Rarity { Common, Uncommon, Rare }

    /// <summary>성격은 길에서 친해지는 난이도와 카페 안 행동에 영향을 줍니다.</summary>
    public enum Personality { Shy, Curious, Playful, Lazy }

    public enum CoatPattern { Plain, Stripes, Spots, Tuxedo, Calico, Points }

    /// <summary>도감 한 칸. 고양이·강아지 품종(또는 털 색) 하나의 고정 정보입니다.</summary>
    public class Species
    {
        public readonly int Index;
        public readonly string Name;
        public readonly AnimalKind Kind;
        public readonly Rarity Rarity;
        public readonly Personality Personality;
        public readonly CoatPattern Pattern;
        /// <summary>손님을 끌어들이는 매력. 카페 매력 합계에 더해집니다.</summary>
        public readonly int Charm;
        public readonly int FavoriteSnack;
        public readonly Color32 Body;
        public readonly Color32 Mark;
        public readonly Color32 Belly;
        public readonly string Note;
        /// <summary>장모종이면 털을 더 풍성하게 그립니다.</summary>
        public readonly bool LongHair;

        public Species(int index, string name, AnimalKind kind, Rarity rarity, Personality personality,
            CoatPattern pattern, int charm, int favoriteSnack, Color32 body, Color32 mark, Color32 belly, string note,
            bool longHair = false)
        {
            Index = index;
            Name = name;
            Kind = kind;
            Rarity = rarity;
            Personality = personality;
            Pattern = pattern;
            Charm = charm;
            FavoriteSnack = favoriteSnack;
            Body = body;
            Mark = mark;
            Belly = belly;
            Note = note;
            LongHair = longHair;
        }

        public string KindName => Kind == AnimalKind.Cat ? "고양이" : "강아지";

        public string PersonalityName => Personality switch
        {
            Personality.Shy => "소심함",
            Personality.Curious => "호기심",
            Personality.Playful => "장난꾸러기",
            _ => "느긋함",
        };

        public string RarityName => Rarity switch
        {
            Rarity.Common => "흔함",
            Rarity.Uncommon => "가끔",
            _ => "희귀",
        };
    }

    public static class SpeciesCatalog
    {
        private static Color32 C(int r, int g, int b) => new Color32((byte)r, (byte)g, (byte)b, 255);

        public static readonly Species[] All =
        {
            new Species(0, "치즈냥", AnimalKind.Cat, Rarity.Common, Personality.Playful, CoatPattern.Stripes, 3, 2,
                C(236, 164, 72), C(198, 116, 42), C(250, 236, 204), "주황 줄무늬. 츄르에 약하다."),
            new Species(1, "턱시도", AnimalKind.Cat, Rarity.Common, Personality.Curious, CoatPattern.Tuxedo, 3, 1,
                C(48, 48, 58), C(245, 245, 245), C(245, 245, 245), "정장을 입은 듯한 검은 고양이."),
            new Species(2, "고등어", AnimalKind.Cat, Rarity.Common, Personality.Shy, CoatPattern.Stripes, 3, 0,
                C(142, 142, 136), C(84, 84, 86), C(216, 216, 210), "회색 줄무늬. 쉽게 놀란다."),
            new Species(3, "삼색이", AnimalKind.Cat, Rarity.Uncommon, Personality.Curious, CoatPattern.Calico, 4, 2,
                C(246, 246, 240), C(232, 150, 70), C(246, 246, 240), "흰·주황·검정 얼룩. 행운의 고양이."),
            new Species(4, "흰둥이", AnimalKind.Cat, Rarity.Common, Personality.Lazy, CoatPattern.Plain, 2, 1,
                C(248, 248, 244), C(220, 220, 215), C(252, 252, 250), "하루 종일 잠만 잔다."),
            new Species(5, "까망이", AnimalKind.Cat, Rarity.Common, Personality.Shy, CoatPattern.Plain, 2, 0,
                C(42, 42, 50), C(70, 70, 80), C(60, 60, 70), "어둠 속에서 눈만 보인다."),
            new Species(6, "러시안블루", AnimalKind.Cat, Rarity.Rare, Personality.Shy, CoatPattern.Plain, 6, 1,
                C(122, 136, 162), C(100, 112, 136), C(160, 172, 194), "은빛 털의 귀족. 좀처럼 보기 어렵다."),
            new Species(7, "샴", AnimalKind.Cat, Rarity.Uncommon, Personality.Curious, CoatPattern.Points, 4, 2,
                C(240, 226, 200), C(92, 66, 52), C(246, 236, 216), "얼굴·귀·발만 짙은 갈색."),
            new Species(8, "시바견", AnimalKind.Dog, Rarity.Common, Personality.Playful, CoatPattern.Plain, 3, 3,
                C(226, 166, 92), C(200, 140, 70), C(250, 240, 222), "말린 꼬리와 미소. 육포를 좋아한다."),
            new Species(9, "웰시코기", AnimalKind.Dog, Rarity.Uncommon, Personality.Playful, CoatPattern.Tuxedo, 4, 3,
                C(226, 160, 82), C(250, 248, 240), C(250, 248, 240), "짧은 다리에 큰 엉덩이."),
            new Species(10, "말티즈", AnimalKind.Dog, Rarity.Common, Personality.Lazy, CoatPattern.Plain, 3, 1,
                C(250, 250, 246), C(230, 230, 226), C(252, 252, 250), "작고 하얀 솜뭉치."),
            new Species(11, "푸들", AnimalKind.Dog, Rarity.Uncommon, Personality.Curious, CoatPattern.Plain, 4, 3,
                C(192, 150, 110), C(170, 128, 90), C(200, 160, 120), "살구색 곱슬털."),
            new Species(12, "닥스훈트", AnimalKind.Dog, Rarity.Common, Personality.Curious, CoatPattern.Plain, 3, 0,
                C(122, 76, 46), C(96, 56, 32), C(160, 110, 72), "긴 몸통의 소시지 강아지."),
            new Species(13, "진돗개", AnimalKind.Dog, Rarity.Uncommon, Personality.Shy, CoatPattern.Plain, 4, 3,
                C(236, 216, 182), C(214, 190, 150), C(250, 246, 236), "한 번 마음을 열면 끝까지 따른다."),
            new Species(14, "골든리트리버", AnimalKind.Dog, Rarity.Rare, Personality.Playful, CoatPattern.Plain, 6, 3,
                C(222, 186, 112), C(200, 160, 90), C(240, 216, 160), "모두의 친구. 카페의 스타."),
            new Species(15, "달마시안", AnimalKind.Dog, Rarity.Rare, Personality.Playful, CoatPattern.Spots, 5, 3,
                C(248, 248, 246), C(40, 40, 46), C(248, 248, 246), "검은 점박이. 소방서 마스코트."),
            // 저장 호환을 위해 새 품종은 뒤에만 덧붙입니다.
            new Species(16, "코리안숏헤어", AnimalKind.Cat, Rarity.Common, Personality.Curious, CoatPattern.Calico, 3, 0,
                C(236, 236, 230), C(120, 120, 124), C(246, 246, 242), "한국 토종 고양이. 흰 바탕에 회색 얼룩."),
            new Species(17, "페르시안", AnimalKind.Cat, Rarity.Rare, Personality.Lazy, CoatPattern.Plain, 6, 2,
                C(246, 236, 220), C(220, 206, 186), C(250, 244, 232), "납작한 얼굴에 긴 털. 하루 종일 우아하게 쉰다.", true),
            new Species(18, "먼치킨", AnimalKind.Cat, Rarity.Uncommon, Personality.Playful, CoatPattern.Tuxedo, 4, 2,
                C(230, 180, 110), C(250, 246, 236), C(250, 246, 236), "다리가 짧아 뒤뚱뒤뚱 걷는다."),
            new Species(19, "스코티시폴드", AnimalKind.Cat, Rarity.Uncommon, Personality.Lazy, CoatPattern.Plain, 4, 1,
                C(160, 160, 166), C(130, 130, 136), C(200, 200, 204), "접힌 귀와 동그란 얼굴."),
            new Species(20, "메인쿤", AnimalKind.Cat, Rarity.Rare, Personality.Curious, CoatPattern.Stripes, 6, 1,
                C(150, 110, 70), C(96, 66, 40), C(220, 200, 170), "고양이 중 가장 크다. 털이 사자 갈기 같다.", true),
            new Species(21, "벵갈", AnimalKind.Cat, Rarity.Uncommon, Personality.Playful, CoatPattern.Spots, 5, 0,
                C(228, 176, 100), C(90, 60, 30), C(246, 228, 196), "표범 무늬. 물놀이를 좋아한다."),
            new Species(22, "랙돌", AnimalKind.Cat, Rarity.Uncommon, Personality.Lazy, CoatPattern.Points, 5, 2,
                C(246, 240, 230), C(120, 100, 90), C(250, 246, 240), "안으면 인형처럼 축 늘어진다.", true),
            new Species(23, "브리티시숏헤어", AnimalKind.Cat, Rarity.Uncommon, Personality.Shy, CoatPattern.Plain, 4, 1,
                C(120, 130, 150), C(96, 106, 126), C(150, 160, 180), "둥글둥글한 회청색 고양이."),
            new Species(24, "스핑크스", AnimalKind.Cat, Rarity.Rare, Personality.Curious, CoatPattern.Plain, 5, 1,
                C(226, 180, 170), C(200, 150, 140), C(240, 200, 190), "털이 없어 늘 따뜻한 곳을 찾는다."),
            new Species(25, "노르웨이숲", AnimalKind.Cat, Rarity.Uncommon, Personality.Shy, CoatPattern.Stripes, 5, 0,
                C(110, 110, 116), C(60, 60, 66), C(236, 236, 232), "북유럽의 숲 고양이. 추위에 강하다.", true),
            new Species(26, "아비시니안", AnimalKind.Cat, Rarity.Uncommon, Personality.Playful, CoatPattern.Plain, 4, 0,
                C(200, 140, 80), C(150, 90, 50), C(230, 190, 140), "날씬하고 활동적인 황갈색 고양이."),
        };

        public static readonly string[] Names =
        {
            "나비", "보리", "콩이", "두부", "모찌", "하루", "밤이", "초코", "구름", "달이",
            "별이", "감자", "호두", "망고", "루비", "코코", "뭉치", "복이", "쿠키", "체리",
            "순이", "탄이", "우유", "연두", "토리", "마루", "사랑", "뽀삐", "깜지", "노을",
        };

        public static int Weight(Rarity rarity) => rarity switch
        {
            Rarity.Common => 60,
            Rarity.Uncommon => 30,
            _ => 10,
        };

        /// <summary>희귀도 가중치로 품종을 하나 뽑습니다. luck 이 높으면 희귀한 쪽이 조금 더 자주 나옵니다.</summary>
        public static Species Roll(System.Random rng, float luck = 0f)
        {
            int total = 0;
            foreach (Species s in All) total += AdjustedWeight(s, luck);
            int pick = rng.Next(total);
            foreach (Species s in All)
            {
                pick -= AdjustedWeight(s, luck);
                if (pick < 0) return s;
            }
            return All[0];
        }

        private static int AdjustedWeight(Species s, float luck)
        {
            int w = Weight(s.Rarity);
            if (s.Rarity == Rarity.Rare) w += Mathf.RoundToInt(luck * 10f);
            return w;
        }

        public static string RandomName(System.Random rng) => Names[rng.Next(Names.Length)];
    }

    /// <summary>길 위의 아이에게 건네는 간식. 종에 따라 친밀도가 다르게 오릅니다.</summary>
    public class Snack
    {
        public readonly int Index;
        public readonly string Name;
        public readonly int Price;
        public readonly float CatBond;
        public readonly float DogBond;

        public Snack(int index, string name, int price, float catBond, float dogBond)
        {
            Index = index;
            Name = name;
            Price = price;
            CatBond = catBond;
            DogBond = dogBond;
        }

        public float BondFor(AnimalKind kind) => kind == AnimalKind.Cat ? CatBond : DogBond;
    }

    public static class SnackCatalog
    {
        public static readonly Snack[] All =
        {
            new Snack(0, "멸치", 500, 15f, 8f),
            new Snack(1, "참치캔", 1000, 25f, 18f),
            new Snack(2, "츄르", 1500, 40f, 12f),
            new Snack(3, "육포", 1500, 12f, 40f),
        };
    }
}
