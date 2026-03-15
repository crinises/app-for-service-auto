using MySql.Data.MySqlClient;

namespace ServiceAuto
{
    public static class DatabaseHelper
    {
        private static readonly string _conn =
            "Server=localhost;Port=3306;Database=service_auto;Uid=root;Pwd=;CharSet=utf8;";

        public static MySqlConnection GetConnection() => new MySqlConnection(_conn);

        public static bool TestConnection()
        {
            try { using var c = GetConnection(); c.Open(); return true; }
            catch { return false; }
        }
    }
}
