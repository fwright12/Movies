using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLite
{
    public static class SQLiteExtensions
    {
        public static async Task CreateTable(this SQLiteAsyncConnection database, Table table)
        {
            //Print.Log(SQLTableDecl(table));
            //await database.ExecuteAsync("drop table " + table.Name);
            await database.ExecuteAsync(SQLTableDecl(table));
            await MigrateTable(database, table);
        }

        public static async Task MigrateTable(this SQLiteAsyncConnection database, Table table)
        {
            var cols = (await database.GetTableInfoAsync(table.Name)).Select(col => col.Name).ToList();

            foreach (var column in table.Columns)
            {
                if (!cols.Remove(column.Name))
                {
                    await database.ExecuteAsync(string.Format("alter table {0} add column {1} {2}", table.Name, column.Name, column.SQLType));
                }
            }

            foreach (string colName in cols)
            {
                Console.WriteLine("drop column " + colName);
                //await database.ExecuteAsync(string.Format("alter table {0} drop column {1}", table, colName));
            }
        }

        public static string SQLTableDecl(Table table)
        {
            string decl = "create table if not exists \"" + table.Name + "\"";

            if (table.Columns.Length > 0)
            {
                var colConstraints = table.Columns.Select(column => ("\"" + column.Name + "\" " + column.SQLType + " " + column.ConstraintDecl).Trim());
                var tableConstraints = new string[] { table.ConstraintsDecl }.Where(value => value != null);

                decl += string.Format("({0})", string.Join(", ", colConstraints.Concat(tableConstraints)));
            }

            return decl;
        }

        public static Dictionary<string, string> SelectFromTuple(params string[] values) => SelectFromTuple((IEnumerable<string>)values);

        public static Dictionary<string, string> SelectFromTuple(IEnumerable<string> values)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            IEnumerator<string> itr = values.GetEnumerator();

            for (int i = 1; itr.MoveNext(); i++)
            {
                map[itr.Current] = "Item" + i;
            }

            return map;
        }

        public static string SelectFrom(params string[] values) => SelectFrom(SelectFromTuple(values));
        public static string SelectFrom(IDictionary<string, string> map) => string.Join(", ", map.Select(kvp => string.Format("{0} as {1}", kvp.Key, kvp.Value)));

        public static string SelectFromMap(IDictionary<string, string> map) => string.Join(", ", map.Select(kvp => string.Format("{0} as {1}", kvp.Key, kvp.Value)));

        private static string TupleMap(params string[] values)
        {
            string result = string.Empty;

            for (int i = 0; i < values.Length; i++)
            {
                result += " " + values[i] + " as Item" + (i + 1) + ",";
            }

            return result.Trim(',');
        }

        private static string SelectMap(Dictionary<string, string> map)
        {
            string result = string.Empty;

            foreach (var value in map)
            {
                result += " " + value.Key + " as " + value.Value + ",";
            }

            return result.TrimEnd(',');
        }
    }

    public class Table
    {
        public string Name { get; set; }
        public Column[] Columns { get; set; }
        public string ConstraintsDecl { get; set; }

        public Table(params Column[] columns)
        {
            Columns = columns;
        }

        public static Table FromMap<T>(string name, Dictionary<string, string> map, bool storeDateTimeAsTicks = true, bool storeTimeSpanAsTicks = true) => new Table(map.Select(kvp => new Column(new TableMapping.Column(typeof(T).GetProperty(kvp.Value)), kvp.Key, "", storeDateTimeAsTicks, storeTimeSpanAsTicks)).ToArray())
        {
            Name = name
        };

        public override string ToString() => Name;

        public class Column
        {
            public string Name { get; set; }
            public string SQLType { get; set; }
            public string ConstraintDecl { get; set; }

            public Column(string name, string sqlType, string constraintDecl = "")
            {
                Name = name;
                SQLType = sqlType;
                ConstraintDecl = constraintDecl;
            }

            public Column(TableMapping.Column column, string name = null, string constraintDecl = "", bool storeDateTimeAsTicks = true, bool storeTimeSpanAsTicks = true) : this(name ?? column.Name, Orm.SqlType(column, storeDateTimeAsTicks, storeTimeSpanAsTicks), constraintDecl) { }

            //public static Column FromType<T>(string columnName, string propertyName, bool storeDateTimeAsTicks = true, bool storeTimeSpanAsTicks = true) => new Column(new TableMapping.Column(typeof(T).GetProperty(propertyName)), columnName, storeDateTimeAsTicks, storeTimeSpanAsTicks);

            public override string ToString() => Name;
        }
    }
}
