

namespace ConfigurationReaderLibrary.Repository
{
    public  interface IConfigurationReader
    {
      
        public Task LoadSettings();
        public void OnTimerElapsed(object state);
        public T GetValue<T>(string name);

    }
}
