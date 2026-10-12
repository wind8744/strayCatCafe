using System.Collections.Generic;
using UnityEngine;

namespace StrayCatCafe
{
    public class AnimalSprites
    {
        public Sprite Idle, Walk, Sleep;
    }

    public class CharacterSprites
    {
        public Sprite Idle, Walk1, Walk2, Sit;
    }

    /// <summary>
    /// 모든 그림을 코드로 만듭니다. 타일은 16px 이 한 칸이고 왼쪽 아래가 기준점, 캐릭터·동물은 발 가운데가 기준점입니다.
    /// </summary>
    public class CafeArt
    {
        public const int Tile = 16;

        private static readonly Color32 Outline = new Color32(52, 38, 34, 255);
        private static readonly Color32 Pink = new Color32(240, 150, 160, 255);
        private static readonly Color32 EyeWhite = new Color32(250, 250, 250, 255);
        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        private readonly Dictionary<char, Sprite> _cafeTiles = new Dictionary<char, Sprite>();
        private readonly Dictionary<char, Sprite> _townTiles = new Dictionary<char, Sprite>();
        private readonly Dictionary<int, AnimalSprites> _animals = new Dictionary<int, AnimalSprites>();

        public Sprite Tree { get; }
        public Sprite Lamp { get; }
        public Sprite CatTower { get; }
        public Sprite BigCatTower { get; }
        public Sprite BowlEmpty { get; }
        public Sprite BowlFull { get; }
        public Sprite Heart { get; }
        public Sprite Alert { get; }
        public Sprite Zzz { get; }
        public Sprite Shadow { get; }
        public Sprite Daylight { get; }
        public Sprite Bandage { get; }
        public CharacterSprites Player { get; }
        public CharacterSprites Barista { get; }
        public CharacterSprites Caretaker { get; }

        public CafeArt()
        {
            foreach (char c in "#w.DkcbtBrh") _cafeTiles[c] = BuildCafeTile(c);
            foreach (char c in "TgsrBwhHEcCDSnl") _townTiles[c] = BuildTownTile(c);
            Tree = BuildTree();
            Lamp = BuildLamp();
            CatTower = BuildTower(false);
            BigCatTower = BuildTower(true);
            BowlEmpty = BuildBowl(false);
            BowlFull = BuildBowl(true);
            Heart = BuildHeart();
            Alert = BuildAlert();
            Zzz = BuildZzz();
            Shadow = BuildShadow();
            Daylight = BuildSolid(new Color32(255, 255, 255, 255));
            Bandage = BuildBandage();
            Player = BuildCharacter(new Color32(250, 220, 190, 255), new Color32(90, 60, 50, 255),
                new Color32(250, 250, 250, 255), new Color32(70, 90, 140, 255), new Color32(240, 160, 170, 255));
            Barista = BuildCharacter(new Color32(240, 205, 175, 255), new Color32(40, 30, 30, 255),
                new Color32(250, 250, 250, 255), new Color32(50, 50, 60, 255), new Color32(120, 80, 50, 255));
            Caretaker = BuildCharacter(new Color32(232, 190, 150, 255), new Color32(220, 180, 90, 255),
                new Color32(250, 250, 250, 255), new Color32(90, 70, 60, 255), new Color32(110, 180, 120, 255));
        }

        public Sprite CafeTile(char c) => _cafeTiles.TryGetValue(c, out Sprite s) ? s : _cafeTiles['.'];
        public Sprite TownTile(char c) => _townTiles.TryGetValue(c, out Sprite s) ? s : _townTiles['g'];

        public AnimalSprites Animal(Species species)
        {
            if (_animals.TryGetValue(species.Index, out AnimalSprites cached)) return cached;
            AnimalSprites sprites = BuildAnimal(species);
            _animals[species.Index] = sprites;
            return sprites;
        }

        private static Color32 C(int r, int g, int b, int a = 255) => new Color32((byte)r, (byte)g, (byte)b, (byte)a);

