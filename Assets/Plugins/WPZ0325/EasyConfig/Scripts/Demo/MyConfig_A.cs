namespace WPZ0325.EasyConfig.Demo
{
    [System.Serializable]
    public class MyConfig_A : IEasyConfigBase<MyConfig_A>
    {
        public string Item1;
        public int Item2;
        public float Item3;

        public MyConfig_A GetDefaultConfigData()
        {
            return new MyConfig_A()
            {
                Item1 = "I AM MYCONFIG_A",
                Item2 = 123456,
                Item3 = 3.14f,
            };
        }
    }
}

