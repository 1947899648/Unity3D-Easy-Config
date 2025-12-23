using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using WPZ0325.EasyConfig;
using UnityEngine.UI;
using System;

namespace WPZ0325.EasyConfig.Demo
{
    public class EasyConfigTest : MonoBehaviour
    {
        [SerializeField] MyConfig_A _myConfig_A;
        [SerializeField] MyConfig_B _myConfig_B;

        [SerializeField] Button _getConfigA_Btn;
        [SerializeField] Button _getConfigB_Btn;

        [SerializeField] Button _saveConfigA_Btn;
        [SerializeField] Button _saveConfigB_Btn;

        private void Awake()
        {
            _getConfigA_Btn.onClick.AddListener(GetConfigA);
            _getConfigB_Btn.onClick.AddListener(GetConfigB);
            _saveConfigA_Btn.onClick.AddListener(SaveConfigA);
            _saveConfigB_Btn.onClick.AddListener(SaveConfigB);
        }

        private void SaveConfigA()
        {
            EasyConfigHandler<MyConfig_A>.Save(_myConfig_A);
        }

        private void SaveConfigB()
        {
            EasyConfigHandler<MyConfig_B>.Save(_myConfig_B);
        }

        private void GetConfigA()
        {
            _myConfig_A = EasyConfigHandler<MyConfig_A>.Data();
        }

        private void GetConfigB()
        {
            _myConfig_B = EasyConfigHandler<MyConfig_B>.Data();
        }
    }
}

