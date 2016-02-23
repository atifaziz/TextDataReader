# TextDataReader

`TextDataReader` implements `IDataReader` over any source sequence of string
fields (i.e. rows) supplied as an `IEnumerator<TRow>`, where `TRow` is a
any type that is a sequence of strings (fields or `IEnumerable<string>`).

`TextDataReader` is particularly useful for representing delimited text data
like CSV wherever an `IDataReader` is expected.

```c#
var data =
    from line in File.ReadLines(@"A:\data.csv")
    select line.Split(',');

using (var row = data.GetEnumerator())
{
    row.MoveNext(); // headers
    using (var reader = TextDataReader.Create(row.Current, row))
    {
        var table = new DataTable();
        table.Load(reader);
        // ...
    }
}
```