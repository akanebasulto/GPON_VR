using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ChatGPTManager : MonoBehaviour
{
    [TextArea]
    public string userPrompt;

    private string apiKey = "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";

    public void SendToChatGPT()
    {
        StartCoroutine(SendRequest(userPrompt));
    }

    IEnumerator SendRequest(string prompt)
    {
        string url = "https://api.openai.com/v1/chat/completions";

        string jsonBody = "{"
            + "\"model\": \"gpt-4o-mini\","
            + "\"messages\": [{\"role\": \"user\", \"content\": \"" + prompt + "\"}]"
            + "}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Respuesta: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }
}
