using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChemLab.Game {
/// <summary>
/// An immutable GPU rendering snapshot. The liquid's eight-vertex box is only
/// a raymarch domain, not its visible surface. Keep the actual density field,
/// vessel profile and exact solid clipping data alive independently of its solver.
/// </summary>
public sealed class FluidVisualSnapshot : IDisposable {
    static readonly string[] BufferNames = { "_FluidProfile", "_FluidOccupiedBounds",
        "SolidBodies", "SolidTriangles", "SolidBodyNodes", "SolidTriangleNodes", "SolidTriangleRanges" };
    readonly List<ComputeBuffer> buffers = new List<ComputeBuffer>();
    RenderTexture density;
    public Material Material { get; private set; }
    public static int LiveCount { get; private set; }
    bool counted;

    internal FluidVisualSnapshot(Material source, params ComputeBuffer[] sources) {
        try {
            var shader = Resources.Load<Shader>("ChemLab/GhostFluid");
            var copy = Resources.Load<ComputeShader>("FluidSnapshotCopy");
            var sourceDensity = source.GetTexture("_FluidDensity") as RenderTexture;
            if (shader == null || copy == null || sourceDensity == null || sources.Length != BufferNames.Length)
                throw new InvalidOperationException("Cannot capture the GPU liquid surface for a david afterimage");
            Material = new Material(source) { shader = shader, name = "David frozen liquid surface" };
            // These uniforms are deliberately outside ShaderLab Properties in
            // FluidVolume; changing shaders does not preserve them automatically.
            foreach (var name in new[]{"_FluidMin","_FluidSize","_FluidTexel"})
                Material.SetVector(name,source.GetVector(name));
            Material.SetFloat("_FluidHeight",source.GetFloat("_FluidHeight"));
            Material.SetInt("SolidCount",source.GetInt("SolidCount"));
            Material.SetInt("SolidBodyNodeCount",source.GetInt("SolidBodyNodeCount"));
            Material.renderQueue = 3008;
            Material.SetFloat("_Glow", 1f); Material.SetFloat("_Saturation", 1.25f);
            Material.SetColor("_OutlineColor", new Color(.07f,.08f,.10f,.7f));
            Material.SetFloat("_OutlineStart", .55f); Material.SetFloat("_OutlineFalloff", .3f);
            Material.SetFloat("_GhostFade", 1f);
            Material.SetFloat("_AuditSurface", 0f);
            density = new RenderTexture(sourceDensity.descriptor) {
                name = "David frozen liquid density", filterMode = sourceDensity.filterMode,
                wrapMode = sourceDensity.wrapMode
            };
            density.Create();
            Graphics.CopyTexture(sourceDensity, density);
            Material.SetTexture("_FluidDensity", density);
            for (int i = 0; i < sources.Length; i++) {
                var src = sources[i];
                if (src == null) throw new InvalidOperationException("Missing liquid snapshot buffer " + BufferNames[i]);
                var dst = new ComputeBuffer(src.count, src.stride);
                buffers.Add(dst);
                int kernel = copy.FindKernel("Copy" + src.stride);
                copy.SetInt("Count", src.count);
                copy.SetBuffer(kernel, "Source", src);
                copy.SetBuffer(kernel, "Destination", dst);
                copy.Dispatch(kernel, (src.count + 63) / 64, 1, 1);
                Material.SetBuffer(BufferNames[i], dst);
            }
            counted = true; LiveCount++;
        } catch { Dispose(); throw; }
    }

    public void Dispose() {
        foreach (var buffer in buffers) buffer.Release();
        buffers.Clear();
        if (density != null) { density.Release(); UnityEngine.Object.Destroy(density); density = null; }
        if (Material != null) { UnityEngine.Object.Destroy(Material); Material = null; }
        if (counted) { counted = false; LiveCount--; }
    }
}
}
