using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Text;

class Program
{
    public static void Serve(Socket clientSocket)
    {
        byte[] buffer = new byte[256];
        List<byte> request = new List<byte>();
        int position = 0;

        while (true)
        {
            int readed = clientSocket.Receive(buffer);
            if (readed == 0)
                break;
            request.AddRange(buffer.Take(readed));
            bool found = false;
            while (position + 3 < request.Count)
            {
                if (request[position] == '\r'
                && request[position + 1] == '\n'
                && request[position + 2] == '\r'
                && request[position + 3] == '\n')
                {
                    found = true;
                    break;
                }
                position += 1;
            }
            if (found)
                break;
        }
        string requestStr = Encoding.ASCII.GetString(request.Take(position).ToArray());
        Console.WriteLine(clientSocket.RemoteEndPoint);
        Console.WriteLine(requestStr);
        int firstLineEnd = requestStr.IndexOf("\r\n");
        if (firstLineEnd < 0)
            firstLineEnd = requestStr.Length;
        string firstLine = requestStr.Substring(0, firstLineEnd);
        string[] arr = firstLine.Split();
        string method = arr[0];
        string path = arr[1];
        string ver = arr[2];
        Console.WriteLine("Method: {0}", method);
        Console.WriteLine("Path: \"{0}\"", path);
        Console.WriteLine("Version: \"{0}\"", ver);
        String contentType = "text/html";
        byte[]? responseContentBytes = null;
        Stream? f = null;
        try
        {
            if (path.Length > 2)
            	f = File.Open(path.Substring(1, path.Length - 1), FileMode.Open);
        }
        catch (System.IO.FileNotFoundException)
        {
            f = null;
        }
        if (f != null)
        {
            List<byte> data = new List<byte>();
            using (BinaryReader br = new BinaryReader(f))
            {
                var buffer1 = new byte[256];

                while (true)
                {
                    int readed = br.Read(buffer1);
                    if (readed == 0)
                        break;
                    data.AddRange(buffer1.Take(readed));
                }

            }
            responseContentBytes = data.ToArray();
            contentType = "image/png";
        }
        else
        {
            String responseContent = $"<!DOCTYPE html>\n<html lang=\"en\"><head><title>Test</title></head><body>Hello World<br>{path}<img src=\"qwe.png\"></body></html>";
            responseContentBytes = Encoding.UTF8.GetBytes(responseContent);
        }
        StringBuilder serverResponse = new StringBuilder(
              "HTTP/1.0 200 OK\r\n"
            + $"Content-Type: {contentType}\r\n"
            + "Connection: close\r\n");
        
        serverResponse.AppendFormat("Content-Length: {0}\r\n", responseContentBytes.Length);
        serverResponse.Append("\r\n");
        
        Console.WriteLine("============ Response Headers ============");
        Console.WriteLine(serverResponse);
        Console.WriteLine("========================");
        clientSocket.Send(Encoding.ASCII.GetBytes(serverResponse.ToString()), SocketFlags.None);
        for (int bytesSent = 0; bytesSent < responseContentBytes.Length;)
        {
            bytesSent += clientSocket.Send(responseContentBytes, bytesSent, responseContentBytes.Length - bytesSent, SocketFlags.None);
        }
        clientSocket.Close();
    }

    public static void Main(string[] args)
    {
        if(args.Length != 1)
        {
            Console.WriteLine("Usage: http port");
            return;
        }
        const int backlog = 10;
        int port = int.Parse(args[0]);

        Socket listenSocket = new Socket(AddressFamily.InterNetwork,
                                         SocketType.Stream,
                                         ProtocolType.Tcp);

        IPAddress hostIP = IPAddress.Any;
        IPEndPoint ep = new IPEndPoint(hostIP, port);

        // bind the listening socket to the port
        listenSocket.Bind(ep);
        Console.WriteLine("Bind done");

        // start listening
        listenSocket.Listen(backlog);
        while (true)
        {
            // accept new client
            Socket clientSocket = listenSocket.Accept();
            Serve(clientSocket);
        }
    }
}
