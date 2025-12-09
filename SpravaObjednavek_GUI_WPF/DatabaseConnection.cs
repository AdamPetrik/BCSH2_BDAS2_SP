using Oracle.ManagedDataAccess.Client;

namespace SpravaObjednavek_GUI_WPF
{
    public class DatabaseConnection
    {
        private static readonly string username = "ST72556";
        private static readonly string password = "6dM8Wb33";
        private static readonly string connectionString =
            $"User Id={username};Password={password};" +
            "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=fei-sql3.upceucebny.cz)(PORT=1521)))(CONNECT_DATA=(SID=BDAS)))";
        public static OracleConnection? GetConnection()
        {
            OracleConnection conn = new OracleConnection(connectionString);
            //try
            //{
            //    conn.Open();
            //}
            //catch (OracleException)
            //{
            //    return null;
            //}
            return conn;
        }
    }
}
