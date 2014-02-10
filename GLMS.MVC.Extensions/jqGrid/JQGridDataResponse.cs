using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Runtime.Serialization.Json;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Collections;

namespace GLMS.MVC.Extensions.jqGrid
{
    public class JQGridDataResponse
    {
        public int page { get; set; }
        public int total { get; set; }
        public int records { get; set; }
        public IList<JQGridDataRow> rows { get; set; }
        public object userdata { get; set; }

        public JQGridDataResponse(IEnumerable data, object userdata = null) : this(data.AsQueryable(), userdata) { }
        public JQGridDataResponse(IQueryable data, object userdata = null)
        {
            page = 1;
            total = 1;
            rows = new List<JQGridDataRow>();
            var properties = data.ElementType.GetProperties();
            foreach (var record in data)
            {
                records++;
                rows.Add(new JQGridDataRow(record, properties));
            }
            this.userdata = userdata;
        }

    }

    [DataContract]
    public class JQGridDataResponse<T>
    {
        [DataMember(Name = "page")]
        public int Page { get; set; }
        [DataMember(Name = "total")]
        public int PageCount
        {
            get { if (PageSize == 0) return 1; else return Convert.ToInt32(Math.Ceiling((double)RecordCount / (double)PageSize)); }
            set { }
        }
        [IgnoreDataMember]
        public int PageSize { get; set; }
        [DataMember(Name = "records")]
        public int RecordCount { get; set; }
        [DataMember(Name = "rows")]
        public IList<JQGridDataRow> Rows { get; set; }
        [DataMember(Name = "userdata")]
        public object UserData { get; set; }

        public JQGridDataResponse()
        {
            this.Rows = new List<JQGridDataRow>();
            this.UserData = "";
        }

    }

    [DataContract]
    [KnownType(typeof(List<JQGridDataRow>))]
    public class JQGridDataRow
    {
        #region JavaScriptSerializer properties
        public object id { get; set; }
        public IList<object> cell { get; set; }
        #endregion
        #region DataContractJsonSerializer properties
        [DataMember(Name = "id"), ScriptIgnore]
        public object RowKey { get { return id; } set { id = value; } }
        [DataMember(Name = "cell"), ScriptIgnore]
        public IList<object> Data { get { return cell; } set { cell = value; } }
        #endregion

        public JQGridDataRow(object record) : this(record, record.GetType().GetProperties(), null) { }
        public JQGridDataRow(object record, PropertyInfo[] properties) : this(record, properties, null) { }
        public JQGridDataRow(object record, PropertyInfo[] properties, string[] Columns)
        {
            cell = new List<object>();
            setPrimaryKey(record, properties);
            PropertyInfo[] orderedProperties;
            if ((Columns == null) || (Columns.Length == 0))
            {
                orderedProperties = properties;
            }
            else
            {
                orderedProperties = Columns.Select(column => properties.Single(prop => prop.Name.Equals(column))).ToArray();
            }
            foreach (PropertyInfo property in orderedProperties)
            {
                AddProperty(record, property);
            }
        }

        private void AddProperty(object record, PropertyInfo property)
        {
            if (property.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), true).Any())
            {
                return;
            }
            object value = property.GetValue(record, null);
            string format = property.GetCustomAttributes(typeof(DisplayFormatAttribute), true)
                .OfType<DisplayFormatAttribute>()
                .Select(x => x.DataFormatString)
                .FirstOrDefault();
            if (value == null)
            {
                value = String.Empty;
            }
            else if ((value is IEnumerable) && !(value is string))
            {
                if (property.GetCustomAttributes(typeof(AsSubgridAttribute), true).Any())
                {
                    List<JQGridDataRow> subrows = new List<JQGridDataRow>();
                    foreach (object item in (IEnumerable)value)
                    {
                        subrows.Add(new JQGridDataRow(item, item.GetType().GetProperties(), null));
                    }
                    value = subrows;
                }
                else
                {
                    List<string> values = new List<string>();
                    foreach (object item in (IEnumerable)value)
                    {
                        values.Add(String.Format(format ?? (((item is DateTime) || (item is DateTime?)) ? "{0:g}" : "{0}"), item));
                    }
                    value = String.Join(", ", values);
                }
            }
            if (value is List<JQGridDataRow>)
            {
                cell.Add(value);
            }
            else
            {
                cell.Add(String.Format(format ?? (((value is DateTime) || (value is DateTime?)) ? "{0:g}" : "{0}"), value));
            }
        }

        private void setPrimaryKey(object record, PropertyInfo[] properties)
        {
            string typeName = record.GetType().Name;
            string[] wantIDs = new string[] { typeName + "ID", typeName + "_ID" };
            string primaryKey = null;
            string keyByConvention = null;
            foreach (PropertyInfo prop in properties)
            {
                if (prop.GetCustomAttributes<KeyAttribute>().Any())
                {
                    primaryKey = prop.Name;
                    break;
                }
                if (wantIDs.Contains(prop.Name))
                {
                    keyByConvention = prop.Name;
                }
            }
            primaryKey = primaryKey ?? keyByConvention;
            if (primaryKey == null)
            {
                id = "";
            }
            else
            {
                id = properties.Where(prop => prop.Name == primaryKey)
                    .Select(x => x.GetValue(record, null))
                    .FirstOrDefault() ?? "";
            }
        }
    }

}
