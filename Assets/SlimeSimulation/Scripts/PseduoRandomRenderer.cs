using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PseduoRandomRenderer : MonoBehaviour
{
    public ComputeShader updateSlimeShader;
    public ComputeShader processTrailsShader;

    public Material agentMaterial;
    public Material trailMaterial;

    public Vector2 dims = new Vector2(512,512);
    public int numAgents = 200;
    public float moveSpeed = 40.0f;
    public float diffusionSpeed = 1.0f;
    public float evaporationSpeed = 1.0f;

    public float sensorAngle = 1.0f;
    public float sensorDistance = 1.0f;
    public int sensorSize = 1;

    private RenderTexture trailTexture;
    private RenderTexture processedTrailTexture;
    private ComputeBuffer agentBuffer;

    int UpdateKernelID;
    int ProcessTrailMapKernelID;

    void Start()
    {
        //Create RTs
        CreateRenderTexture(ref trailTexture, (int)dims.x, (int)dims.y);
        CreateRenderTexture(ref processedTrailTexture, (int)dims.x, (int)dims.x);

        //Grab Kernel IDs
        UpdateKernelID = updateSlimeShader.FindKernel("Update");
        ProcessTrailMapKernelID = processTrailsShader.FindKernel("ProcessSlimeTrails");

        updateSlimeShader.SetTexture(UpdateKernelID, "TrailMap", trailTexture);
        updateSlimeShader.SetTexture(UpdateKernelID, "ProcessedTrailMap", processedTrailTexture);

        processTrailsShader.SetTexture(ProcessTrailMapKernelID, "TrailMap", processedTrailTexture);
        processTrailsShader.SetTexture(ProcessTrailMapKernelID, "ProcessedTrailMap", trailTexture);

        GenerateAgents();
        updateSlimeShader.SetBuffer(UpdateKernelID, "agents", agentBuffer);

        updateSlimeShader.SetInt("width", (int)dims.x);
        updateSlimeShader.SetInt("height", (int)dims.y);
        updateSlimeShader.SetFloat("deltaTime", 0.02f);

        updateSlimeShader.SetInt("numAgents", numAgents);
        updateSlimeShader.SetFloat("moveSpeed", moveSpeed);
        updateSlimeShader.SetFloat("sensorAngleSpacing ", sensorAngle);
        updateSlimeShader.SetFloat("sensorDistance", sensorDistance);
        updateSlimeShader.SetInt("sensorSize", sensorSize);

        processTrailsShader.SetInt("width", (int)dims.x);
        processTrailsShader.SetInt("height", (int)dims.y);
        processTrailsShader.SetFloat("deltaTime", 0.02f);
        processTrailsShader.SetFloat("evaporationSpeed", evaporationSpeed);
        processTrailsShader.SetFloat("diffusionSpeed", diffusionSpeed);

        agentMaterial.mainTexture = trailTexture;
        trailMaterial.mainTexture = processedTrailTexture;
    }

    private void Update()
    {
        trailTexture.DiscardContents();
        updateSlimeShader.SetFloat("deltaTime", Time.deltaTime);
        processTrailsShader.SetFloat("deltaTime", Time.deltaTime);

        updateSlimeShader.Dispatch(UpdateKernelID, ((int)dims.x * (int)dims.y) / 16, 1, 1);
        processTrailsShader.Dispatch(ProcessTrailMapKernelID, (int)dims.x / 8, (int)dims.x / 8, 1);
    }

    void GenerateAgents()
    {
        List<Agent> agents = new List<Agent>();
        for (int n = 0; n < numAgents; n++)
        {
            Agent agent = new Agent();
            agent.position = new Vector2(//(int)dims.x / 2, (int)dims.y / 2);
                Random.Range(0, (int)dims.x),
                Random.Range(0, (int)dims.y));
            agent.angle = Random.Range(0, 360);
            agents.Add(agent);
        }

        agentBuffer = new ComputeBuffer(agents.Count, (sizeof(float) * 2) + sizeof(float), ComputeBufferType.Default);
        agentBuffer.SetData<Agent>(agents);
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