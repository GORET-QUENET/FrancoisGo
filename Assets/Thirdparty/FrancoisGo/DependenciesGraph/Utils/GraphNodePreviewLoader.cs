using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DependenciesGraph
{
    public class GraphNodePreviewLoader
    {
        private readonly string path;
        private readonly AssetType type;

        public GraphNodePreviewLoader(string path, AssetType type)
        {
            this.path = path;
            this.type = type;
        }

        public Texture2D LoadPreview(Object asset, Vector2 canvasResolution)
        {
            Texture2D image = null;

            if (asset is GameObject prefab)
            {
                Editor editor = Editor.CreateEditor(prefab);
                image = editor.RenderStaticPreview(path, null, 200, 200);
                EditorWindow.DestroyImmediate(editor);

                if (image == null && type == AssetType.Prefab)
                {
                    image = LoadPreview(prefab, canvasResolution);
                }

                if (image != null && IsEmpty(image, Color.gray))
                    image = null;
            }
            else if (asset is Material material)
            {
                Editor editor = Editor.CreateEditor(material);
                image = editor.RenderStaticPreview(path, null, 200, 200);
                EditorWindow.DestroyImmediate(editor);
            }
            else
            {
                if (type == AssetType.Sprite)
                {
                    var sp = (asset as Texture2D);
                    image = sp;
                }
            }

            return image;
        }

        private Texture2D LoadPreview(GameObject prefab, Vector2 canvasResolution)
        {
            Texture2D image = null;

            if (prefab.TryGetComponent(out RectTransform prefabRect))
            {
                var previewRender = new PreviewRenderUtility();
                previewRender.camera.backgroundColor = Color.gray;
                previewRender.camera.clearFlags = CameraClearFlags.SolidColor;
                previewRender.camera.cameraType = CameraType.Game;
                previewRender.camera.farClipPlane = 1000f;
                previewRender.camera.nearClipPlane = 0.1f;

                previewRender.BeginStaticPreview(new Rect(0, 0, canvasResolution.x, canvasResolution.y));

                GameObject canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GraphPath.CanvasPrefab);
                GameObject canvasInstance = previewRender.InstantiatePrefabInScene(canvasPrefab);
                canvasInstance.GetComponent<Canvas>().worldCamera = previewRender.camera;
                canvasInstance.GetComponent<CanvasScaler>().referenceResolution =
                    new Vector2(canvasResolution.x, canvasResolution.y);

                GameObject prefabInstance = previewRender.InstantiatePrefabInScene(prefab);
                prefabInstance.transform.SetParent(canvasInstance.transform, false);

                previewRender.Render();
                image = previewRender.EndStaticPreview();

                previewRender.camera.targetTexture = null;
                previewRender.Cleanup();
            }

            return image;
        }

        private bool IsEmpty(Texture2D tex, Color emptyColor)
        {
            int xGap = tex.width / 100;
            int yGap = tex.height / 100;

            for (int x = 0; x < tex.width; x += xGap)
            {
                for (int y = 0; y < tex.height; y += yGap)
                {
                    if (!ColorsAreClose(tex.GetPixel(x, y), emptyColor))
                        return false;
                }
            }

            return true;
        }

        private bool ColorsAreClose(Color a, Color z, float threshold = 0.35f)
        {
            float r = a.r - z.r;
            float g = a.g - z.g;
            float b = a.b - z.b;

            return (r * r + g * g + b * b) <= threshold * threshold;
        }
    }
}