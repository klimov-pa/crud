using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Text;

public enum HttpStatusCode
{
    Ok = 200,
    NotFound = 404,
}

public static class HttpStatusCodeExtensions
{
    public static string GetName(this HttpStatusCode code)
    {
        switch (code)
        {
            case HttpStatusCode.Ok:
                return "OK";
            case HttpStatusCode.NotFound:
                return "Not Found";
            default:
                throw new ArgumentException($"Unknown status code {code}.");
        }
    }
}

class HttpRequest
{
    public string Method = null!;
    public string Path = null!;
    public Dictionary<string, string> PathParameters = null!;
    public Dictionary<string, string> PostParameters = null!;
    public byte[]? Body = null;
}

class HttpResponse
{
    public string ContentType = null!;
    public byte[] ContentBytes = new byte[0];
    public HttpStatusCode StatusCode = 0;
}

class Parameters
{
    public static Dictionary<string, string> Parse(string text)
    {
        Dictionary<string, string> result = new();
        string[] parameters = text.Split('&');
        foreach (string parameter in parameters)
        {
            string[] keyValuePair = parameter.Split('=');
            if (keyValuePair.Length > 0)
                result[keyValuePair[0]] = keyValuePair[1];
            else
                result[keyValuePair[0]] = "";
        }
        return result;
    }
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
        content.Append("<!DOCTYPE html><meta charset=\"UTF-8\"><html lang=\"en\"><head><title>");
        content.Append(title);
        content.Append("</title></head><body><header>");
        content.Append("<ul style=\"display:flex; flex-direction:row; list-style:none;\">");
        content.Append("<li style=\"padding:0.3em;\"><a href=\"/\">Index</a></li>");
        content.Append("<li style=\"padding:0.3em;\"><a href=\"/list\">People</a></li>");
        content.Append("<li style=\"padding:0.3em;\"><a href=\"/add\">Add Person</a></li>");
        content.Append("</ul></header>");
    }

    private void appendFooter(StringBuilder content)
    {
        content.Append("<footer style=\"margin-top:0.5em;\">");
        content.Append($"<div>{System.DateTime.Now.Year} Rights reserved</div>");
        content.Append("</footer></body></html>");
    }

    private HttpResponse responseFromContent(string title, string content)
    {
        var html = new StringBuilder();
        appendHeader(html, title);
        html.Append(content);
        appendFooter(html);
        return new HttpResponse {
            ContentType = "text/html; charset=utf-8",
            ContentBytes = Encoding.UTF8.GetBytes(html.ToString()),
            StatusCode = HttpStatusCode.Ok,
        };
    }

    private HttpResponse index(HttpRequest request)
    {
        StringBuilder responseContent = new StringBuilder();
        responseContent.Append("Hello World");
        return responseFromContent("Index", responseContent.ToString());
    }

    private HttpResponse? list(HttpRequest request)
    {
        var content = new StringBuilder();
        foreach (Person person in people)
            content.Append($"<li>{person.FirstName} {person.LastName} {person.BirthYear}</li>");
        return responseFromContent("People", content.ToString());
    }

    private HttpResponse? addPerson(HttpRequest request)
    {
        string firstName = request.PostParameters.GetValueOrDefault("firstName", "");
        string lastName = request.PostParameters.GetValueOrDefault("lastName", "");
        int birthYear = 0;
        int.TryParse(request.PostParameters.GetValueOrDefault("birthYear", "0"), out birthYear);

        var content = new StringBuilder();
        foreach ((string key, string value) in request.PostParameters)
        {
            content.Append($"<p>Params[\"{key}\"] = \"{value}\"</p>");
        }
        content.Append("<form method=\"post\" style=\"display:grid; grid-template-columns: auto auto; width:400px;\">");
        content.Append("<label for=\"firstName\">First Name:</label>");
        content.Append($"<input id=\"firstName\" type=\"text\" name=\"firstName\" value=\"{firstName}\" required>");
        content.Append("<label for=\"lastName\">Last Name:</label>");
        content.Append($"<input id=\"lastName\" type=\"text\" name=\"lastName\" value=\"{lastName}\" required>");
        content.Append("<label for=\"birthYear\">Birth Year:</label>");
        content.Append("<input id=\"birthYear\" type=\"number\" name=\"birthYear\" step=\"1\" min=\"1900\"");
        content.Append($"max=\"{System.DateTime.Now.Year}\" value=\"" + (birthYear == 0 ? "" : birthYear) + "\" required>");
        content.Append("<div></div><input type=\"submit\">");
        content.Append("</form>");
        if (request.Method == "POST")
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
        return responseFromContent("Add Person", content.ToString());
    }

    private HttpResponse? editPerson(HttpRequest request)
    {
        int id = people.Count;
        int.TryParse(request.PathParameters.GetValueOrDefault("id", id.ToString()), out id);
        if (!(id >= 0 && id < people.Count))
            return null;
        string firstName = request.PostParameters.GetValueOrDefault("firstName", people[id].FirstName);
        string lastName = request.PostParameters.GetValueOrDefault("lastName", people[id].LastName);
        int birthYear = 0;
        int.TryParse(request.PostParameters.GetValueOrDefault("birthYear", people[id].BirthYear.ToString()), out birthYear);

        var content = new StringBuilder();
        foreach ((string key, string value) in request.PostParameters)
        {
            content.Append($"<p>Params[\"{key}\"] = \"{value}\"</p>");
        }
        content.Append("<form method=\"post\" style=\"display:grid; grid-template-columns: auto auto; width:400px;\">");
        content.Append("<label for=\"firstName\">First Name:</label>");
        content.Append($"<input id=\"firstName\" type=\"text\" name=\"firstName\" value=\"{firstName}\" required>");
        content.Append("<label for=\"lastName\">Last Name:</label>");
        content.Append($"<input id=\"lastName\" type=\"text\" name=\"lastName\" value=\"{lastName}\" required>");
        content.Append("<label for=\"birthYear\">Birth Year:</label>");
        content.Append("<input id=\"birthYear\" type=\"number\" name=\"birthYear\" step=\"1\" min=\"1900\"");
        content.Append($"max=\"{System.DateTime.Now.Year}\" value=\"{birthYear}\" required>");
        content.Append("<div></div><input type=\"submit\">");
        content.Append("</form>");
        if (request.Method == "POST")
        {
            if (firstName.Length == 0)
                content.Append("<p style=\"color:red\">Fill in First Name.</p>");
            else if (lastName.Length == 0)
                content.Append("<p style=\"color:red\">Fill in Last Name.</p>");
            else if (birthYear == 0)
                content.Append("<p style=\"color:red\">Fill in Birth Year.</p>");
            else if (birthYear < 1900)
                content.Append($"<p style=\"color:red\">Birth Year {birthYear} should be at least 1900.</p>");
            else if (birthYear > System.DateTime.Now.Year)
                content.Append($"<p style=\"color:red\">Birth Year should not be greater than {System.DateTime.Now.Year}.</p>");
            else
            {
                people[id].FirstName = firstName;
                people[id].LastName = lastName;
                people[id].BirthYear = birthYear;
                content.Append("<p style=\"color:green\">Person successfully edited!</p>");
            }
        }
        return responseFromContent("Edit Person", content.ToString());
    }

    private HttpResponse? deletePerson(HttpRequest request)
    {
        int id = people.Count;
        int.TryParse(request.PathParameters.GetValueOrDefault("id", id.ToString()), out id);
        if (!(id >= 0 && id < people.Count))
            return null;
        var content = new StringBuilder();
        content.Append("<form method=\"post\" style=\"display:grid; grid-template-columns: auto auto; width:400px;\">");
        content.Append("<label for=\"firstName\">First Name:</label>");
        content.Append($"<input id=\"firstName\" type=\"text\" name=\"firstName\" value=\"{people[id].FirstName}\" disabled>");
        content.Append("<label for=\"lastName\">Last Name:</label>");
        content.Append($"<input id=\"lastName\" type=\"text\" name=\"lastName\" value=\"{people[id].LastName}\" disabled>");
        content.Append("<label for=\"birthYear\">Birth Year:</label>");
        content.Append($"<input id=\"birthYear\" type=\"number\" name=\"birthYear\" value=\"{people[id].BirthYear}\" disabled>");
        content.Append("<div></div><button type=\"submit\">Delete</button>");
        content.Append("</form>");
        if (request.Method == "POST")
        {
            people.RemoveAt(id);
            content.Append("<p style=\"color:green\">Person successfully deleted!</p>");
        }
        return responseFromContent("Remove Person", content.ToString());
    }

    private HttpResponse Handle(string method, string pathAndParams, byte[]? requestContent)
    {
        int questionMark = pathAndParams.IndexOf('?');
        if (questionMark < 0)
            questionMark = pathAndParams.Length;
        string path = pathAndParams.Substring(0, questionMark);
        Dictionary<string, string> pathParameters = new Dictionary<string, string>();
        if (questionMark < pathAndParams.Length)
        {
            var pathParams = pathAndParams.Substring(questionMark + 1, pathAndParams.Length - questionMark - 1);
            string[] parameters = pathParams.Split('&');
            pathParameters = Parameters.Parse(pathParams);
        }
        Dictionary<string, string> postParameters = new Dictionary<string, string>();
        if (method == "POST" && requestContent != null)
        {
            postParameters = Parameters.Parse(Encoding.ASCII.GetString(requestContent));
        }
        var request = new HttpRequest {
            Method = method,
            Path = path,
            PathParameters = pathParameters,
            PostParameters = postParameters,
            Body = requestContent,
        };
        HttpResponse? response = null;
        if (response == null && path == "/" || path.ToLower() == "/index")
            response = index(request);
        if (response == null && path == "/list")
            response = list(request);
        if (response == null && path == "/add")
            response = addPerson(request);
        if (response == null && path == "/edit")
            response = editPerson(request);
        if (response == null && path == "/delete")
            response = deletePerson(request);
        if (response != null)
            return response;
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
            response = new HttpResponse();
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
        if (response == null || response.ContentType == null)
        {
            var status = HttpStatusCode.NotFound;
            response = responseFromContent(status.GetName(),
                (int)status + "<br>" + status.GetName());
            response.StatusCode = status;
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
              $"HTTP/1.0 {(int)response.StatusCode} {response.StatusCode.GetName()}\r\n"
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
