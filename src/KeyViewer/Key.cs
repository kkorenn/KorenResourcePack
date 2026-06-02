
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JipperKeyViewer.KeyViewer
{
    public class Key : MonoBehaviour
    {
        public TextMeshProUGUI text;
        public Image background;
        public Image outline;
        public TextMeshProUGUI value;
        public GameObject rain;
        public byte color;
        public Color rainColor = Color.white;
        public List<RawRain> rainList = new List<RawRain>();
        public bool isPressed;
        public KeyCode keyCode = KeyCode.None;
        public KeyCode ghostKeyCode = KeyCode.None;
        public string countPrefKey;
        public string keyName;
        public int count;
        public int lastCounterValue = int.MinValue;
        public bool isStat;
        public bool isKps;
        public bool isTotal;
        public bool wasGhostPressed;
        public bool wasLimiterGhostPressed;
        public bool ignoredPress;
    }
}
