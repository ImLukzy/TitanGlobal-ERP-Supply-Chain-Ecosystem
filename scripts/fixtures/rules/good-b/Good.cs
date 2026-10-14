public class Sample
{
    public IEnumerable<int> Filter(IEnumerable<int> values)
    {
        return values.Where(value => value > 0);
    }
}