        private static Sprite TileSprite(PixelCanvas canvas) => canvas.ToSprite(Vector2.zero);

        private static Sprite BottomSprite(PixelCanvas canvas) => canvas.ToSprite(new Vector2(0.5f, 0f));

        // ---------- 타일 ----------

        private static Sprite BuildCafeTile(char c)
        {
            var cv = new PixelCanvas(Tile, Tile);
            Color32 wood = C(214, 176, 128), woodDark = C(190, 150, 104);
            Color32 wall = C(238, 226, 198), wallDark = C(214, 198, 164);

            void Floor()
            {
                cv.Rect(0, 0, Tile, Tile, wood);
                for (int y = 0; y < Tile; y += 4)
                {
                    cv.Rect(0, y, Tile, 1, woodDark);
                    int offset = (y / 4) % 2 == 0 ? 3 : 11;
                    cv.Rect(offset, y + 1, 1, 3, woodDark);
                }
            }

            void Wall()
            {
                cv.Rect(0, 0, Tile, Tile, wall);
                for (int y = 0; y < Tile; y += 4)
                {
                    cv.Rect(0, y, Tile, 1, wallDark);
                    int offset = (y / 4) % 2 == 0 ? 0 : 8;
                    cv.Rect(offset, y, 1, 4, wallDark);
                }
                cv.Rect(0, 13, Tile, 3, C(150, 104, 70));
            }

            switch (c)
            {
                case '#':
                    Wall();
                    break;
                case 'w':
                    Wall();
                    cv.Rect(3, 2, 10, 9, C(120, 86, 60));
                    cv.Rect(4, 3, 8, 7, C(170, 214, 240));
                    cv.Rect(7, 3, 1, 7, C(120, 86, 60));
                    cv.Rect(4, 6, 8, 1, C(120, 86, 60));
                    cv.Rect(5, 4, 2, 1, EyeWhite);
                    break;
                case 'D':
                    Floor();
                    cv.Rect(1, 2, 14, 12, C(160, 94, 70));
                    cv.Rect(2, 3, 12, 10, C(200, 130, 96));
                    for (int y = 4; y < 12; y += 3) cv.Rect(3, y, 10, 1, C(160, 94, 70));
                    break;
                case 'k':
                    cv.Rect(0, 0, Tile, Tile, C(150, 100, 60));
                    cv.Rect(0, 0, Tile, 6, C(196, 140, 92));
                    cv.Rect(0, 6, Tile, 1, C(110, 70, 44));
                    cv.Rect(0, 11, Tile, 1, C(120, 80, 50));
                    cv.Rect(5, 1, 5, 4, EyeWhite);
                    cv.Rect(10, 2, 1, 2, EyeWhite);
                    cv.Rect(6, 2, 3, 1, C(120, 80, 50));
                    break;
                case 'c':
                    Floor();
                    cv.Rect(3, 3, 10, 10, Pink);
                    cv.Rect(2, 4, 12, 8, Pink);
                    cv.Rect(4, 2, 8, 12, Pink);
                    cv.Rect(4, 4, 8, 6, C(250, 190, 200));
                    cv.Rect(5, 4, 3, 2, EyeWhite);
                    cv.Rect(3, 12, 10, 2, C(214, 120, 136));
                    break;
                case 'r':
                    Floor();
                    cv.Rect(0, 0, Tile, Tile, C(196, 84, 84));
                    cv.Rect(1, 1, 14, 14, C(222, 110, 100));
                    cv.Rect(3, 3, 10, 10, C(196, 84, 84));
                    cv.Rect(5, 5, 6, 6, C(240, 200, 160));
                    break;
                case 'h':
                    Floor();
                    cv.Rect(0, 0, Tile, Tile, C(130, 90, 60));
                    cv.Rect(1, 1, 14, 14, C(100, 140, 210));
                    cv.Rect(1, 1, 14, 5, C(250, 246, 236));
                    cv.Rect(2, 7, 12, 1, C(80, 116, 180));
                    cv.Rect(2, 11, 12, 1, C(80, 116, 180));
                    break;
                case 'B':
                    Floor();
                    cv.Rect(1, 3, 14, 11, C(150, 110, 70));
                    cv.Rect(2, 4, 12, 9, C(196, 150, 100));
                    cv.Rect(3, 5, 10, 7, C(240, 226, 204));
                    cv.Rect(3, 11, 10, 2, C(222, 200, 170));
                    break;
                default:
                    Floor();
                    break;
            }
            return TileSprite(cv);
        }

