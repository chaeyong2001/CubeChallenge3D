using UnityEngine;

namespace CubeChallenge3D.Cube.View
{
    public static class RuntimePrimitiveFactory
    {
        private static Mesh cubeMesh;
        private static Mesh quadMesh;

        public static GameObject CreateCube(string name)
        {
            return Create(name, GetCubeMesh());
        }

        public static GameObject CreateQuad(string name)
        {
            return Create(name, GetQuadMesh());
        }

        private static GameObject Create(string name, Mesh mesh)
        {
            GameObject result = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            result.GetComponent<MeshFilter>().sharedMesh = mesh;
            return result;
        }

        private static Mesh GetCubeMesh()
        {
            if (cubeMesh != null)
            {
                return cubeMesh;
            }

            cubeMesh = new Mesh { name = "RuntimeCubeMesh", hideFlags = HideFlags.DontSave };
            cubeMesh.vertices = new[]
            {
                // Back
                new Vector3(0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f),
                // Front
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
                // Left
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                // Right
                new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f),
                // Bottom
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f),
                // Top
                new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f)
            };
            cubeMesh.uv = new[]
            {
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up
            };
            cubeMesh.triangles = new[]
            {
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7,
                8, 9, 10, 8, 10, 11,
                12, 13, 14, 12, 14, 15,
                16, 17, 18, 16, 18, 19,
                20, 21, 22, 20, 22, 23
            };
            cubeMesh.RecalculateNormals();
            cubeMesh.RecalculateTangents();
            cubeMesh.RecalculateBounds();
            return cubeMesh;
        }

        private static Mesh GetQuadMesh()
        {
            if (quadMesh != null)
            {
                return quadMesh;
            }

            quadMesh = new Mesh { name = "RuntimeQuadMesh", hideFlags = HideFlags.DontSave };
            quadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            quadMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            // Match Unity's built-in Quad: its visible normal points along -Z.
            // CubeFaceletMapping.StickerRotation is defined against this basis.
            quadMesh.triangles = new[] { 2, 1, 0, 3, 2, 0 };
            quadMesh.RecalculateNormals();
            quadMesh.RecalculateBounds();
            return quadMesh;
        }
    }
}
