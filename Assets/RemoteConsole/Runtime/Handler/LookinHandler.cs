using RConsole.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RConsole.Runtime
{
    public class LookinHandler : IHandler
    {
        public override void OnEnable()
        {
            RConsoleCtrl.Instance.WebSocket.On(EnvelopeKind.S2CLookin, (byte)SubLookIn.LookIn, OnLookIn);
        }

        public override void OnDisable()
        {
            RConsoleCtrl.Instance.WebSocket.Off(EnvelopeKind.S2CLookin, (byte)SubLookIn.LookIn, OnLookIn);
        }

        private Envelope OnLookIn(Envelope env)
        {
            var req = (LookInViewModel)env.Model;
            var root = BuildTree(req.Path);
            env.Model = root;
            return env;
        }

        private LookInViewModel BuildTree(string path)
        {
            if (string.IsNullOrEmpty(path) || path == "/")
            {
                var scene = SceneManager.GetActiveScene();
                var model = new LookInViewModel
                {
                    Name = scene.name,
                    Path = "/",
                    IsActive = true,
                    Rect = new Rect()
                };
                var roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    var child = BuildNode(roots[i], "/" + roots[i].name);
                    model.Children.Add(child);
                }

                return model;
            }

            var go = FindByPath(path);
            if (go == null)
            {
                return new LookInViewModel
                {
                    Name = path,
                    Path = path,
                    IsActive = false,
                    Rect = new Rect()
                };
            }

            return BuildNode(go, path);
        }

        private LookInViewModel BuildNode(GameObject go, string currentPath)
        {
            var model = new LookInViewModel
            {
                Name = go.name,
                Path = currentPath,
                IsActive = go.activeInHierarchy,
                Rect = GetNodeRect(go)
            };
            var t = go.transform;
            var count = t.childCount;
            for (int i = 0; i < count; i++)
            {
                var c = t.GetChild(i).gameObject;
                var childPath = currentPath.EndsWith("/") ? currentPath + c.name : currentPath + "/" + c.name;
                model.Children.Add(BuildNode(c, childPath));
            }

            return model;
        }

        private GameObject FindByPath(string path)
        {
            var scene = SceneManager.GetActiveScene();
            var segs = path.Split('/');
            var names = new System.Collections.Generic.List<string>();
            for (int i = 0; i < segs.Length; i++)
            {
                if (!string.IsNullOrEmpty(segs[i])) names.Add(segs[i]);
            }

            if (names.Count == 0) return null;
            GameObject root = null;
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == names[0])
                {
                    root = roots[i];
                    break;
                }
            }

            if (root == null) return null;
            var current = root.transform;
            for (int i = 1; i < names.Count; i++)
            {
                current = current.Find(names[i]);
                if (current == null) return null;
            }

            return current.gameObject;
        }

        private Rect GetNodeRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                var corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                var canvas = rt.GetComponentInParent<Canvas>();
                Camera cam = null;
                if (canvas != null)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace)
                        cam = canvas.worldCamera;
                }

                var p0 = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                var p2 = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                var minX = Mathf.Min(p0.x, p2.x);
                var minY = Mathf.Min(p0.y, p2.y);
                var w = Mathf.Abs(p2.x - p0.x);
                var h = Mathf.Abs(p2.y - p0.y);
                return new Rect(minX, minY, w, h);
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var b = renderer.bounds;
                var cam = Camera.main;
                if (cam != null)
                {
                    var min = cam.WorldToScreenPoint(b.min);
                    var max = cam.WorldToScreenPoint(b.max);
                    var minX = Mathf.Min(min.x, max.x);
                    var minY = Mathf.Min(min.y, max.y);
                    var w = Mathf.Abs(max.x - min.x);
                    var h = Mathf.Abs(max.y - min.y);
                    return new Rect(minX, minY, w, h);
                }
            }

            return new Rect();
        }
    }
}