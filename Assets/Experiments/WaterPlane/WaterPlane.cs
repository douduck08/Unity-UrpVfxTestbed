using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

public class WaterPlane : ScriptableRendererFeature {

    CopyColorPass copyColorPass;
    WaterPlanePass waterPlanePass;

    public override void Create () {
        copyColorPass = new CopyColorPass (RenderPassEvent.AfterRenderingSkybox);
        waterPlanePass = new WaterPlanePass (RenderPassEvent.AfterRenderingSkybox + 1);
    }

    public override void AddRenderPasses (ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass (copyColorPass);
        renderer.EnqueuePass (waterPlanePass);
    }
}

public class CopyColorPass : ScriptableRenderPass {

    const string profilerTag = "Copy of Water Plane";
    ProfilingSampler profilingSampler = new ProfilingSampler (profilerTag);

    int destination = -1;

    public CopyColorPass (RenderPassEvent renderPassEvent) {
        this.renderPassEvent = renderPassEvent;
    }

    public override void Configure (CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescripor) {
        RenderTextureDescriptor descriptor = cameraTextureDescripor;
        descriptor.msaaSamples = 1;
        descriptor.depthBufferBits = 0;

        destination = Shader.PropertyToID ("_UnderWaterPlane");
        cmd.GetTemporaryRT (destination, descriptor, FilterMode.Bilinear);
    }

    public override void FrameCleanup (CommandBuffer cmd) {
        if (destination != -1) {
            cmd.ReleaseTemporaryRT (destination);
            destination = -1;
        }
    }

    public override void Execute (ScriptableRenderContext context, ref RenderingData renderingData) {
        // var source = BuiltinRenderTextureType.CurrentActive;
        var source = Shader.PropertyToID ("_CameraTargetTexture");

        var cmd = CommandBufferPool.Get (profilerTag);
        using (new ProfilingScope (cmd, profilingSampler)) {
            // Blit (cmd, source, destination);
            cmd.Blit (source, destination);

            cmd.SetGlobalTexture ("_UnderWaterPlaneColor", destination);
        }
        context.ExecuteCommandBuffer (cmd);
        CommandBufferPool.Release (cmd);
    }
}

public class WaterPlanePass : ScriptableRenderPass {

    const string profilerTag = "Render Water Plane";
    ProfilingSampler profilingSampler = new ProfilingSampler (profilerTag);

    FilteringSettings filteringSettings;
    List<ShaderTagId> shaderTagIdList = new List<ShaderTagId> ();

    public WaterPlanePass (RenderPassEvent renderPassEvent) {
        this.renderPassEvent = renderPassEvent;

        var renderQueueRange = RenderQueueRange.opaque;
        filteringSettings = new FilteringSettings (renderQueueRange);
        shaderTagIdList.Add (new ShaderTagId ("WaterPlane"));
    }



    public override void Execute (ScriptableRenderContext context, ref RenderingData renderingData) {
        var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
        var drawingSettings = CreateDrawingSettings (shaderTagIdList, ref renderingData, sortingCriteria);

        var cmd = CommandBufferPool.Get (profilerTag);
        using (new ProfilingScope (cmd, profilingSampler)) {
            context.ExecuteCommandBuffer (cmd);
            cmd.Clear ();

            context.DrawRenderers (renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }
        context.ExecuteCommandBuffer (cmd);
        CommandBufferPool.Release (cmd);
    }
}