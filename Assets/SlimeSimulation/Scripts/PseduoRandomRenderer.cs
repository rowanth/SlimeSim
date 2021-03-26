using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PseduoRandomRenderer : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material textureMaterial;

    private RenderTexture trailTexture;
    private RenderTexture processedTrailTexture;
    private ComputeBuffer agentBuffer;
    private int numAgents = 500;
    int UpdateKernelID;
    int ProcessTrailMapKernelID;

    void Start()
    {
        CreateRenderTexture(ref trailTexture, 512, 512);
        CreateRenderTexture(ref processedTrailTexture, 512, 512);

        UpdateKernelID = computeShader.FindKernel("Update");
        ProcessTrailMapKernelID = computeShader.FindKernel("ProcessTrailMap");

        computeShader.SetTexture(UpdateKernelID, "TrailMap", trailTexture);
        computeShader.SetTexture(ProcessTrailMapKernelID, "TrailMap", trailTexture);
        computeShader.SetTexture(ProcessTrailMapKernelID, "ProcessedTrailMap", trailTexture);
        computeShader.SetInt("width", trailTexture.width);
        computeShader.SetInt("height", trailTexture.height);

        computeShader.SetInt("numAgents", numAgents);
        computeShader.SetFloat("moveSpeed", 10.0f);
        computeShader.SetFloat("deltaTime", 0.02f);
        computeShader.SetFloat("evaporateSpeed", 1.0f);

        List<Agent> agents = new List<Agent>();
        for (int n = 0; n < numAgents; n++)
        {
            Agent agent = new Agent();
            agent.position = new Vector2(trailTexture.width/2, trailTexture.height/2);
                //Random.Range(0, renderTexture.width),
                //Random.Range(0, renderTexture.height));
            agent.angle = Random.Range(0, 360);
            agents.Add(agent);
        }

        agentBuffer = new ComputeBuffer(agents.Count, (sizeof(float) * 2) + sizeof(float), ComputeBufferType.Default);
        agentBuffer.SetData<Agent>(agents);
        computeShader.SetBuffer(UpdateKernelID, "agents", agentBuffer);
        computeShader.SetBuffer(ProcessTrailMapKernelID, "agents", agentBuffer);
    }

    private void Update()
    {
        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.Dispatch(UpdateKernelID, (trailTexture.width * trailTexture.height) / 16, 1, 1);

        textureMaterial.mainTexture = trailTexture;

        int groups = Mathf.CeilToInt(processedTrailTexture.width / 8f);
        computeShader.Dispatch(ProcessTrailMapKernelID, groups, groups, 1);
    }

    void CreateRenderTexture(ref RenderTexture rt, int w, int h)
    {
        rt = new RenderTexture(w, h, 24);
        rt.enableRandomWrite = true;
        rt.Create();
    }
    void OnDestroy()
    {
        agentBuffer.Dispose();
    }
}
