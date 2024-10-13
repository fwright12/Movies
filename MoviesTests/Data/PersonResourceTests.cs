namespace MoviesTests.Data
{
    public class PersonResourceTests : ItemResourceTests
    {
        public PersonResourceTests(IProcessorFactory<IEnumerable<KeyValueRequestArgs<Uri>>> processorFactory) : base(processorFactory) { }

        [TestMethod]
        public Task RetrieveAlsoKnownAs() => RetrieveResource(Person.ALSO_KNOWN_AS);

        [TestMethod]
        public Task RetrieveBio() => RetrieveResource(Person.BIO);

        [TestMethod]
        public Task RetrieveBirthday() => RetrieveResource(Person.BIRTHDAY);

        [TestMethod]
        public Task RetrieveBirthplace() => RetrieveResource(Person.BIRTHPLACE);

        [TestMethod]
        public Task RetrieveCredits() => RetrieveResource(Person.CREDITS);

        [TestMethod]
        public Task RetrieveDeathday() => RetrieveResource(Person.DEATHDAY);

        [TestMethod]
        public Task RetrieveGender() => RetrieveResource(Person.GENDER);

        [TestMethod]
        public Task RetrieveName() => RetrieveResource(Person.NAME);

        [TestMethod]
        public Task RetrieveProfilePath() => RetrieveResource(Person.PROFILE_PATH);

        private Task RetrieveResource<T>(Property<T> property) => RetrieveResource(Constants.Person, property);
        private Task RetrieveResource<T>(MultiProperty<T> property) => RetrieveResource(Constants.Person, property);
    }
}
