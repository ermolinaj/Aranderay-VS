using System.Collections.Generic;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Gui
{
    internal sealed class BattleCasualties
    {
        private const float StainHold = 0.72f;
        private const float StainDuration = 1.65f;
        private static readonly Color32 Blood = new Color32(65, 16, 42, 236);
        private static readonly int Tint = Shader.PropertyToID("_Color");
        private static readonly Vector2[] BlotEdge =
        {
            new Vector2(-0.53f, 0.01f), new Vector2(-0.42f, 0.25f), new Vector2(-0.19f, 0.22f),
            new Vector2(0.02f, 0.36f), new Vector2(0.18f, 0.24f), new Vector2(0.47f, 0.20f),
            new Vector2(0.43f, -0.05f), new Vector2(0.22f, -0.13f), new Vector2(0.15f, -0.27f),
            new Vector2(-0.14f, -0.25f), new Vector2(-0.36f, -0.15f)
        };

        private sealed class Casualty
        {
            internal bool Human;
            internal float ContactAt;
            internal Vector3 StainPosition;
            internal float StainSize;
            internal int FloorLayer;
            internal MeshRenderer Stain;
        }

        private readonly AudioSource audio;
        private readonly AudioClip[] humanVoices = Resources.LoadAll<AudioClip>("CombatAudio/Human");
        private readonly AudioClip[] creatureVoices = Resources.LoadAll<AudioClip>("CombatAudio/Creature");
        private readonly System.Random variation = new System.Random();
        private readonly List<Casualty> effects = new List<Casualty>();
        private readonly Material stainMaterial;
        private readonly Mesh stainMesh;
        private readonly MaterialPropertyBlock stainTint = new MaterialPropertyBlock();
        private int lastHuman = -1;
        private int lastCreature = -1;

        internal BattleCasualties(GameObject owner, Shader shader)
        {
            audio = owner.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
            if (shader != null && shader.isSupported)
            {
                stainMaterial = new Material(shader) { renderQueue = 2994 };
                stainMesh = BuildStainMesh();
            }
        }

        internal void Queue(Unit defender, float contactAt, int visibleLayers)
        {
            // The defender releases its tile and is destroyed immediately after the hit event.
            var effect = new Casualty { Human = defender.PlayerNumber == 0, ContactAt = contactAt };
            var cell = defender.Cell;
            if (effect.Human && stainMaterial != null && BattleFloor.TryGetFloor(cell, visibleLayers, out var floor))
            {
                effect.StainPosition = new Vector3(floor.center.x, floor.max.y + 0.015f, floor.center.z);
                effect.StainSize = Mathf.Min(floor.size.x, floor.size.z) * 0.48f;
                effect.FloorLayer = cell.gameObject.layer;
            }
            effects.Add(effect);
        }

        internal void Refresh()
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i];
                float age = Time.time - effect.ContactAt;
                if (age < 0f)
                    continue;

                if (effect.Stain == null)
                {
                    if (effect.Human)
                        PlayVoice(humanVoices, ref lastHuman, 0.64f);
                    else
                        PlayVoice(creatureVoices, ref lastCreature, 0.81f);

                    if (effect.StainSize <= 0f || age >= StainDuration)
                    {
                        effects.RemoveAt(i);
                        continue;
                    }
                    effect.Stain = CreateStain(effect);
                }

                if (age >= StainDuration)
                {
                    Object.Destroy(effect.Stain.gameObject);
                    effects.RemoveAt(i);
                    continue;
                }

                Color color = Blood;
                color.a *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(StainHold, StainDuration, age));
                stainTint.SetColor(Tint, color);
                effect.Stain.SetPropertyBlock(stainTint);
            }
        }

        private void PlayVoice(AudioClip[] clips, ref int previous, float volume)
        {
            if (clips.Length == 0)
                return;

            int next = clips.Length > 1 && previous >= 0
                ? (previous + variation.Next(1, clips.Length)) % clips.Length
                : variation.Next(clips.Length);
            previous = next;
            audio.PlayOneShot(clips[next], volume);
        }

        private MeshRenderer CreateStain(Casualty effect)
        {
            var stain = new GameObject("Human casualty stain", typeof(MeshFilter), typeof(MeshRenderer));
            stain.layer = effect.FloorLayer;
            var rotation = Quaternion.Euler(0f, variation.Next(360), 0f);
            stain.transform.SetPositionAndRotation(effect.StainPosition, rotation);
            stain.transform.localScale = Vector3.one * effect.StainSize;
            stain.GetComponent<MeshFilter>().sharedMesh = stainMesh;
            var renderer = stain.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = stainMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Mesh BuildStainMesh()
        {
            var vertices = new List<Vector3>();
            var colors = new List<Color32>();
            var triangles = new List<int>();
            AddBlot(Vector2.zero, 1f);
            AddBlot(new Vector2(0.57f, 0.22f), 0.19f);
            AddBlot(new Vector2(-0.35f, -0.31f), 0.15f);
            var mesh = new Mesh { name = "Casualty stain" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            return mesh;

            void AddBlot(Vector2 center, float scale)
            {
                int first = vertices.Count;
                vertices.Add(new Vector3(center.x, 0f, center.y));
                colors.Add(new Color32(255, 255, 255, 255));
                for (int i = 0; i < BlotEdge.Length; i++)
                {
                    var edge = center + BlotEdge[i] * scale;
                    vertices.Add(new Vector3(edge.x, 0f, edge.y));
                    colors.Add(new Color32(255, 255, 255, 210));
                    triangles.Add(first);
                    triangles.Add(first + i + 1);
                    triangles.Add(first + (i + 1) % BlotEdge.Length + 1);
                }
            }
        }

        internal void Dispose()
        {
            foreach (var effect in effects)
            {
                if (effect.Stain != null)
                    Object.Destroy(effect.Stain.gameObject);
            }
            Object.Destroy(stainMesh);
            Object.Destroy(stainMaterial);
        }
    }
}
