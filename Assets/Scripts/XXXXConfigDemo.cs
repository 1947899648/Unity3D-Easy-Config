using UnityEngine;
using WPZ0325.EasyConfig;

public class XXXXConfigDemo : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            //Get Config!
            XXXXConfig _configData = EasyConfigHandler<XXXXConfig>.Data();
            Debug.Log(EasyConfigJsonTool.ObjectToJson(_configData));
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            //Save Config!
            XXXXConfig newConfig = new XXXXConfig();
            EasyConfigHandler<XXXXConfig>.Save(newConfig);
        }
    }
}
