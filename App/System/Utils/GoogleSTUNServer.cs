using System.Net;
using System.Net.Sockets;

namespace App.System.Utils;

public class GoogleSTUNServer
{
    public static async Task<IPEndPoint> GetPublicIPAddress(string stunServer = "stun.l.google.com", int stunPort = 19302)
    {
        UdpClient udpClient = null;
        IPEndPoint result = null;
        
        try
        {
            udpClient = new UdpClient(0);
            udpClient.Client.ReceiveTimeout = 5000;

            IPHostEntry hostEntry = await Dns.GetHostEntryAsync(stunServer);
            IPAddress stunServerIp = hostEntry.AddressList[0];
            IPEndPoint stunEndPoint = new IPEndPoint(stunServerIp, stunPort);

            byte[] transactionId = new byte[12];
            new Random().NextBytes(transactionId);

            byte[] requestPacket = CreateStunBindingRequest(transactionId);

            await udpClient.SendAsync(requestPacket, requestPacket.Length, stunEndPoint);
            
            var receiveTask = udpClient.ReceiveAsync();
            var completed = await Task.WhenAny(receiveTask, Task.Delay(udpClient.Client.ReceiveTimeout));
            if (completed != receiveTask)
                throw new SocketException((int)SocketError.TimedOut);
            UdpReceiveResult serverResponse = receiveTask.Result;
            byte[] responsePacket = serverResponse.Buffer;
            
            result = ParseStunResponse(responsePacket, transactionId);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
        {
            Console.WriteLine("[STUN] Socket timeout: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[STUN] Error: " + ex.Message);
        }
        finally
        {
            udpClient?.Close();
        }

        return result;
    }
    
    public static async Task<IPEndPoint> GetPublicIPAddress(int localPort, string stunServer = "stun.l.google.com", int stunPort = 19302)
    {
        UdpClient udpClient = null;
        IPEndPoint result = null;
        
        try
        {
            udpClient = new UdpClient(localPort);
            udpClient.Client.ReceiveTimeout = 5000;

            IPHostEntry hostEntry = await Dns.GetHostEntryAsync(stunServer);
            IPAddress stunServerIp = hostEntry.AddressList[0];
            IPEndPoint stunEndPoint = new IPEndPoint(stunServerIp, stunPort);

            byte[] transactionId = new byte[12];
            new Random().NextBytes(transactionId);

            byte[] requestPacket = CreateStunBindingRequest(transactionId);

            await udpClient.SendAsync(requestPacket, requestPacket.Length, stunEndPoint);
            var receiveTask = udpClient.ReceiveAsync();
            var completed = await Task.WhenAny(receiveTask, Task.Delay(udpClient.Client.ReceiveTimeout));
            if (completed != receiveTask)
                throw new SocketException((int)SocketError.TimedOut);
            UdpReceiveResult serverResponse = receiveTask.Result;
            byte[] responsePacket = serverResponse.Buffer;
            
            result = ParseStunResponse(responsePacket, transactionId);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
        {
            Console.WriteLine("[STUN] Socket timeout: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[STUN] Error: " + ex.Message);
        }
        finally
        {
            udpClient?.Close();
        }

        return result;
    }
    
    private static byte[] CreateStunBindingRequest(byte[] transactionId)
    {
        byte[] packet = new byte[20];

        packet[0] = 0x00;
        packet[1] = 0x01;

        packet[2] = 0x00;
        packet[3] = 0x00;

        packet[4] = 0x21;
        packet[5] = 0x12;
        packet[6] = 0xA4;
        packet[7] = 0x42;

        Buffer.BlockCopy(transactionId, 0, packet, 8, 12);

        return packet;
    }

    private static IPEndPoint ParseStunResponse(byte[] response, byte[] expectedTransactionId)
    {
        if (response.Length < 20)
            throw new ArgumentException("Слишком короткий ответ для STUN-пакета.");

        if (response[4] != 0x21 || response[5] != 0x12 || response[6] != 0xA4 || response[7] != 0x42)
            throw new ArgumentException("Некорректный Magic Cookie в ответе.");

        for (int i = 0; i < 12; i++)
        {
            if (response[8 + i] != expectedTransactionId[i])
                throw new ArgumentException("Transaction ID в ответе не совпадает с запросом.");
        }

        int index = 20; 

        while (index + 4 <= response.Length)
        {
            ushort attributeType = (ushort)((response[index] << 8) | response[index + 1]);
            ushort attributeLength = (ushort)((response[index + 2] << 8) | response[index + 3]);

            if (index + 4 + attributeLength > response.Length)
                break;

            if (attributeType == 0x0020)
            {
                int addrIndex = index + 4;

                byte family = response[addrIndex + 1];

                if (family == 0x01)
                {
                    ushort xPort = (ushort)((response[addrIndex + 2] << 8) | response[addrIndex + 3]);
                    ushort port = (ushort)(xPort ^ 0x2112);

                    uint xIp = (uint)((response[addrIndex + 4] << 24) | (response[addrIndex + 5] << 16) | (response[addrIndex + 6] << 8) | response[addrIndex + 7]);
                    uint ip = xIp ^ 0x2112A442;

                    byte[] ipBytes = BitConverter.GetBytes(ip);
                    
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(ipBytes);
                    
                    IPAddress publicIp = new IPAddress(ipBytes);

                    return new IPEndPoint(publicIp, port);
                }
            }

            index += 4 + attributeLength;
        }

        throw new ArgumentException("В ответе не найден атрибут XOR-MAPPED-ADDRESS.");
    }
}