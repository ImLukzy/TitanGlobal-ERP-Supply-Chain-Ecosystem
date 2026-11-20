public class SampleLambda
{
    public int Count(IEnumerable<int> values)
    {
        return values.Count(
            value => value > 0
                     && value < 10);
    }

    public async Task<int> CountAsync(IEnumerable<Task<int>> values)
    {
        int[] results = await Task.WhenAll(values.Select(
            async value => await value));
        return results.Length;
    }
}
