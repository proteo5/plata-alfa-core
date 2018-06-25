
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Dynamic;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using System.Data.SqlClient;
using System.Data;

namespace PlataAlfa.core
{
    public class DataSteward
    {
        internal readonly string tableName;
        private readonly string table;
        private readonly string schema;
        private static Dictionary<string, Dictionary<string, dynamic>> columnsInfo = new Dictionary<string, Dictionary<string, dynamic>>();
        private static Dictionary<string, string> idCache = new Dictionary<string, string>();
        private static string connectionString = Program.Configuration["conString"];

        public DataSteward()
        {
            string[] thing = this.GetType().FullName.Split('.');
            Array.Reverse<string>(thing);
            table = thing[0].Replace("DS", string.Empty).ToLower();
            schema = thing[1].ToLower();
            tableName = $"{schema}.{table}";

            if (!columnsInfo.ContainsKey(tableName))
                columnsInfo.Add(tableName, GetColumnInfo());

            if (!idCache.ContainsKey(tableName))
                idCache.Add(tableName, GetPrimaryKey());
        }

        public string GetPrimaryKey()
        {
            string Query = "SELECT COLUMN_NAME as ColumnName " +
                           "FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE " +
                           "WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + QUOTENAME(CONSTRAINT_NAME)), 'IsPrimaryKey') = 1 " +
                           $"AND TABLE_NAME = '{table}' AND TABLE_SCHEMA = '{schema}'";
            dynamic dataSet = this.GetDataSet(Query).FirstOrDefault();
            return ((string)dataSet.ColumnName).ToLower();
        }

        public Dictionary<string, dynamic> GetColumnInfo()
        {
            string Query = "SELECT COLUMN_NAME, DATA_TYPE, NUMERIC_PRECISION " +
                           "FROM INFORMATION_SCHEMA.COLUMNS " +
                           $"WHERE TABLE_NAME = '{table}' AND TABLE_SCHEMA = '{schema}'";
            dynamic dataSet = this.GetDataSet(Query);
            Dictionary<string, dynamic> colInfo =
            new Dictionary<string, dynamic>();
            foreach (var item in dataSet)
            {
                colInfo.Add(((string)item.COLUMN_NAME).ToLower(), item);
            }
            return colInfo;
        }

        public Envelope<List<dynamic>> GetAll()
        {
            return this.GetDataSet();
        }

        public Envelope<List<dynamic>> GetByID(dynamic data)
        {
            string options = $" WHERE {idCache[tableName]} = '{data.id}' ";
            var dataSet = this.GetDataSet("*", options);
            return dataSet;
        }

        public Envelope<dynamic> Insert(dynamic data)
        {
            var dataj = (JObject)data;
            string InsertFields = "";
            string InsertValues = "";
            foreach (var item in dataj.Properties())
            {
                if (item.Name != "AuthUser" && item.Name.ToLower() != idCache[tableName].ToLower())
                {
                    string itemName = item.Name.ToLower();
                    string q = columnsInfo[tableName][itemName].NUMERIC_PRECISION != null
                        || item.Value.Type.ToString() == "Null"
                        ? ""
                        : "'";
                    string value = item.Value.Type.ToString() == "Null" ? "NULL" : item.Value.ToString().Replace("'", "''");
                    if (item.Value.Type.ToString() == "Boolean")
                    {
                        value = (Boolean)item.Value ? "1" : "0";
                    }

                    InsertFields += $"[{itemName}],";
                    InsertValues += $"{q}{value}{q},";
                }

            }
            InsertFields = InsertFields.Remove(InsertFields.Length - 1);
            InsertValues = InsertValues.Remove(InsertValues.Length - 1);

            string query = $"INSERT INTO {tableName} ({InsertFields}) VALUES( {InsertValues})";
            var dataSet = this.Execute(query);
            return dataSet;
        }

