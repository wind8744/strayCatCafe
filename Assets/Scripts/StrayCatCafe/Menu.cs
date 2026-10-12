namespace StrayCatCafe
{
    /// <summary>카페에서 파는 커피와 간식. 손님이 하나씩 주문합니다.</summary>
    public class MenuItem
    {
        public readonly int Index;
        public readonly string Name;
        public readonly int Price;
        public readonly bool Drink;

        public MenuItem(int index, string name, int price, bool drink)
        {
            Index = index;
            Name = name;
            Price = price;
            Drink = drink;
        }
    }

    public static class MenuCatalog
    {
        public static readonly MenuItem[] All =
        {
            new MenuItem(0, "아메리카노", 3500, true),
            new MenuItem(1, "카페라떼", 4500, true),
            new MenuItem(2, "핫초코", 4000, true),
            new MenuItem(3, "고양이 쿠키", 2500, false),
            new MenuItem(4, "치즈케이크", 5500, false),
        };

        public static MenuItem Roll(System.Random rng) => All[rng.Next(All.Length)];
    }
}