        private static Sprite BuildTownTile(char c)
        {
            var cv = new PixelCanvas(Tile, Tile);
            Color32 grass = C(122, 186, 96), grassDark = C(104, 166, 82);
            Color32 sidewalk = C(206, 200, 190), sidewalkLine = C(184, 178, 166);
            Color32 road = C(92, 92, 100);
            Color32 houseWall = C(236, 220, 190), houseRoof = C(190, 84, 72), houseRoofDark = C(160, 66, 58);
            Color32 cafeWall = C(252, 246, 236), cafeRoof = C(232, 142, 164), cafeRoofDark = C(200, 110, 134);

            void Grass()
            {
                cv.Rect(0, 0, Tile, Tile, grass);
                cv.Set(3, 4, grassDark); cv.Set(4, 4, grassDark); cv.Set(11, 9, grassDark); cv.Set(12, 9, grassDark);
                cv.Set(7, 13, grassDark); cv.Set(13, 2, grassDark); cv.Set(1, 11, grassDark);
            }

            void Window(Color32 wallColor)
            {
                cv.Rect(0, 0, Tile, Tile, wallColor);
                cv.Rect(0, 15, Tile, 1, PixelCanvas.Mul(wallColor, 0.8f));
                cv.Rect(4, 4, 8, 8, C(110, 80, 60));
                cv.Rect(5, 5, 6, 6, C(170, 214, 240));
                cv.Rect(7, 5, 1, 6, C(110, 80, 60));
                cv.Rect(5, 7, 6, 1, C(110, 80, 60));
            }

            void Roof(Color32 light, Color32 dark)
            {
                cv.Rect(0, 0, Tile, Tile, light);
                for (int y = 0; y < Tile; y += 4)
                {
                    cv.Rect(0, y + 3, Tile, 1, dark);
                    int offset = (y / 4) % 2 == 0 ? 4 : 12;
                    cv.Rect(offset, y, 1, 3, dark);
                }
            }

            switch (c)
            {
                case 's':
                    cv.Rect(0, 0, Tile, Tile, sidewalk);
                    cv.Rect(0, 7, Tile, 1, sidewalkLine);
                    cv.Rect(0, 15, Tile, 1, sidewalkLine);
                    cv.Rect(7, 0, 1, 7, sidewalkLine);
                    cv.Rect(15, 8, 1, 7, sidewalkLine);
                    break;
                case 'r':
                    cv.Rect(0, 0, Tile, Tile, road);
                    cv.Rect(0, 7, 9, 2, C(226, 196, 80));
                    break;
                case 'B':
                    Grass();
                    cv.Rect(2, 4, 12, 10, C(62, 130, 70));
                    cv.Rect(1, 6, 14, 6, C(62, 130, 70));
                    cv.Rect(4, 2, 8, 13, C(62, 130, 70));
                    cv.Rect(4, 4, 6, 4, C(96, 170, 96));
                    cv.Rect(9, 8, 3, 3, C(96, 170, 96));
                    cv.Set(6, 9, C(230, 90, 110)); cv.Set(11, 5, C(230, 90, 110));
                    break;
                case 'h':
                    Roof(houseRoof, houseRoofDark);
                    break;
                case 'H':
                    Window(houseWall);
                    break;
                case 'E':
                    cv.Rect(0, 0, Tile, Tile, houseWall);
                    cv.Rect(2, 0, 12, 14, C(110, 70, 46));
                    cv.Rect(3, 0, 10, 12, C(150, 98, 66));
                    cv.Rect(5, 2, 6, 3, C(170, 214, 240));
                    cv.Set(11, 8, C(240, 200, 80));
                    break;
                case 'w':
                    cv.Rect(0, 0, Tile, Tile, C(96, 164, 220));
                    cv.Rect(2, 3, 5, 1, C(170, 214, 240));
                    cv.Rect(9, 9, 5, 1, C(170, 214, 240));
                    cv.Rect(5, 12, 3, 1, C(170, 214, 240));
                    cv.Rect(11, 1, 2, 1, C(70, 130, 190));
                    cv.Rect(1, 8, 2, 1, C(70, 130, 190));
                    break;
                case 'c':
                    Roof(cafeRoof, cafeRoofDark);
                    break;
                case 'C':
                    Window(cafeWall);
                    break;
                case 'D':
                    cv.Rect(0, 0, Tile, Tile, cafeWall);
                    cv.Rect(2, 0, 12, 14, C(120, 76, 50));
                    cv.Rect(3, 0, 10, 12, C(180, 118, 82));
                    cv.Rect(5, 2, 6, 4, C(170, 214, 240));
                    cv.Set(11, 8, C(240, 200, 80));
                    break;
                case 'S':
                    cv.Rect(0, 0, Tile, Tile, C(150, 100, 60));
                    cv.Rect(0, 0, Tile, 5, C(236, 90, 90));
                    for (int x = 0; x < Tile; x += 4) cv.Rect(x, 0, 2, 5, EyeWhite);
                    cv.Rect(0, 5, Tile, 1, C(110, 70, 44));
                    cv.Rect(2, 7, 4, 3, C(240, 200, 80));
                    cv.Rect(7, 7, 4, 3, C(90, 160, 220));
                    cv.Rect(12, 7, 3, 3, C(236, 120, 60));
                    break;
                case 'n':
                    Grass();
                    cv.Rect(1, 4, 14, 3, C(160, 110, 70));
                    cv.Rect(1, 8, 14, 3, C(160, 110, 70));
                    cv.Rect(2, 11, 2, 4, C(110, 70, 44));
                    cv.Rect(12, 11, 2, 4, C(110, 70, 44));
                    break;
                default:
                    Grass();
                    break;
            }
            return TileSprite(cv);
        }

