using UnityEngine;

namespace BenjisHardwiredLogic
{
    class DebugLines
    {
        public static void draw(Vessel vessel, string name, Vector3 pointingAt, Color color)
        {
            var obj = new GameObject(name);
            var line = obj.AddComponent<LineRenderer>();

            line.sortingLayerName = "OnTop";
            line.sortingOrder = 5;
            Vector3 endPoint = vessel.CoM + 20 * (pointingAt.normalized);
            line.SetPosition(0, vessel.CoM);
            line.SetPosition(1, endPoint);
            line.startWidth = 0.05f;
            line.endWidth = 0.01f;
            //line.useWorldSpace = true;

            Material LineMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            line.material = LineMaterial;

            Gradient gradient = new Gradient();
            gradient.SetKeys
                (
                new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
                );

            line.colorGradient = gradient;
            UnityEngine.Object.Destroy(line, 0.06f);
        }
    }
}
