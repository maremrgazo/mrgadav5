using Npgsql;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Historian;
using static mrgada;

public static partial class Historian
{
    static string GetConnectionString(string dbName) =>
            $"Host=192.168.64.124;Username=postgres;Password=mare;Database={dbName.ToLower()}";
    public class HistorianTag<T>
    {
        string _name;
        string _egu;
        string _dbType;
        private S7Var<T> _s7Var;
        private string _dbname;
        public HistorianTag(S7Var<T> s7Var, string dbname, string name, string egu)
        {
            _name = name;
            _egu = egu;
            _s7Var = s7Var;
            _dbname = dbname;

            if (typeof(T) == typeof(double)) _dbType = "DOUBLE PRECISION";
            else if (typeof(T) == typeof(int)) _dbType = "INTEGER";
            else if (typeof(T) == typeof(string)) _dbType = "TEXT";
            else if (typeof(T) == typeof(bool)) _dbType = "BOOLEAN";
            else if (typeof(T) == typeof(DateTime)) _dbType = "TIMESTAMPTZ";
            else if (typeof(T) == typeof(float)) _dbType = "FLOAT";
            else throw new Exception("Unsupported data type");

            Initialize();

            s7Var.OnValueChanged += (sender, value) =>
            {
                Historize(value, DateTime.Now);
            };
        }
        public void Initialize()
        {
            using var conn = new NpgsqlConnection(GetConnectionString(_dbname));
            conn.Open();
            // Create the tags table if it doesn't exist
            var createTableQuery = $@"
                CREATE TABLE IF NOT EXISTS {_name} (
                    id SERIAL,
                    timestamp TIMESTAMPTZ NOT NULL,
                    value {_dbType} NOT NULL,
                    PRIMARY KEY (id, timestamp) -- Include 'timestamp' in the primary key
                );
                SELECT create_hypertable('{_name}', 'timestamp', if_not_exists => TRUE);
            ";
            using (var cmd = new NpgsqlCommand(createTableQuery, conn))
            {
                cmd.ExecuteNonQuery();
            }
            conn.Close();
        }
        public void Historize(T value, DateTime dateTime)
        {
            if (value == null) throw new Exception($"Value was null for tag {_name}");
            using var conn = new NpgsqlConnection(GetConnectionString(_dbname));
            conn.Open();
            var insertQuery = $@"
                    INSERT INTO {_name} (timestamp, value)
                    VALUES (@timestamp, @value);
                ";
            using (var cmd = new NpgsqlCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("timestamp", dateTime.ToUniversalTime());
                cmd.Parameters.AddWithValue("value", value);
                cmd.ExecuteNonQuery();
            }
            conn.Close();
        }
        public Dictionary<DateTime, T> RetrieveTimeSeries(DateTime startTime, DateTime endTime)
        {
            Dictionary<DateTime, T> dictionary = new();

            using var conn = new NpgsqlConnection(GetConnectionString(_dbname));
            conn.Open();

            // Define the query to retrieve tags within the specified time range and tag name
            var retrieveQuery = $@"
                    SELECT timestamp, value
                    FROM {_name}
                    WHERE timestamp >= @start_time
                      AND timestamp <= @end_time
                    ORDER BY timestamp;
                ";

            using var cmd = new NpgsqlCommand(retrieveQuery, conn);
            cmd.Parameters.AddWithValue("start_time", startTime);
            cmd.Parameters.AddWithValue("end_time", endTime);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                DateTime TimeStamp = reader.GetDateTime(0);
                T Value = default(T);
                if (typeof(T) == typeof(double))
                {
                    Value = (T)Convert.ChangeType(reader.GetDouble(1), typeof(T));
                }
                dictionary.Add(TimeStamp, Value);
                Console.WriteLine($"Tag: {_name}, Timestamp: {TimeStamp.ToString()}, Value: {Value.ToString()}");
            }

            conn.Close();

            return dictionary;
        }
    }

    public static void Start()
    {
        var CT_6DR_201 = new HistorianTag<float>(mrgada.MRP6.dbAnalogSensorsSCADA.CT_6DR_201.ValueEgu, "MRP6", "CT_6DR_201", "mS/cm");
    }
}