        // ---------- 세로로 긴 소품 ----------

        private static Sprite BuildTree()
        {
            var cv = new PixelCanvas(20, 30);
            cv.Rect(8, 20, 4, 10, C(120, 80, 50));
            cv.Rect(3, 6, 14, 16, C(56, 124, 64));
            cv.Rect(1, 10, 18, 9, C(56, 124, 64));
            cv.Rect(5, 2, 10, 6, C(56, 124, 64));
            cv.Rect(6, 5, 6, 5, C(88, 164, 88));
            cv.Rect(3, 12, 5, 4, C(88, 164, 88));
            cv.Rect(12, 13, 4, 3, C(88, 164, 88));
            return BottomSprite(cv);
        }

        private static Sprite BuildLamp()
        {
            var cv = new PixelCanvas(10, 30);
            cv.Rect(4, 6, 2, 24, C(70, 70, 80));
            cv.Rect(2, 28, 6, 2, C(70, 70, 80));
            cv.Rect(2, 0, 6, 7, C(70, 70, 80));
            cv.Rect(3, 1, 4, 5, C(255, 230, 150));
            return BottomSprite(cv);
        }

        private static Sprite BuildTower(bool big)
        {
            int h = big ? 44 : 32;
            var cv = new PixelCanvas(18, h);
            Color32 post = C(200, 170, 130), plate = C(150, 110, 70), carpet = C(236, 196, 160);
            cv.Rect(2, h - 4, 14, 4, plate);
            cv.Rect(7, 6, 4, h - 8, post);
            for (int y = 7; y < h - 8; y += 3) cv.Rect(7, y, 4, 1, C(180, 150, 110));
            cv.Rect(1, h - 16, 10, 3, plate);
            cv.Rect(1, h - 18, 10, 2, carpet);
            cv.Rect(3, 4, 12, 3, plate);
            cv.Rect(3, 2, 12, 2, carpet);
            if (big)
            {
                cv.Rect(8, h - 28, 10, 3, plate);
                cv.Rect(8, h - 30, 10, 2, carpet);
                cv.Rect(11, 0, 6, 4, C(236, 120, 130));
            }
            return BottomSprite(cv);
        }

