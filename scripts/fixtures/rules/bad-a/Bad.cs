public class Sample
{
    public void Test(IEnumerable<int> values)
    {
        var number = 1;
        foreach (var value in values)
        {
            _ = value;
        }
        var (left, right) = (1, 2);
    }
}
