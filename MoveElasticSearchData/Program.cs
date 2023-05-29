using System.Text.RegularExpressions;

namespace MoveElasticSearchData;

internal class Program
{
    private static async Task Main()
    {
        // source and target address for request
        const string source = "your elastic source address";
        const string target = "your elastic destination address";

        var path = Directory.GetCurrentDirectory();
        const string requestFileName = "elasticRequest.txt";
        var dockerFile = Path.Combine(path, "Dockerfile");
        var requestFile = Path.Combine(path, requestFileName);
        const string fileName = "elastic.txt";
        var sourcePath = Path.Combine(path, fileName);

        // read template from url as string and write to elastic.txt file
        DeleteFile(sourcePath);
        DeleteFile(requestFile);
        DeleteFile(dockerFile);

        // delete the file if exist
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"{source}/_cat/indices");
        var responseContent = await response.Content.ReadAsStringAsync();
        WriteFile(sourcePath, responseContent);


        // get current path  of the file and the file name
        var sourceString = ReadFile(sourcePath);
        const string pattern = @"(?<=\bopen\s)\S+";

        // get all matches of sourceString from regex pattern
        var matches = Regex.Matches(sourceString, pattern);
        foreach (var match in matches)
        {
            // get the string of the match
            var matchString = match.ToString();

            if (matchString == null) 
                continue;

            var request = CreateRequest(source, target, matchString);

            // write the request to the file
            WriteFile(requestFile, request);
        }

        // create docker file
        var dockerCommands = ReadFile(requestFile);
        CreateDockerFile(dockerFile, dockerCommands);
    }

    // read from text file and get the string
    public static string ReadFile(string path)
    {
        var text = File.ReadAllText(path);
        return text;
    }

    // delete file if exist
    public static void DeleteFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    // create docker file and write to it
    public static void CreateDockerFile(string path, string commands)
    {
        var dockerText = "FROM node:latest" + Environment.NewLine +
                         "RUN npm install elasticdump -g" + Environment.NewLine +
                         commands;
        WriteFile(path, dockerText);
    }

    // write to text file if exist or create new one and write to it
    public static void WriteFile(string path, string text)
    {
        if (File.Exists(path))
        {
            File.AppendAllText(path, text);
        }
        else
        {
            File.WriteAllText(path, text);
        }
    }

    // create request command
    public static string CreateRequest(string source, string target, string matchString, string type = "data", string limit = "5000")
    {
        var request =
            $"RUN elasticdump --input={source}/{matchString} --output={target}/{matchString} --type={type} --limit={limit}" + Environment.NewLine;
        return request;
    }
}