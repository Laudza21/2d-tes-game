using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using MCPForUnity.Editor.Services.Transport.Transports;

namespace MCPTest.Editor
{
    [InitializeOnLoad]
    public static class MCPTest
    {
        static MCPTest()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("MCPTestRan", false))
                {
                    SessionState.SetBool("MCPTestRan", true);
                    VerifyBridge();
                }
            };
        }

        [MenuItem("Tests/MCP/Verify Bridge")]
        public static void VerifyBridge()
        {
            string status = "UNKNOWN";
            try
            {
                if (!StdioBridgeHost.IsRunning)
                {
                    status = "FAIL: StdioBridgeHost is not running. Please open 'Window > MCP for Unity' and ensure the bridge is started.";
                    Debug.LogError(status);
                    WriteResult(status);
                    return;
                }

                int port = StdioBridgeHost.GetCurrentPort();
                Debug.Log($"[MCPTest] Connecting to port {port}...");

                using (var client = new TcpClient("127.0.0.1", port))
                using (var stream = client.GetStream())
                {
                    client.ReceiveTimeout = 2000;
                    client.SendTimeout = 2000;

                    // Read Welcome
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string welcome = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log($"[MCPTest] Received handshake: {welcome}");

                    if (!welcome.Contains("WELCOME UNITY-MCP"))
                    {
                        status = "FAIL: Invalid handshake: " + welcome;
                        Debug.LogError(status);
                        WriteResult(status);
                        return;
                    }

                    // Send Ping
                    // Protocol: 8 bytes BigEndian length + payload
                    string payload = "ping";
                    byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
                    byte[] header = new byte[8];
                    ulong len = (ulong)payloadBytes.Length;
                    
                    header[0] = (byte)(len >> 56);
                    header[1] = (byte)(len >> 48);
                    header[2] = (byte)(len >> 40);
                    header[3] = (byte)(len >> 32);
                    header[4] = (byte)(len >> 24);
                    header[5] = (byte)(len >> 16);
                    header[6] = (byte)(len >> 8);
                    header[7] = (byte)(len);

                    stream.Write(header, 0, 8);
                    stream.Write(payloadBytes, 0, payloadBytes.Length);
                    Debug.Log("[MCPTest] Sent 'ping' command.");

                    // Read Response Header
                    int totalHeaderRead = 0;
                    while (totalHeaderRead < 8)
                    {
                        int r = stream.Read(header, totalHeaderRead, 8 - totalHeaderRead);
                        if (r == 0) throw new IOException("Connection closed during header read");
                        totalHeaderRead += r;
                    }

                    ulong respLen = 0;
                    respLen |= ((ulong)header[0] << 56);
                    respLen |= ((ulong)header[1] << 48);
                    respLen |= ((ulong)header[2] << 40);
                    respLen |= ((ulong)header[3] << 32);
                    respLen |= ((ulong)header[4] << 24);
                    respLen |= ((ulong)header[5] << 16);
                    respLen |= ((ulong)header[6] << 8);
                    respLen |= ((ulong)header[7]);

                    Debug.Log($"[MCPTest] Reading response payload of size {respLen}...");
                    
                    byte[] respBuffer = new byte[respLen];
                    int totalRead = 0;
                    while (totalRead < (int)respLen)
                    {
                        int r = stream.Read(respBuffer, totalRead, (int)respLen - totalRead);
                        if (r == 0) throw new IOException("Connection closed during payload read");
                        totalRead += r;
                    }

                    string response = Encoding.UTF8.GetString(respBuffer);
                    Debug.Log($"[MCPTest] Response: {response}");

                    if (response.Contains("pong"))
                    {
                        status = "SUCCESS: Pong received.";
                        Debug.Log(status);
                    }
                    else
                    {
                        status = $"FAIL: Unexpected response: {response}";
                        Debug.LogError(status);
                    }
                }
            }
            catch (Exception ex)
            {
                status = $"FAIL: Exception: {ex.Message}";
                Debug.LogError(status);
            }
            WriteResult(status);
        }

        static void WriteResult(string content)
        {
            try
            {
                string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "mcp_test_result.txt");
                File.WriteAllText(path, content);
                Debug.Log($"[MCPTest] Result written to {path}");
            }
            catch(Exception e)
            {
                Debug.LogError($"[MCPTest] Failed to write result file: {e.Message}");
            }
        }
    }
}