        public Envelope<dynamic> Update(dynamic data)
        {
            var dataj = (JObject)data;
            string updateFields = "";
            string idValue = "";
            foreach (var item in dataj.Properties())
            {
                if (item.Name != "AuthUser")
                {

                    string itemName = item.Name.ToLower();
                    string q = columnsInfo[tableName][itemName].NUMERIC_PRECISION != null
                    || item.Value.Type.ToString() == "Null"
                    ? ""
                    : "'";
                    string value = item.Value.Type.ToString() == "Null" ? "NULL" : item.Value.ToString().Replace("'", "''");
                    if (item.Value.Type.ToString() == "Boolean")
                    {
                        value = (Boolean)item.Value ? "1" : "0";
                    }
                    if (item.Name.ToLower() != idCache[tableName].ToLower())
                    {
                        updateFields += $"[{item.Name}] = {q}{value}{q},";
                    }
                    else
                    {
                        idValue = $"{q}{value}{q}"; ;
                    }
                }
            }
            updateFields = updateFields.Remove(updateFields.Length - 1);

            string query = $"UPDATE {tableName} SET {updateFields} WHERE [{idCache[tableName]}] = {idValue} ";
            var dataSet = this.Execute(query);
            return dataSet;
        }

        public Envelope<dynamic> DeleteByID(dynamic data)
        {
            string query = $"Delete {tableName} WHERE [{idCache[tableName]}] = '{data.id}' ";
            var dataSet = this.Execute(query);
            return dataSet;
        }

        public Envelope<dynamic> DeleteAll(dynamic data)
        {
            string query = $"Delete {tableName}";
            var dataSet = this.Execute(query);
            return dataSet;
        }

        internal Envelope<List<dynamic>> GetDataSet(string fields = "*", string options = "")
        {
            string query = $"SELECT {fields} FROM {tableName} {options}";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    try
                    {
                        connection.Open();
                        SqlDataReader reader = cmd.ExecuteReader();

                        List<dynamic> dataSet = new List<dynamic>();

                        while (reader.Read())
                        {
                            dynamic expando = new ExpandoObject();
                            var p = expando as IDictionary<String, object>;

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                p[reader.GetName(i)] = reader[i];
                            }
                            dataSet.Add((dynamic)expando);
                        }
                        reader.Close();

                        return new Envelope<List<dynamic>>() { Result = dataSet.Any() ? "ok" : "empty", Data = dataSet };
                    }
                    catch (Exception ex)
                    {
                        return new Envelope<List<dynamic>>() { Result = "error", Exception = ex, Message = ex.Message };
                    }
                }
            }
        }

        internal List<dynamic> GetDataSet(string queryString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(queryString, connection))
                {
                    try
                    {
                        connection.Open();
                        SqlDataReader reader = cmd.ExecuteReader();

                        List<dynamic> dataSet = new List<dynamic>();

                        while (reader.Read())
                        {
                            dynamic expando = new ExpandoObject();
                            var p = expando as IDictionary<String, object>;

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                p[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader[i];
                            }
                            dataSet.Add((dynamic)expando);
                        }
                        reader.Close();
                        return dataSet;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
        }

        internal List<dynamic> GetSPDataSet(string StoredProcedure, params SQLParam[] sqlParams)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(StoredProcedure, connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    foreach (var item in sqlParams)
                    {
                        cmd.Parameters.Add(item.Param, item.SqlType).Value = item.Value;
                    }

                    try
                    {
                        connection.Open();
                        SqlDataReader reader = cmd.ExecuteReader();

                        List<dynamic> dataSet = new List<dynamic>();

                        while (reader.Read())
                        {
                            dynamic expando = new ExpandoObject();
                            var p = expando as IDictionary<String, object>;

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                p[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader[i];
                            }
                            dataSet.Add((dynamic)expando);
                        }
                        reader.Close();
                        return dataSet;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
        }

        internal Envelope<dynamic> Execute(string queryString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(queryString, connection))
                {
                    try
                    {
                        connection.Open();
                        int affected = cmd.ExecuteNonQuery();

                        return new Envelope<dynamic>() { Result = "ok", Data = new { affected } };
                    }
                    catch (Exception ex)
                    {
                        return new Envelope<dynamic>() { Result = "error", Exception = ex, Message = ex.Message };
                    }
                }
            }
        }
    }
}
