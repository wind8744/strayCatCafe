using UnityEngine;

namespace StrayCatCafe
{
    /// <summary>
    /// 길에서 만난 아이와 친해지는 과정. 가까이서 빨리 움직이면 경계심이 오르고, 꽉 차면 도망갑니다.
    /// 간식을 건네면 친밀도가 오르고 경계심이 내려가며, 친밀도가 다 차면 카페에 따라옵니다.
    /// 순수 로직이라 Unity 오브젝트 없이 테스트할 수 있습니다.
    /// </summary>
    public class Encounter
    {
        public enum Result { Progress, Bonded, Fled }

        public const float BondGoal = 100f;
        /// <summary>이 거리(칸) 안에서는 주인의 움직임이 경계심에 영향을 줍니다.</summary>
        public const float ThreatRadius = 3f;
        public const float FavoriteMultiplier = 1.8f;

        private readonly System.Random _rng;

        public Species Species { get; }
        public float Wariness { get; private set; }
        public float Bond { get; private set; }
        public bool Fled { get; private set; }
        public bool Bonded => Bond >= BondGoal;
        public bool Over => Fled || Bonded;

        public Encounter(Species species, System.Random rng)
        {
            Species = species;
            _rng = rng;
            Wariness = InitialWariness(species.Personality);
        }

        public static float InitialWariness(Personality personality) => personality switch
        {
            Personality.Shy => 0.5f,
            Personality.Curious => 0.2f,
            Personality.Playful => 0.25f,
            _ => 0.1f,
        };

        /// <summary>매 프레임 주인과의 거리와 살금살금 여부를 알려 줍니다.</summary>
        public void Observe(float distance, bool sneaking, float dt)
        {
            if (Over) return;
            float rate;
            if (distance < ThreatRadius)
            {
                rate = sneaking ? -0.05f : 0.22f * (Species.Personality == Personality.Shy ? 1.6f : 1f);
            }
            else
            {
                rate = -0.08f;
            }
            Wariness = Mathf.Clamp01(Wariness + rate * dt);
            if (Wariness >= 1f) Fled = true;
        }

        public float BondGain(Snack snack)
        {
            float gain = snack.BondFor(Species.Kind);
            if (snack.Index == Species.FavoriteSnack) gain *= FavoriteMultiplier;
            if (Species.Personality == Personality.Lazy) gain *= 1.2f;
            return gain;
        }

        /// <summary>경계심이 높을수록 간식을 받고도 달아날 확률이 높습니다.</summary>
        public float FleeChance => Wariness * (Species.Personality == Personality.Shy ? 0.5f : 0.3f);

        public Result Offer(Snack snack)
        {
            if (Fled) return Result.Fled;
            if (Bonded) return Result.Bonded;

            Bond = Mathf.Min(BondGoal, Bond + BondGain(snack));
            Wariness = Mathf.Max(0f, Wariness - 0.3f);
            if (Bonded) return Result.Bonded;

            if (_rng.NextDouble() < FleeChance)
            {
                Fled = true;
                return Result.Fled;
            }
            return Result.Progress;
        }

        /// <summary>이 간식만 줄 때 친해지기까지 필요한 횟수 (도감 안내용).</summary>
        public int OffersNeeded(Snack snack) => Mathf.CeilToInt(BondGoal / BondGain(snack));
    }
}
