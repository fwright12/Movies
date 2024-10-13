namespace MoviesTests.Data.Local
{
    [TestClass]
    public class SqlResourceDAOTests
    {
        public static dBConnection Connection => new dBConnection(DATABASE_FILENAME);
        private static readonly string DATABASE_FILENAME = $"{typeof(SqlResourceDAOTests).FullName}.db";
    }
}
