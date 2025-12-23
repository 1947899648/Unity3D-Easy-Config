using System.Collections.Generic;

namespace WPZ0325.EasyConfig.Demo
{
    [System.Serializable]
    public class MyConfig_B : IEasyConfigBase<MyConfig_B>
    {
        public string Item1;

        public List<string> Item2;

        public MySubConfig_B Item3;

        [System.Serializable]
        public class MySubConfig_B
        {
            public string SubItem1;
            public byte SubItem2;
        }
        public MyConfig_B GetDefaultConfigData()
        {
            return new MyConfig_B()
            {
                Item1 = "I AM MYCONFIG_B",
                Item2 = new List<string>()
                {
                    "A","B","C"
                },
                Item3 = new MySubConfig_B()
                {
                    SubItem1 = "I AM SUB ITEM",
                    SubItem2 = 24
                }
            };
        }
    }
}
