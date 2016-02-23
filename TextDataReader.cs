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

// ReSharper disable once CheckNamespace

namespace TextDataReaderLib
{
    #region Imports

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Globalization;
    using System.Linq;

    #endregion

    // ReSharper disable once PartialTypeWithSinglePart

    static partial class TextDataReader
    {
        public static TextDataReader<TRow> Create<TRow>(IEnumerable<string> headers, IEnumerator<TRow> cursor)
            where TRow : IEnumerable<string> => new TextDataReader<TRow>(headers, cursor);
    }

    // ReSharper disable once PartialTypeWithSinglePart

    partial class TextDataReader<TRow> : DbDataReader where TRow : IEnumerable<string>
    {
        sealed class TextReaderState
        {
            public readonly IEnumerator<TRow> Cursor;
            public readonly string[] Headers;
            public IList<string> Fields;
            public DataTable Schema;

            public TextReaderState(IEnumerable<string> headers, IEnumerator<TRow> cursor)
            {
                Headers = headers.ToArray();
                Cursor = cursor;
            }

            // ReSharper disable once MemberCanBeMadeStatic.Local
            public TResult Return<TResult>(TResult result) => result;
        }

        TextReaderState _state;

        TextReaderState State
        {
            get
            {
                if (_state == null) throw new ObjectDisposedException(nameof(TextDataReader<TRow>));
                return _state;
            }
        }

        public TextDataReader(IEnumerable<string> headers, IEnumerator<TRow> cursor)
        {
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            if (cursor == null) throw new ArgumentNullException(nameof(cursor));

            _state = new TextReaderState(headers.ToArray(), cursor);
        }

        public override void Close()
        {
            _state?.Cursor.Dispose();
            _state = null;
        }

        public override DataTable GetSchemaTable() => State.Schema ?? (State.Schema = CreateSchemaTable(State.Headers));

        static DataTable CreateSchemaTable(string[] headers)
        {
            if (headers == null) throw new ArgumentNullException(nameof(headers));

            var schema = new DataTable("SchemaTable") { Locale = CultureInfo.InvariantCulture };

            var dc = new
            {
                ColumnName    = new DataColumn("ColumnName"   , typeof(string)),
                ColumnOrdinal = new DataColumn("ColumnOrdinal", typeof(int)),
                DataType      = new DataColumn("DataType"     , typeof(Type)),
            };

            var columns = new[]
            {
                dc.ColumnName                                                                 ,
                dc.ColumnOrdinal                                                              ,
                new DataColumn("ColumnSize"         , typeof(int))    { DefaultValue = -1    },
                new DataColumn("NumericPrecision"   , typeof(short))                          ,
                new DataColumn("NumericScale"       , typeof(short))                          ,
                dc.DataType                                                                   ,
                new DataColumn("ProviderType"       , typeof(int))                            ,
                new DataColumn("IsLong"             , typeof(bool))   { DefaultValue = false },
                new DataColumn("AllowDBNull"        , typeof(bool))                           ,
                new DataColumn("IsReadOnly"         , typeof(bool))   { DefaultValue = false },
                new DataColumn("IsRowVersion"       , typeof(bool))   { DefaultValue = false },
                new DataColumn("IsUnique"           , typeof(bool))                           ,
                new DataColumn("IsKey"              , typeof(bool))   { DefaultValue = false },
                new DataColumn("IsAutoIncrement"    , typeof(bool))   { DefaultValue = false },
                new DataColumn("BaseCatalogName"    , typeof(string))                         ,
                new DataColumn("BaseSchemaName"     , typeof(string))                         ,
            };

            Array.ForEach(columns, schema.Columns.Add);

            for (var i = 0; i < headers.Length; i++)
            {
                var header = headers[i];
                var row = schema.NewRow();
                row[dc.ColumnName]    = header;
                row[dc.ColumnOrdinal] = i + 1;
                row[dc.DataType]      = typeof(string);
                schema.Rows.Add(row);
            }

            return schema;
        }

        public override bool NextResult() => State.Return(false);

        public override bool Read()
        {
            while (true)
            {
                var cursor = State.Cursor;
                if (!cursor.MoveNext())
                {
                    State.Fields = null;
                    return false;
                }

                var row = cursor.Current;
                if (row == null)
                {
                    // Technically this would be an implementation error on the
                    // cursor end but forgive and move on.

                    continue;
                }

                var list = row as IList<string>;
                State.Fields = list?.IsReadOnly == true ? list : row.ToArray();

                return true;
            }
        }

        public override int Depth => State.Return(0);
        public override bool IsClosed => _state == null;
        public override int RecordsAffected => State.Return(0);

        public override bool GetBoolean(int ordinal) => (bool) GetValue(ordinal);
        public override byte GetByte(int ordinal) => (byte) GetValue(ordinal);

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            throw new InvalidCastException();
        }

        public override char GetChar(int ordinal) => (char) GetValue(ordinal);

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            var str = GetString(ordinal);
            if (dataOffset > str.Length)
                return 0;
            var count = (int) Math.Min(length, str.Length - dataOffset);
            str.CopyTo((int) dataOffset, buffer, bufferOffset, count);
            return count;
        }

        public override Guid     GetGuid(int ordinal)     => (Guid) GetValue(ordinal);
        public override short    GetInt16(int ordinal)    => (short) GetValue(ordinal);
        public override int      GetInt32(int ordinal)    => (int) GetValue(ordinal);
        public override long     GetInt64(int ordinal)    => (long) GetValue(ordinal);
        public override DateTime GetDateTime(int ordinal) => (DateTime) GetValue(ordinal);
        public override string   GetString(int ordinal)   => State.Fields[ordinal];
        public override decimal  GetDecimal(int ordinal)  => (decimal) GetValue(ordinal);
        public override double   GetDouble(int ordinal)   => (double) GetValue(ordinal);
        public override float    GetFloat(int ordinal)    => (float) GetValue(ordinal);
        public override string   GetName(int ordinal)     => State.Headers[ordinal];

        public override int GetValues(object[] values)
        {
            var count = Math.Min(FieldCount, values.Length);
            for (var i = 0; i < count; i++)
                values[i] = this[i];
            return count;
        }

        public override bool IsDBNull(int ordinal) => State.Return(false);
        public override int FieldCount => State.Fields?.Count ?? State.Headers.Length;

        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => this[GetOrdinal(name)];
        public override bool HasRows => State.Return(true);

        public override int GetOrdinal(string name) => Array.FindIndex(State.Headers, h => string.Equals(name, h, StringComparison.OrdinalIgnoreCase));
        public override string GetDataTypeName(int ordinal) => State.Return(nameof(String));
        public override Type GetFieldType(int ordinal) => State.Return(typeof(string));
        public override object GetValue(int ordinal) => State.Fields[ordinal];
        public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: true);
    }
}