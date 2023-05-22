using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Text;

class HttpResponse
{
    public string ContentType = null!;
    public byte[] ContentBytes = new byte[0];
}

class Person
{
    public string FirstName = "";
    public string LastName = "";
    public int BirthYear;
}

class Program
{
    List<Person> people = new List<Person>(new Person[] {
        new Person {FirstName = "John", LastName = "Wick", BirthYear = 1980},
        new Person {FirstName = "Sarah", LastName = "Smith", BirthYear = 1985},
    });

    private void appendHeader(StringBuilder content, string title)
    {
        content.Append($"<!DOCTYPE html><html lang=\"en\"><head><title>{title}</title></head><body>");
    }

    private void appendFooter(StringBuilder content)
    {
        content.Append("</body></html>");
    }

    private HttpResponse Handle(string method, string pathAndParams, byte[]? requestContent)
    {
        HttpResponse response = new HttpResponse();
        int questionMark = pathAndParams.IndexOf('?');
        if (questionMark < 0)
            questionMark = pathAndParams.Length;
        string path = pathAndParams.Substring(0, questionMark);
        if (path == "/" || path == "/index")
        {
            response.ContentType = "text/html; charset=utf-8";

            StringBuilder responseContent = new StringBuilder();
            appendHeader(responseContent, "Index");
            responseContent.Append("Hello World");
            appendFooter(responseContent);
            response.ContentBytes = Encoding.UTF8.GetBytes(responseContent.ToString());
        }
        if (path == "/list")
        {
            response.ContentType = "text/html; charset=utf-8";
            StringBuilder responseContent = new StringBuilder();
            appendHeader(responseContent, "People");
            responseContent.Append("<ol>");
            foreach (Person person in people)
            {
                responseContent.Append($"<li>{person.FirstName} {person.LastName} {person.BirthYear}</li>");
            }
            responseContent.Append("</ol>");
            appendFooter(responseContent);
            response.ContentBytes = Encoding.UTF8.GetBytes(responseContent.ToString());
        }
        if (path == "/add")
        {
            Dictionary<string, string> postParameters = new Dictionary<string, string>();
            if (method == "POST" && requestContent != null)
            {
                Console.WriteLine(Encoding.ASCII.GetString(requestContent));
                string[] parameters = Encoding.ASCII.GetString(requestContent).Split('&');
                foreach (string parameter in parameters)
                {
                    string[] keyValuePair = parameter.Split('=');
                    if (keyValuePair.Length > 0)
                        postParameters[keyValuePair[0]] = keyValuePair[1];
                    else
                        postParameters[keyValuePair[0]] = "";
                }
            }
            string firstName = postParameters.GetValueOrDefault("firstName", "");
            string lastName = postParameters.GetValueOrDefault("lastName", "");
            int birthYear = 0;
            int.TryParse(postParameters.GetValueOrDefault("birthYear", "0"), out birthYear);

            response.ContentType = "text/html; charset=utf-8";
            var content = new StringBuilder();
            appendHeader(content, "Add Person");
            foreach ((string key, string value) in postParameters)
            {
                content.Append($"<p>Params[\"{key}\"] = \"{value}\"</p>");
            }
            content.Append("<form method=\"post\" style=\"display:grid; grid-template-columns: auto auto; width:400px;\">");
            content.Append("<label for=\"firstName\">First Name:</label>");
            content.Append($"<input id=\"firstName\" type=\"text\" name=\"firstName\" value=\"{firstName}\">");
            content.Append("<label for=\"lastName\">Last Name:</label>");
            content.Append($"<input id=\"lastName\" type=\"text\" name=\"lastName\" value=\"{lastName}\">");
            content.Append("<label for=\"birthYear\">Birth Year:</label>");
            content.Append("<input id=\"birthYear\" type=\"number\" name=\"birthYear\" value=\"" + (birthYear == 0 ? "" : birthYear) + "\">");
            content.Append("<div></div><input type=\"submit\">");
            content.Append("</form>");
            if (method == "POST")
            {
                if (firstName.Length == 0)
                    content.Append("<p style=\"color:red\">Fill in First Name.</p>");
                else if (lastName.Length == 0)
                    content.Append("<p style=\"color:red\">Fill in Last Name.</p>");
                else if (birthYear == 0)
                    content.Append("<p style=\"color:red\">Fill in Birth Year.</p>");
                else if (birthYear < 1900)
                    content.Append("<p style=\"color:red\">Birth Year should be at least 1900.</p>");
                else if (birthYear > System.DateTime.Now.Year)
                    content.Append($"<p style=\"color:red\">Birth Year should not be greater than {System.DateTime.Now.Year}.</p>");
                else
                {
                    people.Add(new Person {FirstName = firstName, LastName = lastName, BirthYear = birthYear});
                    content.Append("<p style=\"color:green\">Person successfully added!</p>");
                }
            }
            appendFooter(content);
            response.ContentBytes = Encoding.UTF8.GetBytes(content.ToString());
            return response;
        }
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
            response.ContentBytes = data.ToArray();
            if (path.EndsWith(".png"))
                response.ContentType = "image/png";
            else if (path.EndsWith(".jpg") || path.EndsWith(".jpeg"))
                response.ContentType = "image/jpeg";
            else if (path.EndsWith(".css"))
                response.ContentType = "text/css";
            else if (path.EndsWith(".js"))
                response.ContentType = "text/javascript";
            else if (path.EndsWith(".txt"))
                response.ContentType = "text/plain";
            else if (path.EndsWith(".html"))
                response.ContentType = "text/html";
            else
                response.ContentType = "application/octet-stream";
        }
        if (response.ContentType == null)
        {
            StringBuilder responseContent = new StringBuilder();
            appendHeader(responseContent, "Not Found");
            responseContent.Append("404<br>Not Found");
            appendFooter(responseContent);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentBytes = Encoding.UTF8.GetBytes(responseContent.ToString());
        }
        return response;
    }

    public void Serve(Socket clientSocket)
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
        int contentLength = 0;
        int contentLengthPos = requestStr.IndexOf("Content-Length:");
        if (contentLengthPos >= 0)
        {
            const int LengthOfContentLength = 15; // "Content-Length:".Length;
            contentLengthPos += LengthOfContentLength;
            int contentLengthEndLine = requestStr.IndexOf('\r', contentLengthPos);
            if (contentLengthEndLine < 0)
                contentLengthEndLine = requestStr.Length;
            while (contentLengthPos < requestStr.Length && requestStr[contentLengthPos] == ' ')
                ++contentLengthPos;
            int.TryParse(requestStr.AsSpan(contentLengthPos, contentLengthEndLine - contentLengthPos), out contentLength);
        }
        Console.WriteLine("===== Parsed =====");
        Console.WriteLine("Method: {0}", method);
        Console.WriteLine("Path: \"{0}\"", path);
        Console.WriteLine("Version: \"{0}\"", ver);
        Console.WriteLine("Content-Length: {0}", contentLength);

        while (contentLength > 0 && request.Count < position + 4 + contentLength)
        {
            int readed = clientSocket.Receive(buffer);
            if (readed == 0)
                break;
            request.AddRange(buffer.Take(readed));
        }
        byte[]? requestContent = null;
        if (contentLength > 0)
        {
            requestContent = request.Skip(position + 4).Take(contentLength).ToArray();
        }

        HttpResponse response = Handle(method, path, requestContent);

        StringBuilder serverResponse = new StringBuilder(
              "HTTP/1.0 200 OK\r\n"
            + $"Content-Type: {response.ContentType}\r\n"
            + "Connection: close\r\n");
        
        serverResponse.AppendFormat("Content-Length: {0}\r\n", response.ContentBytes.Length);
        serverResponse.Append("\r\n");
        
        Console.WriteLine("============ Response Headers ============");
        Console.WriteLine(serverResponse);
        Console.WriteLine("========================");
        clientSocket.Send(Encoding.ASCII.GetBytes(serverResponse.ToString()), SocketFlags.None);
        for (int bytesSent = 0; bytesSent < response.ContentBytes.Length;)
        {
            bytesSent += clientSocket.Send(response.ContentBytes, bytesSent, response.ContentBytes.Length - bytesSent, SocketFlags.None);
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
        Program app = new Program();
        while (true)
        {
            // accept new client
            Socket clientSocket = listenSocket.Accept();
            app.Serve(clientSocket);
        }
    }
}
