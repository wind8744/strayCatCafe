using UnityEngine;

namespace StrayCatCafe
{
    /// <summary>효과음과 배경 음악. 음원 파일 없이 짧은 음을 이어 붙여 런타임에 만듭니다.</summary>
    public class CafeAudio
    {
        private const int SampleRate = 44100;
        private const float Volume = 0.25f;

        private readonly AudioSource _source;
        private readonly AudioSource _music;

        public AudioClip Coin { get; }
        public AudioClip Meow { get; }
        public AudioClip Woof { get; }
        public AudioClip Heart { get; }
        public AudioClip Flee { get; }
        public AudioClip Buy { get; }
        public AudioClip Fill { get; }
        public AudioClip DayEnd { get; }
        public AudioClip Deny { get; }

        public CafeAudio(GameObject host)
        {
            _source = host.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            Coin = Tones("Coin", true, (988f, 0.07f), (1319f, 0.2f));
            Meow = Tones("Meow", false, (880f, 0.08f), (1046f, 0.1f), (784f, 0.18f));
            Woof = Tones("Woof", true, (330f, 0.06f), (247f, 0.14f));
            Heart = Tones("Heart", false, (1319f, 0.06f), (1568f, 0.06f), (2093f, 0.22f));
            Flee = Tones("Flee", true, (784f, 0.05f), (523f, 0.05f), (392f, 0.16f));
            Buy = Tones("Buy", true, (523f, 0.06f), (659f, 0.06f), (784f, 0.06f), (1047f, 0.2f));
            Fill = Tones("Fill", false, (330f, 0.05f), (440f, 0.05f), (554f, 0.1f));
            DayEnd = Tones("DayEnd", false, (523f, 0.15f), (659f, 0.15f), (784f, 0.15f), (1047f, 0.45f));
            Deny = Tones("Deny", true, (220f, 0.1f), (196f, 0.18f));

            _music = host.AddComponent<AudioSource>();
            _music.clip = BuildMusic();
            _music.loop = true;
            _music.volume = 0.45f;
            _music.Play();
        }

        public bool MusicOn => !_music.mute;

        public void ToggleMusic() => _music.mute = !_music.mute;

        public void Play(AudioClip clip) => _source.PlayOneShot(clip, Volume);

        public void PlayVoice(AnimalKind kind) => Play(kind == AnimalKind.Cat ? Meow : Woof);

        /// <summary>배경 음악: 8초짜리 보사노바풍 느긋한 멜로디. 한 칸이 8분음표이고 0 은 쉼표입니다.</summary>
        public static AudioClip BuildMusic()
        {
            const float step = 0.25f;
            const float G4 = 392f, A4 = 440f, B4 = 493.88f, C5 = 523.25f, D5 = 587.33f, E5 = 659.25f, G5 = 783.99f, Fs5 = 739.99f;
            float[] melody =
            {
                G4, B4, D5, G5, Fs5, D5, B4, 0f,
                A4, C5, E5, D5, C5, A4, G4, 0f,
                B4, D5, G5, D5, E5, D5, B4, 0f,
                C5, E5, D5, B4, A4, B4, G4, 0f,
            };
            float[] bass = { 98f, 98f, 87.31f, 87.31f, 123.47f, 123.47f, 110f, 110f };

            int stepSamples = Mathf.RoundToInt(step * SampleRate);
            var data = new float[stepSamples * melody.Length];
            for (int i = 0; i < data.Length; i++)
            {
                int s = i / stepSamples;
                float t = (i % stepSamples) / (float)SampleRate;
                float sample = 0f;
                if (melody[s] > 0f)
                {
                    float w = 2f * Mathf.PI * melody[s] * t;
                    float env = Mathf.Min(1f, t / 0.005f) * Mathf.Exp(-5f * t / step);
                    sample += (Mathf.Sin(w) + 0.25f * Mathf.Sin(2f * w)) * env * 0.16f;
                }
                float bt = (i % (stepSamples * 4)) / (float)SampleRate;
                float bw = 2f * Mathf.PI * bass[s / 4] * bt;
                float benv = Mathf.Exp(-2.5f * bt);
                sample += Mathf.Sin(bw) * benv * 0.12f;
                data[i] = sample;
            }
            var clip = AudioClip.Create("CafeMusic", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Tones(string name, bool square, params (float freq, float seconds)[] notes)
        {
            int total = 0;
            foreach ((float _, float seconds) in notes) total += Mathf.RoundToInt(seconds * SampleRate);
            var data = new float[total];
            int offset = 0;
            foreach ((float freq, float seconds) in notes)
            {
                int n = Mathf.RoundToInt(seconds * SampleRate);
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)SampleRate;
                    float phase = 2f * Mathf.PI * freq * t;
                    float wave = square ? Mathf.Sign(Mathf.Sin(phase)) * 0.5f : Mathf.Sin(phase);
                    float env = Mathf.Min(1f, i / 200f) * Mathf.Min(1f, (n - i) / 400f);
                    data[offset + i] = wave * env;
                }
                offset += n;
            }
            var clip = AudioClip.Create(name, total, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
