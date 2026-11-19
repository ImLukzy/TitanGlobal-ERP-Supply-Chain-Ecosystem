public class SampleMembers
{
    public SampleMembers() => Value = 1;
    private int Value { get; set; }
    int Compute(int x) => x * 2;
    public static List<int> Items => new List<int>();
}
