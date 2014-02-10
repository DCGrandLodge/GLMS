using System;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Reflection;
using System.Text;

static class EFQueryUtils
{
    public class EFChangedException : InvalidOperationException {
        public EFChangedException() : base("Entity Framework internals has changed, please review and fix reflection code") { }
    }
    public static string GetLimitedQuery(ObjectQuery query)
    {
        string select = query.ToTraceString();

        // get private ObjectQueryState ObjectQuery._state;
        // of actual type internal class
        //      System.Data.Objects.ELinq.ELinqQueryState
        object queryState = GetProperty(query, "QueryState");
        AssertNonNullAndOfType(queryState, "System.Data.Objects.ELinq.ELinqQueryState");

        // get protected ObjectQueryExecutionPlan ObjectQueryState._cachedPlan;
        // of actual type internal sealed class
        //      System.Data.Objects.Internal.ObjectQueryExecutionPlan
        object plan = GetField(queryState, "_cachedPlan");
        AssertNonNullAndOfType(plan, "System.Data.Objects.Internal.ObjectQueryExecutionPlan");

        // get internal readonly DbCommandDefinition ObjectQueryExecutionPlan.CommandDefinition;
        // of actual type internal sealed class
        //      System.Data.EntityClient.EntityCommandDefinition
        object commandDefinition = GetField(plan, "CommandDefinition");
        AssertNonNullAndOfType(commandDefinition, "System.Data.EntityClient.EntityCommandDefinition");

        // get private readonly IColumnMapGenerator EntityCommandDefinition._columnMapGenerator;
        // of actual type private sealed class
        //      System.Data.EntityClient.EntityCommandDefinition.ConstantColumnMapGenerator
        object columnMapGenerator;
        try
        {
            columnMapGenerator = GetField(commandDefinition, "_columnMapGenerator");
        }
        catch (EFChangedException)
        {
            columnMapGenerator = GetField(commandDefinition, "_columnMapGenerators", 0);
        }
        AssertNonNullAndOfType(columnMapGenerator, "System.Data.EntityClient.EntityCommandDefinition+ConstantColumnMapGenerator");

        // get private readonly ColumnMap ConstantColumnMapGenerator._columnMap;
        // of actual type internal class
        //      System.Data.Query.InternalTrees.SimpleCollectionColumnMap
        object columnMap = GetField(columnMapGenerator, "_columnMap");
        AssertNonNullAndOfType(columnMap, "System.Data.Query.InternalTrees.SimpleCollectionColumnMap");

        // get internal ColumnMap CollectionColumnMap.Element;
        // of actual type internal class
        //      System.Data.Query.InternalTrees.RecordColumnMap
        object columnMapElement = GetProperty(columnMap, "Element");
        AssertNonNullAndOfType(columnMapElement, "System.Data.Query.InternalTrees.RecordColumnMap");

        // get internal ColumnMap[] StructuredColumnMap.Properties;
        // array of internal abstract class
        //      System.Data.Query.InternalTrees.ColumnMap
        Array columnMapProperties = GetProperty(columnMapElement, "Properties") as Array;
        AssertNonNullAndOfType(columnMapProperties, "System.Data.Query.InternalTrees.ColumnMap[]");

        int n = columnMapProperties.Length;
        var cols = select.Substring(select.IndexOf("SELECT") + 6, select.IndexOf("FROM") - 6)
                    .Split(new char[] { ',' }, columnMapProperties.Length + 1);
        StringBuilder output = new StringBuilder(select.Length * 2);
        for (int i = 0; i < n; ++i)
        {
            // get value at index i in array
            // of actual type internal class
            //      System.Data.Query.InternalTrees.ScalarColumnMap
            object column = columnMapProperties.GetValue(i);
            AssertNonNullAndOfType(column, "System.Data.Query.InternalTrees.ScalarColumnMap");

            //string colName = (string)GetProp(column, "Name");
            // can be used for more advanced bingings

            // get internal int ScalarColumnMap.ColumnPos;
            object columnPositionOfAProperty = GetProperty(column, "ColumnPos");
            AssertNonNullAndOfType(columnPositionOfAProperty, "System.Int32");

            if (output.Length > 0)
            {
                output.Append(", ");
            }
            output.Append(cols[(int)columnPositionOfAProperty]);
        }
        output.Insert(0, "SELECT ");
        output.Append(select.Substring(select.IndexOf("FROM")));
        return output.ToString();
    }

    static object GetProperty(object obj, string propName)
    {
        PropertyInfo prop = obj.GetType().GetProperty(propName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop == null) throw new EFChangedException();
        return prop.GetValue(obj, new object[0]);
    }

    static object GetField(object obj, string fieldName, int index = -1)
    {
        FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) throw new EFChangedException();
        var result = field.GetValue(obj);
        if (index >= 0 && result is Array)
        {
            result = ((Array)result).GetValue(index);
        }
        return result;
    }

    static void AssertNonNullAndOfType(object obj, string fullName)
    {
        if (obj == null) throw new EFChangedException();
        string typeFullName = obj.GetType().FullName;
        if (typeFullName != fullName) throw new EFChangedException();
    }
}
