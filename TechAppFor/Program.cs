using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using System.Net;
using TechAppFor.Models;
using TechAppFor;




// Hi, i`m Andrew! My Telegram: @HARUFORK 



var MyConfig = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
string AccesToken = MyConfig.GetValue<string>("Acces_tokenVK");
const string ApiUrlVk = "https://api.vk.com/method/";
const string VerVkApi = "5.131";
const string PathFileOfLog = "logs.log";

DateTime dateTime = new DateTime();


// builde
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "14.11.22",
        Title = "Counting characters in the last five VK posts of profile",
        Contact = new OpenApiContact
        {
            Name = "Contact me on Telegram @HARUFORK",
            Url = new Uri("https://t.me/HARUFORK")
        }
    });
});

// function  get response
JObject wall(string id, int offset, int count, string AccesToken, string VerVkApi)
{
    var wc = new WebClient() { Encoding = System.Text.Encoding.UTF8 };
    var geted_s = wc.DownloadString($"{ApiUrlVk}wall.get?domain={id}&count={count}&extended=0&offset={offset}&access_token={AccesToken}&v={VerVkApi}");
    var parse_respone = JObject.Parse(geted_s);
    return parse_respone;
}



//app
var app = builder.Build();
app.UseSwagger( x => x.SerializeAsV2 = true);
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});



app.MapGet("/api/counting_characters_in_last_five_vk_posts_of_profile", (string domain_of_page_profile) =>
{
    //log file
    using (StreamWriter writer = new StreamWriter(PathFileOfLog, true, System.Text.Encoding.Default))
    {
        if(domain_of_page_profile.StartsWith("https://vk.com/"))
        {
            domain_of_page_profile=domain_of_page_profile.Replace("https://vk.com/","");
        }

        if (domain_of_page_profile == null || domain_of_page_profile == "" )
        {
            writer.WriteLine($"Warning! Empty values were entered! \n Finish ({DateTime.Now})");
            return $"Warning({DateTime.Now})! More information in log file. Empty values were entered!";
        }

        writer.WriteLine($"\nStart counting for domain ({DateTime.Now}): {domain_of_page_profile}\nCorrent token: {AccesToken}");


        //Get last vk post
        var wall_parse_respone = wall(domain_of_page_profile, 0, 5, AccesToken, VerVkApi);


        if (wall_parse_respone == null)
        {
            writer.WriteLine($"Unknow error.\n Finish ({DateTime.Now})");
            return $"Unknow error({DateTime.Now})! More information in log file.";
        }
        else if (wall_parse_respone["error"] != null)
        {
            writer.WriteLine($"Error (Code:{wall_parse_respone["error"]["error_code"]}) {wall_parse_respone["error"]["error_msg"]}\n Finish ({DateTime.Now})");
            return $"Error({DateTime.Now})! More information in log file. Error (Code:{wall_parse_respone["error"]["error_code"]}) {wall_parse_respone["error"]["error_msg"]}";
        }
        else if (wall_parse_respone["response"]["count"].ToString() == "0")
        {
            writer.WriteLine($" Warning! This user has no posts. \n Finish ({DateTime.Now})");
            return $"Warning({DateTime.Now})! This user has no posts. More information in log file. ";
        }
        else
        {
            //Counting characters
            var char_list = new Dictionary<char, int>();
            foreach (var itm in wall_parse_respone["response"]["items"])
            {
                foreach (char el in itm["text"].ToString())
                {
                    if (char.IsLetter(el))
                    {
                        if (!char_list.ContainsKey(char.ToUpper(el)))
                        {
                            char_list.Add(char.ToUpper(el), 1);
                        }
                        else
                        {
                            char_list[char.ToUpper(el)]++;
                        };
                    };
                }
            }

            //order by
            string log_data_string = "";
            foreach (var el in char_list.OrderBy(x => x.Key))
            {
                log_data_string += el.ToString();
            }

            writer.WriteLine($"Success ({DateTime.Now})! Result: {log_data_string}");

            // add result to database
            try
            {
                using (ApplicationContext db = new ApplicationContext())
                {
                    // create table it does not exist
                    /* db.ResultOfCountingCharacters */

                    ResultOfCountingCharacters resultOfCountingCharacters = new ResultOfCountingCharacters { Date = DateTime.UtcNow, Result = log_data_string };
                    db.ResultOfCountingCharacters.Add(resultOfCountingCharacters);
                    writer.WriteLine($"Result was added to database.");
                    db.SaveChanges();
                    writer.WriteLine($"Was saved changes of database.");

                }

                writer.WriteLine($"Finish of successful counting for domain({DateTime.Now}): {domain_of_page_profile} ");
                return $"Success({DateTime.Now})! More information in log file. Result: \n {log_data_string}";
            }
            catch(Exception ex)
            {
                writer.WriteLine($"Error({DateTime.Now})!" +
                    $" Finish of a successful count, but without writing to the database." +
                    $" For domain: {domain_of_page_profile} ({DateTime.Now})" +
                    $"\nMore about error: {ex}");

                return $"Error({DateTime.Now})! Finish of a successful count, but without writing to the database. More information in log file. Result: \n {log_data_string}";
            }
        }
    }
});


app.Run();
