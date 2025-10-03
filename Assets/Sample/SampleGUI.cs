using UnityEngine;

    public class SampleGUI : MonoBehaviour
    {
        public GUISkin guiSkin;

        void OnGUI()
        {
            GUI.skin = guiSkin;

            // Make a background box
            GUI.Box(new Rect(10, 10, 350, 53), "WASD: Movement in layer 0\n" +
                                               "Q: Play golf in layer 0 \n" +
                                               "E: Play shoot in layer 1 with UpperBody mask \n");

            GUI.Box(new Rect(10, 70, 150, 70), "Color Representation \n" +
                                                "Green: Input Authority \n" +
                                                "Yellow: Server \n" +
                                                "Red: Proxy ");
        }
    }