        private static Sprite BuildBowl(bool full)
        {
            var cv = new PixelCanvas(14, 8);
            cv.Rect(1, 3, 12, 5, C(90, 140, 200));
            cv.Rect(0, 4, 14, 2, C(90, 140, 200));
            cv.Rect(2, 2, 10, 2, C(60, 100, 160));
            cv.Rect(3, 3, 8, 1, full ? C(190, 130, 80) : C(50, 80, 130));
            if (full)
            {
                cv.Rect(4, 1, 6, 2, C(190, 130, 80));
                cv.Set(5, 1, C(150, 100, 60)); cv.Set(8, 2, C(150, 100, 60));
            }
            return BottomSprite(cv);
        }

        private static Sprite BuildHeart()
        {
            var cv = new PixelCanvas(9, 8);
            Color32 red = C(236, 80, 110);
            cv.Rect(1, 0, 3, 2, red); cv.Rect(5, 0, 3, 2, red);
            cv.Rect(0, 1, 9, 3, red);
            cv.Rect(1, 4, 7, 1, red); cv.Rect(2, 5, 5, 1, red); cv.Rect(3, 6, 3, 1, red); cv.Set(4, 7, red);
            cv.Set(2, 1, C(250, 170, 190));
            return cv.ToSprite(new Vector2(0.5f, 0f));
        }

        private static Sprite BuildAlert()
        {
            var cv = new PixelCanvas(6, 10);
            Color32 y = C(250, 200, 60);
            cv.Rect(2, 0, 2, 6, y);
            cv.Rect(1, 0, 4, 2, y);
            cv.Rect(2, 8, 2, 2, y);
            return cv.ToSprite(new Vector2(0.5f, 0f));
        }

        private static Sprite BuildZzz()
        {
            var cv = new PixelCanvas(10, 10);
            Color32 blue = C(120, 150, 220);
            cv.Rect(0, 6, 4, 1, blue); cv.Set(2, 7, blue); cv.Set(1, 8, blue); cv.Rect(0, 9, 4, 1, blue);
            cv.Rect(5, 0, 5, 1, blue); cv.Set(8, 1, blue); cv.Set(7, 2, blue); cv.Set(6, 3, blue); cv.Rect(5, 4, 5, 1, blue);
            return cv.ToSprite(new Vector2(0.5f, 0f));
        }

        private static Sprite BuildBandage()
        {
            var cv = new PixelCanvas(7, 7);
            cv.Rect(0, 0, 7, 7, C(250, 250, 250));
            cv.Rect(3, 1, 1, 5, C(230, 70, 80));
            cv.Rect(1, 3, 5, 1, C(230, 70, 80));
            return cv.ToSprite(new Vector2(0.5f, 0.5f));
        }

        private static Sprite BuildShadow()
        {
            var cv = new PixelCanvas(14, 5);
            Color32 s = C(0, 0, 0, 70);
            cv.Rect(1, 0, 12, 5, s);
            cv.Rect(0, 1, 14, 3, s);
            return cv.ToSprite(new Vector2(0.5f, 0.5f));
        }

        private static Sprite BuildSolid(Color32 c)
        {
            var cv = new PixelCanvas(2, 2);
            cv.Rect(0, 0, 2, 2, c);
            return cv.ToSprite(new Vector2(0.5f, 0.5f), 2);
        }

        // ---------- 사람 ----------

