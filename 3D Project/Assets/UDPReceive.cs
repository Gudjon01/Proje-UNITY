using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;

public class UDPReceive : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client;
    public int port = 5052;
    public bool startRecieving = true;
    public bool printToConsole = false;
    public string data;
    public GameObject landmarkPrefab;
    public Material lineMaterial; // Material for the lines
    public float lineWidth = 0.02f; // Public variable to control the line width

    private ConcurrentQueue<Dictionary<string, LandmarkData>> dataQueue = new ConcurrentQueue<Dictionary<string, LandmarkData>>();
    private GameObject[] landmarks = new GameObject[33]; // Array to store the prefab instances
    private List<LineRenderer> lines = new List<LineRenderer>(); // List to store the LineRenderers

    private readonly int[,] connections = new int[,] // Define the connections between the landmarks
    {
        {4,5},{5,6},{1,2},{2,3},
        {5,0},{2,0},
        {5,8},{2,7},
        {10,9},
        {18,16}, {20,16}, {22,16}, {16,14}, {14,12}, {12,11}, {13,11}, {13,15},
        {15,17}, {15,19}, {15,21},
        {12,24}, {11,23}, {23,24},
        {24,26}, {23,25},
        {26,28}, {25,27},
        {28,30}, {27,29},
        {30,32}, {29,31},
    };

    private void Start()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        // Instantiate the prefabs at the start
        for (int i = 0; i < landmarks.Length; i++)
        {
            landmarks[i] = Instantiate(landmarkPrefab, Vector3.zero, Quaternion.identity);
            landmarks[i].name = GetLandmarkName(i); // Assign a name to each prefab
        }

        // Create LineRenderers for the connections
        for (int i = 0; i < connections.GetLength(0); i++)
        {
            GameObject lineObj = new GameObject("Line");
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.positionCount = 2;
            lr.startWidth = lineWidth; // Set the line width
            lr.endWidth = lineWidth; // Set the line width
            lines.Add(lr);
        }
    }

    // Receive thread
    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (startRecieving)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] dataByte = client.Receive(ref anyIP);
                data = Encoding.UTF8.GetString(dataByte);

                if (printToConsole) { print(data); }

                // Deserialize the JSON data
                Dictionary<string, LandmarkData> landmarkData = JsonConvert.DeserializeObject<Dictionary<string, LandmarkData>>(data);
                
                // Enqueue the data for processing on the main thread
                dataQueue.Enqueue(landmarkData);
            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

    private void Update()
    {
        // Process the queued data on the main thread
        while (dataQueue.TryDequeue(out Dictionary<string, LandmarkData> landmarkData))
        {
            UpdateLandmarks(landmarkData);
        }

        // Update line widths if changed
        foreach (var line in lines)
        {
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
        }
    }

    private void UpdateLandmarks(Dictionary<string, LandmarkData> landmarkData)
    {
        foreach (var key in landmarkData.Keys)
        {
            int index = int.Parse(key);
            if (index >= 0 && index < landmarks.Length)
            {
                LandmarkData ld = landmarkData[key];
                Vector3 position = new Vector3(ld.pixel_x, -ld.pixel_y, ld.landmark_z); // Invert the y position
                landmarks[index].transform.position = position;
            }
        }

        // Update the lines to connect the prefabs
        for (int i = 0; i < connections.GetLength(0); i++)
        {
            int start = connections[i, 0];
            int end = connections[i, 1];
            lines[i].SetPosition(0, landmarks[start].transform.position);
            lines[i].SetPosition(1, landmarks[end].transform.position);
        }
    }

    private string GetLandmarkName(int index)
    {
        string[] names = {
            "nose", "left eye (inner)", "left eye", "left eye (outer)", "right eye (inner)",
            "right eye", "right eye (outer)", "left ear", "right ear", "mouth (left)",
            "mouth (right)", "left shoulder", "right shoulder", "left elbow", "right elbow",
            "left wrist", "right wrist", "left pinky", "right pinky", "left index",
            "right index", "left thumb", "right thumb", "left hip", "right hip",
            "left knee", "right knee", "left ankle", "right ankle", "left heel",
            "right heel", "left foot index", "right foot index"
        };

        return index < names.Length ? names[index] : "Unknown";
    }

    [Serializable]
    public class LandmarkData
    {
        public float pixel_x;
        public float pixel_y;
        public float landmark_z;
    }
}
