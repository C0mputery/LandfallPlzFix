using Landfall.Network;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

namespace ComputeryLib.Utilities;

public static class PlayerInteractionUtilities {
    public static void SendChatThrowable(string message, TABGPlayerServer sender, ServerClient world) {
        string[] chunks = SplitMessageIntoChunks(message, 255);
        Vector3 centerPosition = sender.PlayerPosition + new Vector3(sender.PlayerRotation.x, 0, sender.PlayerRotation.y) * 1f;

    }
    
    private static string[] SplitMessageIntoChunks(string message, int maxBytes) {
        if (string.IsNullOrEmpty(message)) { return [""]; }

        List<string> chunks = [];
        int currentIndex = 0;

        while (currentIndex < message.Length) {
            int chunkLength = 0;
            int byteCount = 0;

            while (currentIndex + chunkLength < message.Length) {
                int charBytes = Encoding.Unicode.GetByteCount(message[currentIndex + chunkLength].ToString());
                if (byteCount + charBytes <= maxBytes) {
                    byteCount += charBytes;
                    chunkLength++;
                } else {
                    break;
                }
            }

            if (currentIndex + chunkLength >= message.Length) {
                chunks.Add(message.Substring(currentIndex));
                break;
            }

            int lastSpaceIndex = message.LastIndexOf(' ', currentIndex + chunkLength - 1, chunkLength);

            if (lastSpaceIndex > currentIndex) {
                chunks.Add(message.Substring(currentIndex, lastSpaceIndex - currentIndex));
                currentIndex = lastSpaceIndex + 1;
            } else {
                if (chunkLength == 0) { chunkLength = 1; }
                chunks.Add(message.Substring(currentIndex, chunkLength));
                currentIndex += chunkLength;
            }
        }

        return chunks.ToArray();
    }
}