        /// <summary>손님은 머리·옷 색이 저마다 다릅니다.</summary>
        public CharacterSprites RandomCustomer(int seed)
        {
            var rng = new System.Random(seed);
            Color32[] skins = { C(250, 220, 190), C(232, 190, 150), C(190, 140, 100), C(255, 230, 205) };
            Color32[] hairs = { C(40, 30, 30), C(110, 70, 40), C(220, 180, 90), C(150, 60, 40), C(90, 90, 100), C(230, 120, 150) };
            Color32[] shirts = { C(90, 140, 220), C(230, 110, 100), C(100, 180, 120), C(240, 200, 90), C(170, 120, 200), C(250, 250, 250) };
            Color32[] pants = { C(60, 70, 110), C(90, 70, 60), C(50, 50, 60), C(140, 120, 100) };
            return BuildCharacter(skins[rng.Next(skins.Length)], hairs[rng.Next(hairs.Length)],
                shirts[rng.Next(shirts.Length)], pants[rng.Next(pants.Length)], null);
        }

        private static CharacterSprites BuildCharacter(Color32 skin, Color32 hair, Color32 shirt, Color32 pants, Color32? apron)
        {
            Sprite Frame(int leftLeg, int rightLeg, bool sitting)
            {
                var cv = new PixelCanvas(14, 20);
                int legH = sitting ? 2 : 4;
                int bodyTop = sitting ? 5 : 2;
                // 머리
                cv.Rect(4, bodyTop, 6, 6, skin);
                cv.Rect(3, bodyTop - 1, 8, 3, hair);
                cv.Rect(3, bodyTop + 2, 1, 2, hair);
                cv.Rect(10, bodyTop + 2, 1, 2, hair);
                cv.Set(5, bodyTop + 3, Outline);
                cv.Set(8, bodyTop + 3, Outline);
                cv.Set(6, bodyTop + 5, C(220, 120, 120));
                // 몸
                cv.Rect(3, bodyTop + 6, 8, 7, shirt);
                cv.Rect(2, bodyTop + 7, 1, 4, shirt);
                cv.Rect(11, bodyTop + 7, 1, 4, shirt);
                cv.Rect(2, bodyTop + 11, 1, 1, skin);
                cv.Rect(11, bodyTop + 11, 1, 1, skin);
                if (apron.HasValue)
                {
                    cv.Rect(4, bodyTop + 8, 6, 5, apron.Value);
                    cv.Rect(5, bodyTop + 10, 4, 1, PixelCanvas.Mul(apron.Value, 0.85f));
                }
                // 다리
                int legTop = bodyTop + 13;
                cv.Rect(4, legTop + leftLeg, 2, legH - leftLeg, pants);
                cv.Rect(8, legTop + rightLeg, 2, legH - rightLeg, pants);
                cv.Rect(4, legTop + legH, 2, 1, Outline);
                cv.Rect(8, legTop + legH, 2, 1, Outline);
                // 바닥에 맞춰 내려 그리기: 캔버스 높이 20, 발은 아래 끝에
                int shift = 20 - (legTop + legH + 1);
                if (shift != 0)
                {
                    var moved = new PixelCanvas(14, 20);
                    for (int y = 0; y < 20; y++)
                        for (int x = 0; x < 14; x++)
                            moved.Set(x, y + shift, cv.Get(x, y));
                    cv = moved;
                }
                return cv.ToSprite(new Vector2(0.5f, 0f));
            }

            return new CharacterSprites
            {
                Idle = Frame(0, 0, false),
                Walk1 = Frame(1, 0, false),
                Walk2 = Frame(0, 1, false),
                Sit = Frame(0, 0, true),
            };
        }

        // ---------- 동물 ----------

        private static readonly string[] CatIdle =
        {
            "..o...o.........",
            "..oo.oo.........",
            "..opoopo.......o",
            "..obbbbbo......o",
            "..obebbeo.....ob",
            "..obbbbboooooob.",
            "..obbnbbbbbbbbbo",
            "...obbbbbbbbbbbo",
            "...obwwbbbbbbbo.",
            "...obwwbbbbbbbo.",
            "...obbbbbbbbbbo.",
            "...obbbobbbobbo.",
            "...obbo.obo.obo.",
            "...oooo.ooo.ooo.",
        };

