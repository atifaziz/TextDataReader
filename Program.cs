#region Copyright (c) 2016 Atif Aziz. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//
#endregion

namespace TextDataReaderApp
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using Microsoft.VisualBasic.FileIO;
    using TextDataReaderLib;

    #endregion

    static class Program
    {
        static void Wain()
        {
            using (var stream = typeof(Program).Assembly.GetManifestResourceStream(typeof(Program), "data.csv"))
            using (var parser = new TextFieldParser(stream, Encoding.UTF8, detectEncoding: false, leaveOpen: false)
            {
                Delimiters = new[] { "," }
            })
            using (var e = GetEnumerator(parser))
            {
                e.MoveNext(); // headers
                using (var reader = TextDataReader.Create(e.Current, e))
                {
                    var table = new DataTable("Data");
                    table.Load(reader);
                    table.WriteXml(Console.Out, XmlWriteMode.WriteSchema);
                }
            }
        }

        static IEnumerator<string[]> GetEnumerator(TextFieldParser parser)
        {
            while (!parser.EndOfData)
                yield return parser.ReadFields();
        }

        static int Main()
        {
            try
            {
                Wain();
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.GetBaseException().Message);
                return 0xbad;
            }
        }
    }
}
