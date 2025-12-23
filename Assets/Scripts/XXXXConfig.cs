
using System.Collections.Generic;
using WPZ0325.EasyConfig;

public class XXXXConfig : IEasyConfigBase<XXXXConfig>
{
    public bool BoolTest;

    public int IntTest;

    public string StringTest;

    public List<byte> BytesListTest;

    public XXXXConfig GetDefaultConfigData()
    {
        return new XXXXConfig()
        {
            BoolTest = false,
            IntTest = 1234,
            StringTest = "Hello",
            BytesListTest = new List<byte>()
            {
                1,2,3,4,5
            }
        };
    }
}