        private static readonly string[] CatWalk =
        {
            "..o...o.........",
            "..oo.oo.........",
            "..opoopo.....o..",
            "..obbbbbo...ob..",
            "..obebbeo..obo..",
            "..obbbbbooobo...",
            "..obbnbbbbbbbbo.",
            "...obbbbbbbbbbo.",
            "...obwwbbbbbbbo.",
            "...obwwbbbbbbbo.",
            "...obbbbbbbbbbo.",
            "...obbobbbbobbo.",
            "..obbo..ob..obbo",
            "..oooo..oo..oooo",
        };

        private static readonly string[] CatSleep =
        {
            "................",
            "................",
            "................",
            "................",
            "................",
            "....o..o........",
            "....opop........",
            "...oobbboooooo..",
            "..obbbbbbbbbbbbo",
            "..obcbcbbbbbbbbo",
            "..obbnbbbwwbbbbo",
            "..obbbbbbwwbbbbo",
            "...obbbbbbbbbbo.",
            "....oooooooooo..",
        };

        private static readonly string[] DogIdle =
        {
            "................",
            "...ooo..........",
            "..obbbo........o",
            ".obbbbbo......ob",
            ".obmbebbo....obo",
            ".obmbbbbooooobo.",
            ".obbbnbbbbbbbbo.",
            "..oobbbbbbbbbbo.",
            "...obwwbbbbbbbo.",
            "...obwwbbbbbbbo.",
            "...obbbbbbbbbbo.",
            "...obbbobbbobbo.",
            "...obbo.obo.obo.",
            "...oooo.ooo.ooo.",
        };

        private static readonly string[] DogWalk =
        {
            "................",
            "...ooo..........",
            "..obbbo.........",
            ".obbbbbo.....oo.",
            ".obmbebbo...obbo",
            ".obmbbbbooooobo.",
            ".obbbnbbbbbbbbo.",
            "..oobbbbbbbbbbo.",
            "...obwwbbbbbbbo.",
            "...obwwbbbbbbbo.",
            "...obbbbbbbbbbo.",
            "...obbobbbbobbo.",
            "..obbo..ob..obbo",
            "..oooo..oo..oooo",
        };

        private static readonly string[] DogSleep =
        {
            "................",
            "................",
            "................",
            "................",
            "................",
            "................",
            "....ooo.........",
            "...obbbooooooo..",
            "..obmbbbbbbbbbbo",
            "..obmcbbbbbbbbbo",
            "..obbbnbbwwbbbbo",
            "..obbbbbbwwbbbbo",
            "...obbbbbbbbbbo.",
            "....oooooooooo..",
        };

        private static AnimalSprites BuildAnimal(Species s)
        {
            bool cat = s.Kind == AnimalKind.Cat;
            var palette = new Dictionary<char, Color32>
            {
                ['o'] = Outline,
                ['b'] = s.Body,
                ['w'] = s.Belly,
                ['e'] = s.Index == 5 ? C(240, 220, 90) : Outline,
                ['n'] = cat ? Pink : Outline,
                ['p'] = Pink,
                ['m'] = s.Mark,
                ['c'] = Outline,
            };

            Sprite Build(string[] rows, bool asleep)
            {
                var cv = new PixelCanvas(16, 14);
                cv.Blit(rows, palette, 0, 0);
                ApplyPattern(cv, s, rows, asleep);
                if (s.LongHair) ApplyFluff(cv, s, asleep);
                return cv.ToSprite(new Vector2(0.5f, 0f));
            }

            return new AnimalSprites
            {
                Idle = Build(cat ? CatIdle : DogIdle, false),
                Walk = Build(cat ? CatWalk : DogWalk, false),
                Sleep = Build(cat ? CatSleep : DogSleep, true),
            };
        }

