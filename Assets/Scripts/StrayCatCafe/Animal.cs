using System;
using UnityEngine;

namespace StrayCatCafe
{
    /// <summary>카페에 사는 아이 한 마리의 저장되는 상태.</summary>
    [Serializable]
    public class Animal
    {
        public int SpeciesIndex;
        public string Name;
        [Range(0f, 100f)] public float Happiness = 70f;
        [Range(0f, 100f)] public float Hunger = 20f;
        /// <summary>손님이 쓰다듬어 준 횟수 (도감 기록).</summary>
        public int Pets;

        public Species Species => SpeciesCatalog.All[SpeciesIndex];

        public bool Hungry => Hunger >= 60f;
        public bool Starving => Hunger >= 90f;

        /// <summary>손님 눈에 보이는 매력. 배고프거나 우울하면 떨어집니다.</summary>
        public float EffectiveCharm => Species.Charm * (0.5f + Happiness / 200f);

        public string MoodName
        {
            get
            {
                if (Starving) return "배고파요!";
                if (Happiness >= 80f) return "행복";
                if (Happiness >= 50f) return "평온";
                if (Happiness >= 25f) return "심심";
                return "우울";
            }
        }

        public static Animal Create(Species species, System.Random rng) => new Animal
        {
            SpeciesIndex = species.Index,
            Name = SpeciesCatalog.RandomName(rng),
            Happiness = 60f,
            Hunger = 30f,
        };

        /// <summary>시간이 흐르면 배가 고파지고, 굶으면 기분이 나빠집니다. cushion 은 방석 업그레이드.</summary>
        public void Tick(float dt, bool cushion)
        {
            Hunger = Mathf.Min(100f, Hunger + dt * 1.6f);
            float target = Starving ? 10f : Hungry ? 45f : cushion ? 85f : 70f;
            float speed = Starving ? 3f : 1.2f;
            Happiness = Mathf.MoveTowards(Happiness, target, speed * dt);
        }

        public void Eat()
        {
            Hunger = 0f;
            Happiness = Mathf.Min(100f, Happiness + 12f);
        }

        public void Pet()
        {
            Pets++;
            Happiness = Mathf.Min(100f, Happiness + 6f);
        }
    }
}
