using Vertica.Data.VerticaClient;

namespace BabyNiProject
{
    public class dbConnection
    {
        public dbConnection()
        {
        }
        public string ConnectionString()
        {
            VerticaConnectionStringBuilder builder = new();

            builder.Host = "10.10.4.231";
            builder.Database = "test";
            builder.Port = 5433;
            builder.User = "bootcamp9";
            builder.Password = "bootcamp92023";

            VerticaConnection _conn = new(builder.ToString());


            return builder.ToString();


        }
    }
}