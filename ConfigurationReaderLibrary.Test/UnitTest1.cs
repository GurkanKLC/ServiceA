namespace ConfigurationReaderLibrary.Test
{
    public class UnitTest1
    {
        [Fact]
        public void ServiceASiteNameTest()
        {
            ConfigurationReader configurationReader=new ConfigurationReader("SERVICE-A", "mongodb://root:example@localhost:27017/",10);
            string result = configurationReader.GetValue<string>("SiteName");
            Assert.Equal("soty.io", result);
        }  

        [Fact]
        public void ServiceBSiteNameTest()
        {
            ConfigurationReader configurationReader=new ConfigurationReader("SERVICE-B", "mongodb://root:example@localhost:27017/",10);
            Assert.Throws<Exception>(() => configurationReader.GetValue<string>("SiteName"));
        }
    }
}