        /// <summary>장모종: 가슴·등·꼬리 둘레에 털 뭉치를 덧그립니다.</summary>
        private static void ApplyFluff(PixelCanvas cv, Species s, bool asleep)
        {
            Color32 fur = s.Body;
            Color32 shade = PixelCanvas.Mul(s.Body, 0.85f);
            void Tuft(int x, int y, Color32 c)
            {
                if (cv.Get(x, y).a == 0) cv.Set(x, y, c);
            }
            if (asleep)
            {
                Tuft(1, 9, fur); Tuft(1, 10, shade); Tuft(1, 11, fur);
                Tuft(2, 12, fur); Tuft(13, 12, fur); Tuft(14, 11, fur);
                return;
            }
            Tuft(2, 7, fur); Tuft(2, 8, shade); Tuft(2, 9, fur); Tuft(1, 8, fur);
            Tuft(15, 7, fur); Tuft(15, 8, shade); Tuft(15, 9, fur);
            Tuft(14, 2, fur); Tuft(15, 3, shade); Tuft(13, 3, fur);
            Tuft(1, 3, fur); Tuft(1, 4, shade); Tuft(9, 3, fur);
        }

        private static bool IsBody(string[] rows, int x, int y) =>
            y >= 0 && y < rows.Length && x >= 0 && x < rows[y].Length && rows[y][x] == 'b';

        /// <summary>털 무늬. 몸통 색 픽셀 위에만 덧칠합니다.</summary>
        private static void ApplyPattern(PixelCanvas cv, Species s, string[] rows, bool asleep)
        {
            Color32 black = C(40, 40, 46);
            void Paint(int x, int y, Color32 c)
            {
                if (IsBody(rows, x, y)) cv.Set(x, y, c);
            }

            switch (s.Pattern)
            {
                case CoatPattern.Stripes:
                    for (int y = 0; y < rows.Length; y++)
                        for (int x = 5; x < 15; x++)
                            if ((x + y) % 3 == 0 && y >= (asleep ? 8 : 5) && y <= 11) Paint(x, y, s.Mark);
                    Paint(4, 3, s.Mark); Paint(6, 3, s.Mark);
                    break;
                case CoatPattern.Spots:
                    int[,] spots = { { 6, 7 }, { 7, 7 }, { 10, 8 }, { 11, 8 }, { 8, 10 }, { 12, 10 }, { 13, 6 }, { 4, 4 }, { 9, 11 }, { 5, 10 } };
                    for (int i = 0; i < spots.GetLength(0); i++) Paint(spots[i, 0], spots[i, 1], s.Mark);
                    break;
                case CoatPattern.Tuxedo:
                    for (int y = 6; y < 13; y++)
                        for (int x = 3; x < 8; x++) Paint(x, y, s.Mark);
                    for (int x = 0; x < 16; x++) Paint(x, 12, s.Mark);
                    Paint(5, 6, s.Mark); Paint(4, 5, s.Mark);
                    break;
                case CoatPattern.Calico:
                    for (int y = 2; y < 6; y++)
                        for (int x = 3; x < 7; x++) Paint(x, y, s.Mark);
                    for (int y = 6; y < 10; y++)
                        for (int x = 9; x < 13; x++) Paint(x, y, black);
                    for (int y = 3; y < 6; y++)
                        for (int x = 13; x < 16; x++) Paint(x, y, black);
                    for (int y = 10; y < 12; y++)
                        for (int x = 5; x < 8; x++) Paint(x, y, s.Mark);
                    break;
                case CoatPattern.Points:
                    for (int y = 0; y < 4; y++)
                        for (int x = 0; x < 9; x++) Paint(x, y, s.Mark);
                    for (int y = 5; y < 7; y++)
                        for (int x = 2; x < 6; x++) Paint(x, y, s.Mark);
                    for (int y = 11; y < 13; y++)
                        for (int x = 0; x < 16; x++) Paint(x, y, s.Mark);
                    for (int y = 0; y < 7; y++)
                        for (int x = 12; x < 16; x++) Paint(x, y, s.Mark);
                    break;
            }
        }
    }
}
